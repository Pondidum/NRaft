using System;
using NRaft.Infrastructure;

namespace NRaft.Tests.TestInfrastructure
{
	public class ControllableClock : IClock
	{
		private Action<ElectionTimer> _onElectionTimerCreated;
		private Action<HeartbeatTimer> _onHeartbeatTimerCreated;

		public void OnElectionTimeoutCreated(Action<ElectionTimer> callback)
		{
			_onElectionTimerCreated = callback;
		}

		public void OnHeartbeatTimeoutCreated(Action<HeartbeatTimer> callback)
		{
			_onHeartbeatTimerCreated = callback;
		}

		public IDisposable CreateElectionTimeout(TimeSpan duration, Action elapsed)
		{
			var timer = new ElectionTimer(duration, elapsed);
			_onElectionTimerCreated?.Invoke(timer);

			return timer;
		}

		public IPulseable CreateHeartbeatTimeout(TimeSpan maxBetweenPulses, Action elapsed)
		{
			var timer = new HeartbeatTimer(maxBetweenPulses, elapsed);
			_onHeartbeatTimerCreated?.Invoke(timer);

			return timer;
		}
	}
}
