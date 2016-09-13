using System;

namespace NRaft.Tests.TestInfrastructure
{
	public class ElectionTimer : IDisposable
	{

		public TimeSpan Duration { get; }
		public Action OnExpire { get; }
		public Action OnDispose { get; set; }

		public ElectionTimer(TimeSpan duration, Action elapsed)
		{
			Duration = duration;
			OnExpire = elapsed;
		}

		public void Dispose()
		{
			OnDispose?.Invoke();
		}
	}
}
