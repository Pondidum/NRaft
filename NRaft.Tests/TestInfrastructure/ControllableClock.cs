using System;
using NRaft.Infrastructure;

namespace NRaft.Tests.TestInfrastructure
{
	public class ControllableClock : IClock
	{
		public ElectionTimer LastElectionTimer { get; private set; }
		public HeartbeatTimer LastHeartbeatTimer { get; private set; }

		public void EndCurrentElection() => LastElectionTimer.Expire();
		public void EndCurrentHeartbeat() => LastHeartbeatTimer.Expire();

		public IDisposable CreateElectionTimeout(TimeSpan duration, Action elapsed)
		{
			return LastElectionTimer = new ElectionTimer(duration, elapsed);
		}

		public IPulseable CreateHeartbeatTimeout(TimeSpan maxBetweenPulses, Action elapsed)
		{
			return LastHeartbeatTimer = new HeartbeatTimer(maxBetweenPulses, elapsed);
		}
	}
}
