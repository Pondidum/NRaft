using System;

namespace NRaft.Timing
{
	public interface IHeart
	{
		void StartPulsing(TimeSpan interval);
		void StopPulsing();

		void ConnectTo(Action onHeartbeat);
	}

	public interface IElectionTimeout
	{
		void StartElection(TimeSpan duration);

		void ConnectTo(Action onElectionOver);
	}

	public interface IPulseMonitor
	{
		void StartMonitoring(TimeSpan duration);
		void StopMonitoring();

		void ConnectTo(Action onPulseLost);
	}
}
