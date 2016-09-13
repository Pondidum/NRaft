using System;
using NRaft.Infrastructure;

namespace NRaft.Tests.TestInfrastructure
{
	public class HeartbeatTimer : IPulseable
	{
		private readonly Action _elapsed;

		public TimeSpan MaxBetweenPulses { get; }
		public Action OnPulsed { get; set; }
		public Action OnDispose { get; set; }
		public bool WasDisposed { get; private set; }

		public HeartbeatTimer(TimeSpan maxBetweenPulses, Action elapsed)
		{
			_elapsed = elapsed;
			MaxBetweenPulses = maxBetweenPulses;
		}

		public void Pulse()
		{
			OnPulsed?.Invoke();
		}

		public void Expire() => _elapsed();

		public void Dispose()
		{
			WasDisposed = true;
			OnDispose?.Invoke();
		}
	}
}
