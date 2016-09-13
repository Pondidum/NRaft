using System;
using NRaft.Infrastructure;

namespace NRaft.Tests.TestInfrastructure
{
	public class ControllableClock : IClock
	{
		private ElectionTimer _lastElectionTimerCreated;
		private HeartbeatTimer _lastHeartbeatTimerCreated;

		public void EndCurrentElection() => _lastElectionTimerCreated.Expire();
		public void EndCurrentHeartbeat() => _lastHeartbeatTimerCreated.Expire();

		public IDisposable CreateElectionTimeout(TimeSpan duration, Action elapsed)
		{
			return _lastElectionTimerCreated = new ElectionTimer(duration, elapsed);
		}

		public IPulseable CreateHeartbeatTimeout(TimeSpan maxBetweenPulses, Action elapsed)
		{
			return _lastHeartbeatTimerCreated = new HeartbeatTimer(maxBetweenPulses, elapsed);
		}
	}
}
