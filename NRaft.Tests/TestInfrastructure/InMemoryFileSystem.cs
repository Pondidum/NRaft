using System;
using System.Collections.Generic;
using System.IO;
using NRaft.Storage;

namespace NRaft.Tests.TestInfrastructure
{
	public class InMemoryFileSystem : IFileSystem
	{
		private readonly Dictionary<string, string> _files;

		public InMemoryFileSystem()
		{
			_files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}

		public bool FileExists(string path)
		{
			return _files.ContainsKey(path?.Trim());
		}

		public void WriteFile(string path, string contents)
		{
			_files[path.Trim()] = contents;
		}

		public string ReadFile(string path)
		{
			if (FileExists(path))
				return _files[path.Trim()];

			throw new FileNotFoundException("File not found.", path);
		}
	}
}
