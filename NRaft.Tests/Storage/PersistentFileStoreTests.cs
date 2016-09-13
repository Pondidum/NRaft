using NRaft.Storage;
using NRaft.Tests.TestInfrastructure;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.Storage
{
	public class PersistentFileStoreTests
	{
		private const string StorePath = "./store.json";
		private readonly PersistentFileStore _store;
		private readonly IFileSystem _fs;

		public PersistentFileStoreTests()
		{
			_fs = Substitute.For<IFileSystem>();
			_store = new PersistentFileStore(_fs, StorePath);
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
			_fs.FileExists(Arg.Any<string>()).Returns(true);
			_fs.ReadFile(Arg.Any<string>()).Returns("{}");

			var t1 = _store.CurrentTerm;
			var t2 = _store.CurrentTerm;

			_fs.Received(1).ReadFile(Arg.Any<string>());
		}

		[Fact]
		public void When_writing_all_properties()
		{
			var fs = new InMemoryFileSystem();
			var store = new PersistentFileStore(fs, StorePath);

			store.Write(write =>
			{
				write.CurrentTerm = 2;
				write.VotedFor = 14;
				write.Log = new[]
				{
					new LogEntry { Index = 1, Term = 0 },
				};
			});

			var expectedJson = "{\r\n  \"CurrentTerm\": 2,\r\n  \"VotedFor\": 14,\r\n  \"Log\": [\r\n    {\r\n      \"Index\": 1,\r\n      \"Term\": 0,\r\n      \"Command\": null\r\n    }\r\n  ]\r\n}";

			fs.ReadFile(StorePath).ShouldBe(expectedJson);
		}

		[Fact]
		public void When_writing_a_log_with_inherited_items()
		{
			var fs = new InMemoryFileSystem();
			var writeStore = new PersistentFileStore(fs, StorePath);

			writeStore.Write(write =>
			{
				write.CurrentTerm = 2;
				write.VotedFor = 14;
				write.Log = new[]
				{
					new LogEntry { Index = 1, Term = 24, Command = new Child { Value = 123} },
					new LogEntry { Index = 2, Term = 25, Command = new Sibling { Text = "abc"} },
				};
			});

			var readStore = new PersistentFileStore(fs, StorePath);

			readStore.ShouldSatisfyAllConditions(
				() => readStore.CurrentTerm.ShouldBe(2),
				() => readStore.VotedFor.ShouldBe(14),
				() => readStore.Log[0].Index.ShouldBe(1),
				() => readStore.Log[0].Term.ShouldBe(24),
				() => readStore.Log[0].Command.ShouldBeOfType<Child>(),
				() => readStore.Log[1].Index.ShouldBe(2),
				() => readStore.Log[1].Term.ShouldBe(25),
				() => readStore.Log[1].Command.ShouldBeOfType<Sibling>()
			);
		}

		private class Parent { }
		private class Child : Parent { public int Value { get; set; } }
		private class Sibling : Parent { public string Text { get; set; } }

	}
}
