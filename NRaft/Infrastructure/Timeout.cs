using System;
using System.Threading;
using System.Threading.Tasks;

namespace NRaft.Infrastructure
{
	public class Timeout : IPulseable
	{
		private DateTime _pulsedAt;
		private Task _task;

		private readonly Action _onTimeout;
		private readonly CancellationTokenSource _background;

		public Timeout(Action onTimeout)
		{
			_onTimeout = onTimeout;
			_background = new CancellationTokenSource();

			GetTimestamp = () => DateTime.UtcNow;
			Duration = TimeSpan.FromMilliseconds(350);
			CheckInterval = TimeSpan.FromMilliseconds(50);
		}

		public Func<DateTime> GetTimestamp { get; set; }
		public TimeSpan Duration { get; set; }
		public TimeSpan CheckInterval { get; set; }

		public void Start()
		{
			_pulsedAt = GetTimestamp();

			_task = Task.Run(() =>
			{
				while (GetTimestamp().Subtract(_pulsedAt) < Duration && _background.IsCancellationRequested == false)
				{
					Task
						.Delay(CheckInterval, _background.Token)
						.Wait(_background.Token);
				}

				if (_background.IsCancellationRequested == false)
					_onTimeout();

			}, _background.Token);
		}

		public void Pulse()
		{
			_pulsedAt = GetTimestamp();
		}

		public void Dispose()
		{
			_background.Cancel();
			_background.Dispose();
			_task.Dispose();
		}
	}
}
