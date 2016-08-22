using System.Linq;

namespace NRaft
{
	public class AppendEntriesRpc
	{
		public int Term { get; set; }
		public int LeaderID { get; set; }

		public int PreviousLogIndex { get; set; }
		public int PreviousLogTerm { get; set; }

		public LogEntry[] Entries { get; set; }
		public int LeaderCommit { get; set; }

		public int RecipientID { get; set; }

		public AppendEntriesRpc()
		{
			Entries = Enumerable.Empty<LogEntry>().ToArray();
		}
	}
}
