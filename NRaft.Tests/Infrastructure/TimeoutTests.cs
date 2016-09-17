using System;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Timeout = NRaft.Infrastructure.Timeout;

namespace NRaft.Tests.Infrastructure
{
	public class TimeoutTests
	{
		[Fact]
		public void When_a_timeout_is_created()
		{
			var reset = new AutoResetEvent(false);
			var triggered = false;

			var timeout = new Timeout(TimeSpan.FromMilliseconds(100), () =>
			{
				triggered = true;
				reset.Set();
			});

			reset.WaitOne(TimeSpan.FromMilliseconds(200));
			triggered.ShouldBe(true);
		}

		[Fact]
		public void When_a_timeout_is_pulsed()
		{
			var reset = new AutoResetEvent(false);
			var triggered = false;

			var timeout = new Timeout(TimeSpan.FromMilliseconds(100), () =>
			{
				triggered = true;
				reset.Set();
			});

			Task.Delay(TimeSpan.FromMilliseconds(80)).ContinueWith(t => timeout.Pulse());
			Task.Delay(TimeSpan.FromMilliseconds(160)).ContinueWith(t => timeout.Pulse());

			reset.WaitOne(TimeSpan.FromMilliseconds(200));
			triggered.ShouldBe(false);
		}

		[Fact]
		public void When_a_timeout_is_disposed_in_its_timeout()
		{
			var triggered = false;
			var reset = new AutoResetEvent(false);

			Timeout timeout = null;

			timeout = new Timeout(TimeSpan.FromMilliseconds(50), () =>
			{
				timeout.Dispose();
				triggered = true;
				reset.Set();
			});

			reset.WaitOne(TimeSpan.FromMilliseconds(100));
			triggered.ShouldBe(true);
		}
	}
}
