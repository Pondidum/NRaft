using System;
using System.IO;
using Newtonsoft.Json;

namespace NRaft.Storage
{
	public class PersistentFileStore : IStore
	{
		public int CurrentTerm => _store.Value.CurrentTerm;
		public int? VotedFor => _store.Value.VotedFor;
		public LogEntry[] Log => _store.Value.Log;

		private readonly string _path;
		private readonly Lazy<Dto> _store;

		public PersistentFileStore(string path)
		{
			_path = path;
			_store = new Lazy<Dto>(FromDisk);
		}

		public void Write(Action<IStoreWriter> apply)
		{
			apply(_store.Value);
			Write();
		}

		private Dto FromDisk()
		{
			return JsonConvert.DeserializeObject<Dto>(File.ReadAllText(_path));
		}

		private void Write()
		{
			File.WriteAllText(_path, JsonConvert.SerializeObject(_store.Value));
		}

		private class Dto : IStoreWriter
		{
			public int CurrentTerm { get; set; }
			public int? VotedFor { get; set; }
			public LogEntry[] Log { get; set; }
		}
	}
}
