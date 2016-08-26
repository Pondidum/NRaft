using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using NRaft.Infrastructure;
using NRaft.Messages;

namespace NRaft
{
	public class State : IDisposable
	{
		private readonly IDispatcher _dispatcher;
		private readonly IListener _listener;
		private readonly int _nodeID;

		private readonly HashSet<int> _knownNodes;
		private HashSet<HashSet<int>> _quorum;

		//need to be persistent store
		public int CurrentTerm { get; private set; }
		private int? _votedFor;
		private LogEntry[] _log;

		//only in memory
		public Types Role { get; private set; }
		public int CommitIndex { get; private set; }

		//leader-only state - perhaps subclass or extract
		private readonly LightweightCache<int, int> _nextIndex;
		private readonly LightweightCache<int, int> _matchIndex;

		//candidate0only state - perhaps subclass or extract
		private readonly HashSet<int> _votesResponded;
		private readonly HashSet<int> _votesGranted;


		public State(IDispatcher dispatcher, IListener listener, int nodeID)
		{
			_dispatcher = dispatcher;
			_listener = listener;
			_nodeID = nodeID;
			_knownNodes = new HashSet<int>();
			_quorum = new HashSet<HashSet<int>>();

			_nextIndex = new LightweightCache<int, int>(id => 1);
			_matchIndex = new LightweightCache<int, int>(id => 1);

			_votesResponded = new HashSet<int>();
			_votesGranted = new HashSet<int>();

			CurrentTerm = 0;
			_votedFor = null;
			_log = Enumerable.Empty<LogEntry>().ToArray();
			Role = Types.Follower;

			CommitIndex = 0;

			_listener.Register(_nodeID, OnAppendEntries);
			_listener.Register(_nodeID, OnAppendEntriesResponse);
			_listener.Register(_nodeID, OnRequestVote);
			_listener.Register(_nodeID, OnRequestVoteResponse);
		}

		public IEnumerable<int> KnownNodes => _knownNodes;
		public IEnumerable<LogEntry> Log => _log;
		public IEnumerable<int> VotesResponded => _votesResponded;
		public IEnumerable<int> VotesGranted => _votesGranted;

		public int NextIndexFor(int nodeID) => _nextIndex[nodeID];
		public int MatchIndexFor(int nodeID) => _matchIndex[nodeID];

		public void OnAppendEntries(AppendEntriesRequest message)
		{
			UpdateTerm(message.Term);

			var success = AppendEntries(message);

			_dispatcher.SendReply(new AppendEntriesResponse
			{
				LeaderID = message.LeaderID,
				FollowerID = _nodeID,
				Success = success,
				Term = CurrentTerm,
				MatchIndex = success ? message.PreviousLogIndex + message.Entries.Length : 0
			});
		}

		public void OnAppendEntriesResponse(AppendEntriesResponse message)
		{
			UpdateTerm(message.Term);

			if (message.Term != CurrentTerm)
				return;

			if (message.Success)
			{
				_nextIndex[message.FollowerID] = message.MatchIndex + 1;
				_matchIndex[message.FollowerID] = message.MatchIndex;
			}
			else
			{
				_nextIndex[message.FollowerID] = Math.Max(message.MatchIndex - 1, 1);
			}
		}

		public void OnRequestVote(RequestVoteRequest message)
		{
			UpdateTerm(message.Term);

			var voteGranted = RequestVote(message);

			_dispatcher.SendReply(new RequestVoteResponse
			{
				CandidateID = message.CandidateID,
				GranterID = _nodeID,
				Term = CurrentTerm,
				VoteGranted = voteGranted
			});
		}

		public void OnRequestVoteResponse(RequestVoteResponse message)
		{
			UpdateTerm(message.Term);

			if (message.Term != CurrentTerm)
				return;

			_votesResponded.Add(message.GranterID);

			if (message.VoteGranted)
				_votesGranted.Add(message.GranterID);
		}

		public void ResetVotes()
		{
			_votesResponded.Clear();
			_votesGranted.Clear();
		}

		public void BecomeCandidate()
		{
			Role = Types.Candidate;
			CurrentTerm++;

			OnRequestVoteResponse(new RequestVoteResponse
			{
				GranterID = _nodeID,
				Term = CurrentTerm,
				VoteGranted = true
			});

			_dispatcher.RequestVotes(new RequestVoteRequest
			{
				CandidateID = _nodeID,
				Term = CurrentTerm,
				LastLogIndex = LastIndex(),
				LastLogTerm = LastTerm()
			});
		}

		public void BecomeLeader()
		{
			if (Role != Types.Candidate)
				return;

			if (_quorum.Any(q => q.IsSubsetOf(_votesGranted)) == false)
				return;

			Role = Types.Leader;

			var last = LastIndex() + 1;

			foreach (var nodeID in KnownNodes)
				_nextIndex[nodeID] = last;

			foreach (var nodeID in KnownNodes)
				_matchIndex[nodeID] = 0;

			SendAppendEntries();
		}

