using NRaft.Storage;

namespace NRaft.Tests.Storage
{
	public class PersistentFileStoreTests
	{
		private readonly PersistentFileStore _store;

		public PersistentFileStoreTests()
		{
			_store = new PersistentFileStore();
		}
	}
}
