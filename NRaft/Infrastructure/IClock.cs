using System;

namespace NRaft.Infrastructure
{
	public interface IClock
	{
		IDisposable CreateElectionTimeout(TimeSpan duration, Action elapsed);
		IPulseable CreateHeartbeatTimeout(TimeSpan maxBetweenPulses, Action elapsed);
	}
}
