using System;

namespace NRaft.Storage
{
	public class InMemoryStore : IStore, IStoreWriter
	{
		public int CurrentTerm { get; set; }
		public int? VotedFor { get; set; }
		public LogEntry[] Log { get; set; }

		public InMemoryStore()
		{
			Log = new LogEntry[0];
		}

		public void Write(Action<IStoreWriter> write)
		{
			write(this);
		}
	}
}
