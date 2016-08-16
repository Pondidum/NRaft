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

		public void OnAppendEntries(AppendEntriesRpc message)
		{
			if (message.Term < _currentTerm)
				_dispatcher.SendReply(new AppendEntriesResponse { Success = false, Term = _currentTerm });

			else if (_log.Any(e => e.Index == message.PreviousLogIndex) == false)
				_dispatcher.SendReply(new AppendEntriesResponse { Success = false, Term = _currentTerm });

			else if (_log.Single(e => e.Index == message.PreviousLogIndex).Term != message.Term)
				_dispatcher.SendReply(new AppendEntriesResponse { Success = false, Term = _currentTerm });

			else
			{
				_log = MergeChangeSets(_log, message.Entries);


				if (message.LeaderCommit > CommitIndex)
					CommitIndex = Math.Min(message.LeaderCommit, _log.Last().Index);
			}
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
	}

	public class LogEntry : IEquatable<LogEntry>
	{
		public int Index { get; set; }
		public int Term { get; set; }
		public string Command { get; set; }

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
			return Index == other.Index && Term == other.Term;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Index * 397) ^ Term;
			}
		}

		public override string ToString()
		{
			return $"Index {Index}, Term {Term}";
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

		public LogEntry[] Entries { get; set; }
		public int LeaderCommit { get; set; }

		public AppendEntriesRpc()
		{
			Entries = Enumerable.Empty<LogEntry>().ToArray();
		}
	}

	public class AppendEntriesResponse
	{
		public int Term { get; set; }
		public bool Success { get; set; }
	}
}