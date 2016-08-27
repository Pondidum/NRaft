﻿using System;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NSubstitute;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class StateTests
	{
		[Fact]
		public void When_disposing_all_listeners_are_removed()
		{
			var store = new InMemoryStore();
			var connector = Substitute.For<IConnector>();

			using (var node = new State(store, connector, 1))
			{
			}

			connector.Received(1).Deregister(1, Arg.Any<Action<AppendEntriesRequest>>());
			connector.Received(1).Deregister(1, Arg.Any<Action<AppendEntriesResponse>>());
			connector.Received(1).Deregister(1, Arg.Any<Action<RequestVoteRequest>>());
			connector.Received(1).Deregister(1, Arg.Any<Action<RequestVoteResponse>>());
		}
	}
}
