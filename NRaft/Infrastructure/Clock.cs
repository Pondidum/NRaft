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

		public IPulseable CreateHeartbeatTimeout(TimeSpan maxBetweenPulses, Action elapsed)
		{
			return new Timeout(elapsed)
			{
				Duration = maxBetweenPulses
			};
		}
	}
}