		public void SendAppendEntries()
		{
			foreach (var nodeID in KnownNodes)
			{
				var prevIndex = _nextIndex[nodeID] - 1;
				var prevTerm = prevIndex > 0 ? Log.Single(e => e.Index == prevIndex).Term : 0;

				var lastEntry = Math.Min(LastIndex(), _nextIndex[nodeID]);

				var start = _nextIndex[nodeID];
				var entries = Log
					.SkipWhile(e => e.Index < start)
					.TakeWhile(e => e.Index <= lastEntry)
					.ToArray();

				_dispatcher.SendHeartbeat(new AppendEntriesRequest
				{
					LeaderID = _nodeID,
					RecipientID = nodeID,
					Term = CurrentTerm,
					PreviousLogIndex = prevIndex,
					PreviousLogTerm = prevTerm,
					LeaderCommit = Math.Min(CommitIndex, lastEntry),
					Entries = entries
				});
			}
		}

		public void OnClientRequest(object value)
		{
			if (Role != Types.Leader)
				return;

			var newEntry = new LogEntry
			{
				Index = LastIndex() + 1,
				Term = CurrentTerm,
				Command = value
			};

			_log = Log.Concat(new[] { newEntry }).ToArray();
		}

		public void AddNodeToCluster(int nodeID)
		{
			_knownNodes.Add(nodeID);
			_quorum = Quorum.GenerateAllPossibilities(_knownNodes.ToArray());
		}

		public void AdvanceCommitIndex()
		{
			if (Role != Types.Leader)
				return;

			Func<int, HashSet<int>> agree = index =>
			{
				var nodes = _matchIndex
					.Dictionary
					.Where(pair => pair.Value >= index)
					.Select(pair => pair.Key);

				return new HashSet<int>(nodes);
			};

			var agreeIndexes = _log
				.Select(e => e.Index)
				.Where(index => KnownNodes.Any() == false || _quorum.Any(q => q.SetEquals(agree(index))))
				.ToArray();

			if (agreeIndexes.Any() && _log.Single(e => e.Index == agreeIndexes.Max()).Term == CurrentTerm)
				CommitIndex = agreeIndexes.Max();

		}

		private int LastTerm() => _log.Length == 0 ? 0 : _log.Last().Term;
		private int LastIndex() => _log.Length == 0 ? 0 : _log.Last().Index;

		private bool RequestVote(RequestVoteRequest message)
		{
			var logOk = message.LastLogTerm > LastTerm()
				|| (message.LastLogTerm == LastTerm() && message.LastLogIndex >= LastIndex());

			var grant = message.Term == CurrentTerm
				&& logOk
				&& (_votedFor.HasValue == false || _votedFor.Value == message.CandidateID);

			if (grant)
				_votedFor = message.CandidateID;

			return grant;
		}


		private bool AppendEntries(AppendEntriesRequest message)
		{
			if (message.Term > CurrentTerm)
				return false;

			var logOk = message.PreviousLogIndex == 0
				|| (
					message.PreviousLogIndex > 0
					&& message.PreviousLogIndex <= LastIndex()
					&& message.PreviousLogTerm == _log.Single(e => e.Index == message.PreviousLogIndex).Term
				);

			if (message.Term < CurrentTerm || (message.Term == CurrentTerm && Role == Types.Follower && logOk == false))
				return false;

			if (message.Term == CurrentTerm && Role == Types.Candidate)
			{
				Role = Types.Follower;
				return true;
			}

			if (message.Term == CurrentTerm && Role == Types.Follower && logOk)
			{
				_log = MergeChangeSets(_log, message.Entries);
				CommitIndex = Math.Min(message.LeaderCommit, LastIndex());
				return true;
			}

			return false;
		}

		private static LogEntry[] MergeChangeSets(LogEntry[] current, LogEntry[] changes)
		{
			if (changes.Length == 0)
				return current;

			return current
				.TakeWhile(e => e.Index < changes[0].Index)
				.Concat(changes)
				.ToArray();
		}

		private void UpdateTerm(int messageTerm)
		{
			if (messageTerm <= CurrentTerm)
				return;

			CurrentTerm = messageTerm;
			Role = Types.Follower;
			_votedFor = null;
		}

		public void ForceTerm(int term)
		{
			CurrentTerm = term;
		}

		public void ForceLog(params LogEntry[] log)
		{
			_log = log;
		}

		public void ForceCommitIndex(int index)
		{
			CommitIndex = index;
		}

		public void ForceVotedFor(int candidateID)
		{
			_votedFor = candidateID;
		}

		public void ForceType(Types type)
		{
			Role = type;
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}
