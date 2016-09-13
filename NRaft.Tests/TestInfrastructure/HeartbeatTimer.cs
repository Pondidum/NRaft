using System;
using NRaft.Infrastructure;

namespace NRaft.Tests.TestInfrastructure
{
	public class HeartbeatTimer : IPulseable
	{
		public TimeSpan MaxBetweenPulses { get; }
		public Action OnExpire { get; }
		public Action OnPulsed { get; set; }
		public Action OnDispose { get; set; }

		public HeartbeatTimer(TimeSpan maxBetweenPulses, Action onExpire)
		{
			MaxBetweenPulses = maxBetweenPulses;
			OnExpire = onExpire;
		}

		public void Pulse()
		{
			OnPulsed?.Invoke();
		}

		public void Dispose()
		{
			OnDispose?.Invoke();
		}
	}
}
