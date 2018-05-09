namespace NRaft.Storage
{
	public interface IFileSystem
	{
		bool FileExists(string path);
		void WriteFile(string path, string contents);
		string ReadFile(string path);
	}
}
