﻿using System;
using Newtonsoft.Json;

namespace NRaft.Storage
{
	public class PersistentFileStore : IStore
	{
		public int CurrentTerm => _store.Value.CurrentTerm;
		public int? VotedFor => _store.Value.VotedFor;
		public LogEntry[] Log => _store.Value.Log;

		private readonly IFileSystem _fileSystem;
		private readonly string _path;
		private readonly Lazy<Dto> _store;

		public PersistentFileStore(IFileSystem fileSystem, string path)
		{
			_fileSystem = fileSystem;
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
			if (_fileSystem.FileExists(_path) == false)
				return new Dto();

			return JsonConvert.DeserializeObject<Dto>(_fileSystem.ReadFile(_path));
		}

		private void Write()
		{
			_fileSystem.WriteFile(_path, JsonConvert.SerializeObject(_store.Value));
		}

		private class Dto : IStoreWriter
		{
			public int CurrentTerm { get; set; }
			public int? VotedFor { get; set; }
			public LogEntry[] Log { get; set; }
		}
	}
}
