using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace NRaft
{
	public class State
	{
		private readonly IDispatcher _dispatcher;
		private readonly int _nodeID;

		//need to be persistent store
		private int _currentTerm;
		private int? _votedFor;
		private LogEntry[] _log;

		//only in memory
		public Types Role { get; private set; }
		public int CommitIndex { get; private set; }
		private int _lastApplied;

		//leader-only state - perhaps refactor to leaderState
		private int _nextIndex;
		private int _matchIndex;


		public State(IDispatcher dispatcher, int nodeID)
		{
			_dispatcher = dispatcher;
			_nodeID = nodeID;
			_currentTerm = 0;
			_votedFor = null;
			_log = Enumerable.Empty<LogEntry>().ToArray();
			Role = Types.Follower;

			CommitIndex = 0;
			_lastApplied = 0;
		}

		public IEnumerable<LogEntry> Log => _log;


		public void OnAppendEntries(AppendEntriesRpc message)
		{
			var success = AppendEntries(message);

			_dispatcher.SendReply(new AppendEntriesResponse
			{
				Success = success,
				Term = _currentTerm
			});
		}

		public void OnRequestVote(RequestVoteRpc message)
		{
			var voteGranted = RequestVote(message);

			_dispatcher.SendReply(new RequestVoteResponse
			{
				Term = _currentTerm,
				VoteGranted = voteGranted
			});
		}


		private int LastTerm() => _log.Length == 0 ? 0 : _log.Last().Term;
		private int LastIndex() => _log.Length == 0 ? 0 : _log.Last().Index;

		private bool RequestVote(RequestVoteRpc message)
		{
			var logOk = message.LastLogTerm > LastTerm()
				|| (message.LastLogTerm == LastTerm() && message.LastLogIndex >= LastIndex());

			var grant = message.Term == _currentTerm
				&& logOk
				&& (_votedFor.HasValue == false || _votedFor.Value == message.CandidateID);

			if (grant)
				_votedFor = message.CandidateID;

			return grant;
		}


		private bool AppendEntries(AppendEntriesRpc message)
		{
			if (message.Term > _currentTerm)
				return false;

			var logOk = message.PreviousLogIndex == 0
				|| (
					message.PreviousLogIndex > 0
					&& message.PreviousLogIndex <= LastIndex()
					&& message.PreviousLogTerm == _log.Single(e => e.Index == message.PreviousLogIndex).Term
				);

			if (message.Term < _currentTerm || (message.Term == _currentTerm && Role == Types.Follower && logOk == false))
				return false;

			if (message.Term == _currentTerm && Role == Types.Candidate)
			{
				Role = Types.Follower;
				return true;
			}

			if (message.Term == _currentTerm && Role == Types.Follower && logOk)
			{
				_log = MergeChangeSets(_log, message.Entries);
				CommitIndex = Math.Min(message.LeaderCommit, _log.Last().Index);
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

		public void ForceTerm(int term)
		{
			_currentTerm = term;
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
	}
}
