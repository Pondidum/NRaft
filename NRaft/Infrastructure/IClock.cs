using System;

namespace NRaft.Infrastructure
{
	public interface IClock
	{
		IDisposable CreateElectionTimeout(TimeSpan duration, Action elapsed);
		IPulseable CreatePulseTimeout(TimeSpan maxBetweenPulses, Action elapsed);
		IDisposable CreateHeartbeat(TimeSpan interval, Action elapsed);
	}
}
