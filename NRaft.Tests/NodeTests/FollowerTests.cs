using System;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NRaft.Tests.TestInfrastructure;
using NRaft.Timing;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.NodeTests
{
	public class FollowerTests
	{
		private readonly Node _node;
		private readonly ControllableTimers _timers;

		public FollowerTests()
		{
			var store = Substitute.For<IStore>();
			_timers = new ControllableTimers();
			var connector = Substitute.For<IConnector>();
			
			_node = new Node(store, _timers, connector, 1234);
		}

		[Fact]
		public void OnAppendEntries_pulses_the_pulse()
		{
			_node.OnAppendEntries(new AppendEntriesRequest());

			_timers.PulseMonitor.Received(1).Pulse();
		}

		[Fact]
		public void When_the_pulse_times_out()
		{
			_timers.LoosePulse();
			_node.Role.ShouldBe(Types.Candidate);
		}
	}
}
