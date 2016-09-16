using System;
using System.Collections.Generic;
using System.Linq;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;

namespace NRaft
{
	public class Node : IDisposable
	{
		private readonly IStore _store;
		private readonly IClock _clock;
		private readonly IConnector _connector;
		private readonly int _nodeID;

		private readonly HashSet<int> _knownNodes;
		private HashSet<HashSet<int>> _quorum;

		//only in memory
		public Types Role { get; private set; }
		public int CommitIndex { get; private set; }

		//leader-only state - perhaps subclass or extract
		private readonly LightweightCache<int, int> _nextIndex;
		private readonly LightweightCache<int, int> _matchIndex;

		//candidate0only state - perhaps subclass or extract
		private readonly HashSet<int> _votesResponded;
		private readonly HashSet<int> _votesGranted;
		private readonly IPulseable _heart;
		private IDisposable _election;

		public Node(IStore store, IClock clock, IConnector connector, int nodeID)
		{
			_store = store;
			_clock = clock;
			_connector = connector;
			_nodeID = nodeID;
			_knownNodes = new HashSet<int>();
			_quorum = new HashSet<HashSet<int>>();

			AddNodeToCluster(nodeID);

			_nextIndex = new LightweightCache<int, int>(id => 1);
			_matchIndex = new LightweightCache<int, int>(id => 1);

			_votesResponded = new HashSet<int>();
			_votesGranted = new HashSet<int>();

			BecomeFollower();

			CommitIndex = 0;

			_connector.Register(_nodeID, OnAppendEntries);
			_connector.Register(_nodeID, OnAppendEntriesResponse);
			_connector.Register(_nodeID, OnRequestVote);
			_connector.Register(_nodeID, OnRequestVoteResponse);

			_heart = clock.CreatePulseTimeout(TimeSpan.FromMilliseconds(350), OnHeartbeatElapsed); //should be random...
		}

		public IEnumerable<int> KnownNodes => _knownNodes;
		public IEnumerable<int> VotesResponded => _votesResponded;
		public IEnumerable<int> VotesGranted => _votesGranted;

		public int NextIndexFor(int nodeID) => _nextIndex[nodeID];
		public int MatchIndexFor(int nodeID) => _matchIndex[nodeID];

		public void OnAppendEntries(AppendEntriesRequest message)
		{
			_heart.Pulse();

			UpdateTerm(message.Term);

			var success = AppendEntries(message);

			_connector.SendReply(new AppendEntriesResponse
			{
				LeaderID = message.LeaderID,
				FollowerID = _nodeID,
				Success = success,
				Term = _store.CurrentTerm,
				MatchIndex = success ? message.PreviousLogIndex + message.Entries.Length : 0
			});
		}

		public void OnAppendEntriesResponse(AppendEntriesResponse message)
		{
			UpdateTerm(message.Term);

			if (message.Term != _store.CurrentTerm)
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

			AdvanceCommitIndex();
		}

		public void OnRequestVote(RequestVoteRequest message)
		{
			UpdateTerm(message.Term);

			var voteGranted = RequestVote(message);

			_connector.SendReply(new RequestVoteResponse
			{
				CandidateID = message.CandidateID,
				GranterID = _nodeID,
				Term = _store.CurrentTerm,
				VoteGranted = voteGranted
			});
		}

