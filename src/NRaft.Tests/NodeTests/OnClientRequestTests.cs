﻿using System.Linq;
using NRaft.Infrastructure;
using NRaft.Storage;
using NRaft.Tests.TestInfrastructure;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.NodeTests
{
	public class OnClientRequestTests
	{
		private readonly InMemoryStore _store;
		private readonly Node _node;
		private readonly ControllableTimers _timers;

		public OnClientRequestTests()
		{
			_store = new InMemoryStore();
			_timers = new ControllableTimers();
			var dispatcher = Substitute.For<IConnector>();

			_node = new Node(_store, _timers, dispatcher, 1234);
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

			_timers.LoosePulse();
			_timers.EndElection();

			_node.OnClientRequest(value);

			_store.Log.Single().Command.ShouldBe(value);
		}


		private class Dto
		{
			public string Value { get; set; }
		}
	}
}
