using System;

namespace NRaft.Tests.TestInfrastructure
{
	public class ElectionTimer : IDisposable
	{
		private readonly Action _elapsed;

		public TimeSpan Duration { get; }
		public Action OnDispose { get; set; }
		public bool WasDisposed { get; private set; }

		public ElectionTimer(TimeSpan duration, Action elapsed)
		{
			_elapsed = elapsed;
			Duration = duration;
		}

		public void Expire() => _elapsed();

		public void Dispose()
		{
			WasDisposed = true;
			OnDispose?.Invoke();
		}
	}
}
