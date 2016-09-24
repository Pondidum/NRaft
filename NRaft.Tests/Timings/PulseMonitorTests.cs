using System;
using System.Threading.Tasks;
using NRaft.Timing;
using Shouldly;
using Xunit;

namespace NRaft.Tests.Timings
{
	public class PulseMonitorTests
	{
		private readonly IPulseMonitor _monitor;

		public PulseMonitorTests()
		{
			_monitor = new PulseMonitor();
		}

		[Fact]
		public void When_started_and_not_connected()
		{
			Should.Throw<InvalidOperationException>(() => _monitor.StartMonitoring(TimeSpan.FromMilliseconds(10)));
		}

		[Fact]
		public async Task When_started_and_no_pulses_are_received()
		{
			var pulsesLost = 0;
			_monitor.ConnectTo(() => pulsesLost++);

			_monitor.StartMonitoring(TimeSpan.FromMilliseconds(50));

			await Task.Delay(75);

			pulsesLost.ShouldBe(1);
		}

		[Fact]
		public async Task When_started_and_pulses_are_received()
		{
			var pulsesLost = 0;
			_monitor.ConnectTo(() => pulsesLost++);

			_monitor.StartMonitoring(TimeSpan.FromMilliseconds(50));

			await Task.Delay(40);
			_monitor.Pulse();

			await Task.Delay(40);
			_monitor.Pulse();

			pulsesLost.ShouldBe(0);
		}

		[Fact]
		public async Task When_started_and_stopped()
		{
			var pulsesLost = 0;
			_monitor.ConnectTo(() => pulsesLost++);

			_monitor.StartMonitoring(TimeSpan.FromMilliseconds(50));

			await Task.Delay(40);
			_monitor.StopMonitoring();

			await Task.Delay(40);

			pulsesLost.ShouldBe(0);
		}

		[Fact]
		public void When_a_non_started_monitor_is_stopped()
		{
			Should.NotThrow(() => _monitor.StopMonitoring());
		}

		[Fact]
		public async Task When_a_stopped_monitor_is_started_again()
		{
			var pulsesLost = 0;
			_monitor.ConnectTo(() => pulsesLost++);

			_monitor.StartMonitoring(TimeSpan.FromMilliseconds(50));

			await Task.Delay(25);
			_monitor.StopMonitoring();

			await Task.Delay(40);
			_monitor.StartMonitoring(TimeSpan.FromMilliseconds(50));

			await Task.Delay(75);

			pulsesLost.ShouldBe(1);
		}

		[Fact]
		public async Task When_a_monitor_is_stopped_within_its_callback()
		{
			var pre = 0;
			var post = 0;

			_monitor.ConnectTo(() =>
			{
				pre++;
				_monitor.StopMonitoring();
				post++;
			});

			_monitor.StartMonitoring(TimeSpan.FromMilliseconds(30));

			await Task.Delay(TimeSpan.FromMilliseconds(50));

			pre.ShouldBe(1);
			post.ShouldBe(1);
		}
	}
}
