using System;

namespace NRaft.Timing
{
	public interface IHeart
	{
		void StartPulsing(TimeSpan interval);
		void StopPulsing();

		void ConnectTo(Action onHeartbeat);
	}
}
