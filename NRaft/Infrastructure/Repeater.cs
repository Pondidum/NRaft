using System;
using System.Threading;
using System.Threading.Tasks;

namespace NRaft.Infrastructure
{
	public class Repeater : IDisposable
	{
		private readonly CancellationTokenSource _source;
		private readonly Task _task;

		public Repeater(TimeSpan interval, Action elapsed)
		{
			_source = new CancellationTokenSource();
			_task = Task.Run(() =>
			{
				while (_source.IsCancellationRequested == false)
				{
					Task.Delay(interval, _source.Token).Wait(_source.Token);
					elapsed();
				}

			}, _source.Token);
		}

		public void Dispose()
		{
			_source.Cancel();
			_task.Dispose();
		}
	}
}
