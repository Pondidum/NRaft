using System.Linq;
using NRaft.Infrastructure;
using NRaft.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.NodeTests
{
	public class OnClientRequestTests
	{
		private readonly InMemoryStore _store;
		private readonly Node _node;

		public OnClientRequestTests()
		{
			_store = new InMemoryStore();
			var clock = Substitute.For<IClock>();
			var dispatcher = Substitute.For<IConnector>();

			_node = new Node(_store, clock, dispatcher, 1234);
		}

		[Fact]
		public void When_the_node_is_not_the_leader()
		{
			_node.OnClientRequest(new Dto { Value = "abc" });

			_store.Log.ShouldBeEmpty();
		}

		[Fact]
		public void When_the_node_is_the_leader()
		{
			var value = new Dto { Value = "abc" };

			_node.BecomeCandidate();
			_node.BecomeLeader();

			_node.OnClientRequest(value);

			_store.Log.Single().Command.ShouldBe(value);
		}


		private class Dto
		{
			public string Value { get; set; }
		}
	}
}
