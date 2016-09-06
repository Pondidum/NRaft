using System;

namespace NRaft.Infrastructure
{
	public class Clock : IClock
	{
		public IDisposable CreateTimeout(TimeSpan duration, Action elapsed)
		{
			return new Timeout(elapsed)
			{
				Duration = duration
			};
		}

		public IPulseable CreatePulseMonitor(TimeSpan maxBetweenPulses, Action elapsed)
		{
			return new Timeout(elapsed)
			{
				Duration = maxBetweenPulses
			};
		}
	}
}
