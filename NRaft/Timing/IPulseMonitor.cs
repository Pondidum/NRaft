using System;

namespace NRaft.Timing
{
	public interface IPulseMonitor
	{
		void StartMonitoring(TimeSpan duration);
		void StopMonitoring();
		void Pulse();

		void ConnectTo(Action onPulseLost);
	}
}
