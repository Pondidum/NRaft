using System;

namespace NRaft.Infrastructure
{
	public class Clock : IClock
	{
		public IDisposable CreateElectionTimeout(TimeSpan duration, Action elapsed)
		{
			return new Timeout(duration, elapsed);
		}

		public IPulseable CreatePulseTimeout(TimeSpan maxBetweenPulses, Action elapsed)
		{
			return new Timeout(maxBetweenPulses, elapsed);
		}

		public IDisposable CreateHeartbeat(TimeSpan interval, Action elapsed)
		{
			return new Repeater(interval, elapsed);
		}
	}
}
