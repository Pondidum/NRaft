using NRaft.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.Storage
{
	public class PersistentFileStoreTests
	{
		private readonly PersistentFileStore _store;
		private readonly IFileSystem _fs;

		public PersistentFileStoreTests()
		{
			_fs = Substitute.For<IFileSystem>();
			_store = new PersistentFileStore(_fs, "./store.json");
		}

		[Fact]
		public void When_creating_an_instance_the_fs_is_not_hit()
		{
			_fs.DidNotReceive().ReadFile(Arg.Any<string>());
		}

		[Fact]
		public void When_no_file_exists_and_reading_current_term()
		{
			_store.CurrentTerm.ShouldBe(0);
		}

		[Fact]
		public void When_no_file_exists_and_reading_voted_for()
		{
			_store.VotedFor.ShouldBeNull();
		}

		[Fact]
		public void When_no_file_exists_and_reading_log()
		{
			_store.Log.ShouldBeEmpty();
		}

		[Fact]
		public void When_a_property_is_read_multiple_times()
		{
			var t1 = _store.CurrentTerm;
			var t2 = _store.CurrentTerm;

			_fs.Received(1).ReadFile(Arg.Any<string>());

		}
	}
}
