using System;
using System.Collections.Generic;
using System.Linq;

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

			CommitIndex = 0;
			_lastApplied = 0;
		}

		public IEnumerable<LogEntry> Log => _log;

		public void ForceTerm(int term)
		{
			_currentTerm = term;
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

		private bool RequestVote(RequestVoteRpc message)
		{
			if (message.Term < _currentTerm)
				return false;

			if (_log.Any() && message.LastLogIndex != _log.Last().Index)
				return false;

			if (_votedFor.HasValue && _votedFor.Value != message.CandidateID)
				return false;

			_votedFor = message.CandidateID;
			return true;
		}

		public void OnAppendEntries(AppendEntriesRpc message)
		{
			var success = AppendEntries(message);

			_dispatcher.SendReply(new AppendEntriesResponse
			{
				Success = success,
				Term = _currentTerm
			});
		}

		private bool AppendEntries(AppendEntriesRpc message)
		{
			if (message.Term < _currentTerm)
				return false;

			if (_log.Any(e => e.Index == message.PreviousLogIndex) == false)
				return false;

			if (_log.Single(e => e.Index == message.PreviousLogIndex).Term != message.Term)
				return false;

			_log = MergeChangeSets(_log, message.Entries);

			if (message.LeaderCommit > CommitIndex)
				CommitIndex = Math.Min(message.LeaderCommit, _log.Last().Index);

			return true;
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
	}
}
