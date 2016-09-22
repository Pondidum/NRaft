using System;

namespace NRaft.Timing
{
	public interface IHeart
	{
		void StartPulsing(TimeSpan interval);
		void StopPulsing();

		void ConnectTo(Action onHeartbeat);
	}

	public interface IPulseMonitor
	{
		void StartMonitoring(TimeSpan duration);
		void StopMonitoring();

		void ConnectTo(Action onPulseLost);
	}
}
