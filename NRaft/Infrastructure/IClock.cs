using System;

namespace NRaft.Infrastructure
{
	public interface IClock
	{
		IDisposable CreateTimeout(TimeSpan duration, Action elapsed);
		IPulseable CreatePulseMonitor(TimeSpan maxBetweenPulses, Action elapsed);
	}
}
