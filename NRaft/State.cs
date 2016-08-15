using System.Collections.Generic;

namespace NRaft
{
	public class State
	{
		private readonly int _nodeID;

		//need to be persistent store
		private int _currentTerm;
		private int? _votedFor;
		private List<LogEntry> _log;

		//only in memory
		private int _commitIndex;
		private int _lastApplied;

		//leader-only state - perhaps refactor to leaderState
		private int _nextIndex;
		private int _matchIndex;

		public State(int nodeID)
		{
			_nodeID = nodeID;
			_currentTerm = 0;
			_votedFor = null;
			_log = new List<LogEntry>();

			_commitIndex = 0;
			_lastApplied = 0;
		}
	}

	public class LogEntry
	{
		public string Command { get; set; }
		public int Term { get; set; }
	}

	public class AppendEntriesRpc
	{
		public int Term { get; set; }
		public int LeaderID { get; set; }

		public int PreviousLogIndex { get; set; }
		public int PreviousLogTerm { get; set; }

		public LogEntry[] Entries { get; set; }
		public int LeaderCommit { get; set; }
	}

	public class AppendEntriesResponse
	{
		public int Term { get; set; }
		public bool Success { get; set; }
	}
}