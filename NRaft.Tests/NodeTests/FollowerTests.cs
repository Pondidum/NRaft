using System;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.NodeTests
{
	public class FollowerTests
	{
		private readonly IPulseable _heart;
		private readonly Node _node;

		private Action _elapsed;

		public FollowerTests()
		{
			var store = Substitute.For<IStore>();
			var clock = Substitute.For<IClock>();
			var connector = Substitute.For<IConnector>();

			_heart = Substitute.For<IPulseable>();

			clock
				.CreatePulseTimeout(Arg.Any<TimeSpan>(), Arg.Any<Action>())
				.Returns(_heart)
				.AndDoes(cb => _elapsed = cb.Arg<Action>());

			_node = new Node(store, clock, connector, 1234);
		}

		[Fact]
		public void OnAppendEntries_pulses_the_heart()
		{
			_node.OnAppendEntries(new AppendEntriesRequest());

			_heart.Received(1).Pulse();
		}

		[Fact]
		public void When_the_heart_times_out()
		{
			_elapsed();
			_node.Role.ShouldBe(Types.Candidate);
		}
	}
}
