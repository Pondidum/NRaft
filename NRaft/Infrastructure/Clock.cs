using System;

namespace NRaft.Infrastructure
{
	public class Clock : IClock
	{
		public IDisposable CreateElectionTimeout(TimeSpan duration, Action elapsed)
		{
			return new Timeout(elapsed)
			{
				Duration = duration
			};
		}

		public IPulseable CreatePulseTimeout(TimeSpan maxBetweenPulses, Action elapsed)
		{
			return new Timeout(elapsed)
			{
				Duration = maxBetweenPulses
			};
		}

		public IDisposable CreateHeartbeat(TimeSpan interval, Action elapsed)
		{
			return new Repeater(interval, elapsed);
		}
	}
}
