using System;

namespace NRaft.Storage
{
	public interface IStore
	{
		int CurrentTerm { get; }
		int? VotedFor { get; }
		LogEntry[] Log { get; }

		void Write(Action<IStoreWriter> write);
	}

	public interface IStoreWriter
	{
		int CurrentTerm { set; }
		int? VotedFor { set; }
		LogEntry[] Log { set; }
	}
}