		public void OnRequestVoteResponse(RequestVoteResponse message)
		{
			UpdateTerm(message.Term);

			if (message.Term != _store.CurrentTerm)
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

		private void BecomeFollower()
		{
			Role = Types.Follower;
		}

		public void SendAppendEntries()
		{
			foreach (var nodeID in KnownNodes)
			{
				var prevIndex = _nextIndex[nodeID] - 1;
				var prevTerm = prevIndex > 0 ? _store.Log.Single(e => e.Index == prevIndex).Term : 0;

				var lastEntry = Math.Min(LastIndex(), _nextIndex[nodeID]);

				var start = _nextIndex[nodeID];
				var entries = _store.Log
					.SkipWhile(e => e.Index < start)
					.TakeWhile(e => e.Index <= lastEntry)
					.ToArray();

				_connector.SendHeartbeat(new AppendEntriesRequest
				{
					LeaderID = _nodeID,
					RecipientID = nodeID,
					Term = _store.CurrentTerm,
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
				Term = _store.CurrentTerm,
				Command = value
			};

			_store.Write(write =>
			{
				write.Log = _store.Log.Concat(new[] { newEntry }).ToArray();
			});
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
					.Select(pair => pair.Key)
					.Concat(new[] { _nodeID });

				return new HashSet<int>(nodes);
			};

			var agreeIndexes = _store.Log
				.Select(e => e.Index)
				.Where(index => _quorum.Any(q => q.SetEquals(agree(index))))
				.ToArray();

			if (agreeIndexes.Any() && _store.Log.Single(e => e.Index == agreeIndexes.Max()).Term == _store.CurrentTerm)
				CommitIndex = agreeIndexes.Max();

		}

		private int LastTerm() => _store.Log.Length == 0 ? 0 : _store.Log.Last().Term;
		private int LastIndex() => _store.Log.Length == 0 ? 0 : _store.Log.Last().Index;

		private bool RequestVote(RequestVoteRequest message)
		{
			var logOk = message.LastLogTerm > LastTerm()
				|| (message.LastLogTerm == LastTerm() && message.LastLogIndex >= LastIndex());

			var grant = message.Term == _store.CurrentTerm
				&& logOk
				&& (_store.VotedFor.HasValue == false || _store.VotedFor.Value == message.CandidateID);

			if (grant)
				_store.Write(write => write.VotedFor = message.CandidateID);

			return grant;
		}


		private bool AppendEntries(AppendEntriesRequest message)
		{
			if (message.Term > _store.CurrentTerm)
				return false;

			var logOk = message.PreviousLogIndex == 0
				|| (
					message.PreviousLogIndex > 0
					&& message.PreviousLogIndex <= LastIndex()
					&& message.PreviousLogTerm == _store.Log.Single(e => e.Index == message.PreviousLogIndex).Term
				);

			if (message.Term < _store.CurrentTerm || (message.Term == _store.CurrentTerm && Role == Types.Follower && logOk == false))
				return false;

			if (message.Term == _store.CurrentTerm && Role == Types.Candidate)
			{
				BecomeFollower();
				return true;
			}

			if (message.Term == _store.CurrentTerm && Role == Types.Follower && logOk)
			{
				_store.Write(write => write.Log = MergeChangeSets(_store.Log, message.Entries));
				CommitIndex = message.LeaderCommit;
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
			if (messageTerm <= _store.CurrentTerm)
				return;

			BecomeFollower();

			_store.Write(write =>
			{
				write.CurrentTerm = messageTerm;
				write.VotedFor = null;
			});
		}

		//I wanted to call this OnHeartFailure...
		private void OnHeartbeatElapsed()
		{
			BecomeCandidate();
		}

		private void OnElectionTimeout()
		{
			_election?.Dispose();

			BecomeLeader();

			if (Role != Types.Leader)
				BecomeCandidate();
		}

		private void BecomeCandidate()
		{
			Role = Types.Candidate;

			_votesResponded.Clear();
			_votesGranted.Clear();

			_store.Write(write =>
			{
				write.CurrentTerm = _store.CurrentTerm + 1;
				write.VotedFor = _nodeID;
			});

			OnRequestVoteResponse(new RequestVoteResponse
			{
				GranterID = _nodeID,
				Term = _store.CurrentTerm,
				VoteGranted = true
			});

			_connector.RequestVotes(new RequestVoteRequest
			{
				CandidateID = _nodeID,
				Term = _store.CurrentTerm,
				LastLogIndex = LastIndex(),
				LastLogTerm = LastTerm()
			});

			_election = _clock.CreateElectionTimeout(TimeSpan.FromMilliseconds(500), OnElectionTimeout); //or whatever the electiontimeout is
		}

		private void BecomeLeader()
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

		public void Dispose()
		{
			_connector.Deregister(_nodeID, OnAppendEntries);
			_connector.Deregister(_nodeID, OnAppendEntriesResponse);
			_connector.Deregister(_nodeID, OnRequestVote);
			_connector.Deregister(_nodeID, OnRequestVoteResponse);
		}
	}
}
