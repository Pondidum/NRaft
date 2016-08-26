using System.Linq;
using NRaft.Infrastructure;
using NRaft.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class OnClientRequestTests
	{
		private readonly State _state;

		public OnClientRequestTests()
		{
			var dispatcher = Substitute.For<IConnector>();
			var store = new InMemoryStore();

			_state = new State(store, dispatcher, 1234);
		}

		[Fact]
		public void When_the_node_is_not_the_leader()
		{
			_state.OnClientRequest(new Dto { Value = "abc" });

			_state.Log.ShouldBeEmpty();
		}

		[Fact]
		public void When_the_node_is_the_leader()
		{
			var value = new Dto { Value = "abc" };

			_state.ForceType(Types.Leader);
			_state.OnClientRequest(value);

			_state.Log.Single().Command.ShouldBe(value);
		}


		private class Dto
		{
			public string Value { get; set; }
		}
	}
}
