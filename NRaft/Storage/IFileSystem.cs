namespace NRaft.Storage
{
	public interface IFileSystem
	{
		void WriteFile(string path, string contents);
		string ReadFile(string path);
	}
}
