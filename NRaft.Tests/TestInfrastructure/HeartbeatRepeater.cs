using System;

namespace NRaft.Tests.TestInfrastructure
{
	public class HeartbeatRepeater : IDisposable
	{
		private readonly Action _elapsed;

		public TimeSpan Interval { get; }
		public Action OnDispose { get; set; }
		public bool WasDisposed { get; private set; }

		public HeartbeatRepeater(TimeSpan interval, Action elapsed)
		{
			_elapsed = elapsed;
			Interval = interval;
		}

		public void Trigger()
		{
			_elapsed();
		}

		public void Dispose()
		{
			WasDisposed = true;
			OnDispose?.Invoke();
		}
	}
}
