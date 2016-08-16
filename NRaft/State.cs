using System;
using System.Collections.Generic;
using System.Linq;

namespace NRaft
{
	public interface IDispatcher
	{
		void SendReply(AppendEntriesResponse message);
	}

	public class State
	{
		private readonly IDispatcher _dispatcher;
		private readonly int _nodeID;

		//need to be persistent store
		private int _currentTerm;
		private int? _votedFor;
		private List<LogEntry> _log;

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
			_log = new List<LogEntry>();

			CommitIndex = 0;
			_lastApplied = 0;
		}

		public IEnumerable<LogEntry> Log => _log;

		public void ForceTerm(int term)
		{
			_currentTerm = term;
		}

		public void OnAppendEntries(AppendEntriesRpc message)
		{
			if (message.Term < _currentTerm)
				_dispatcher.SendReply(new AppendEntriesResponse { Success = false, Term = _currentTerm });

			if (_log.Count - 1 < message.PreviousLogIndex)
				_dispatcher.SendReply(new AppendEntriesResponse { Success = false, Term = _currentTerm });

			if (_log[message.PreviousLogIndex].Term != message.Term)
				_dispatcher.SendReply(new AppendEntriesResponse { Success = false, Term = _currentTerm });

			//get all new items where the index is already in the log
			//check they don't conflict, delete if they do

			var firstBroken = message
				.Entries
				.Where(pair => pair.Key < _log.Count)
				.OrderBy(e => e.Key)
				.Where(pair => pair.Value.Term != _log[pair.Key].Term)
				.Select(pair => pair.Key)
				.ToArray();

			if (firstBroken.Any())
			{
				_log.RemoveRange(firstBroken.First(),_log.Count - firstBroken.First());
			}

			var remaining = message
				.Entries
				.Where(pair => pair.Key >= _log.Count)
				.OrderBy(pair => pair.Key)
				.Select(pair => pair.Value);

			_log.AddRange(remaining);

			if (message.LeaderCommit > CommitIndex)
				CommitIndex = Math.Min(message.LeaderCommit, _log.Count - 1);
		}

		public void ForceLog(params LogEntry[] log)
		{
			_log.Clear();
			_log.AddRange(log);
		}

		public void ForceCommitIndex(int index)
		{
			CommitIndex = index;
		}
	}

	public class LogEntry : IEquatable<LogEntry>
	{
		public string Command { get; set; }
		public int Term { get; set; }

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((LogEntry)obj);
		}

		public bool Equals(LogEntry other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Term == other.Term;
		}

		public override int GetHashCode()
		{
			return Term;
		}

		public static bool operator ==(LogEntry left, LogEntry right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(LogEntry left, LogEntry right)
		{
			return !Equals(left, right);
		}
	}

	public class AppendEntriesRpc
	{
		public int Term { get; set; }
		public int LeaderID { get; set; }

		public int PreviousLogIndex { get; set; }
		public int PreviousLogTerm { get; set; }

		public Dictionary<int, LogEntry> Entries { get; set; }
		public int LeaderCommit { get; set; }

		public AppendEntriesRpc()
		{
			Entries = new Dictionary<int, LogEntry>();
		}
	}

	public class AppendEntriesResponse
	{
		public int Term { get; set; }
		public bool Success { get; set; }
	}
}