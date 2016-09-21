using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace NRaft.Infrastructure
{
	public class Timeout : IPulseable
	{
		private static readonly ILogger Log = Serilog.Log.ForContext<Timeout>();

		private DateTime _pulsedAt;
		private bool _disposed;

		private readonly TimeSpan _duration;
		private readonly Action _onTimeout;
		private readonly CancellationTokenSource _background;
		private readonly Task _task;

		public Timeout(TimeSpan duration, Action onTimeout)
		{
			_duration = duration;
			_onTimeout = onTimeout;
			_background = new CancellationTokenSource();

			GetTimestamp = () => DateTime.UtcNow;
			CheckInterval = TimeSpan.FromMilliseconds(50);

			_pulsedAt = GetTimestamp();

			_task = Task.Run(() =>
			{
				while (GetTimestamp().Subtract(_pulsedAt) < _duration && _background.IsCancellationRequested == false)
				{
					Task
						.Delay(CheckInterval, _background.Token)
						.Wait(_background.Token);
				}
			}, _background.Token);

			_task.ContinueWith(t =>
			{
				if (t.IsCanceled)
				{
					Log.Debug("Timer was cancelled");
					return;
				}


				Log.Debug("Timer timed out");
				_onTimeout();
			});

			Log.Information("Timer created, duration is {duration}ms", duration.TotalMilliseconds);
		}

		public Func<DateTime> GetTimestamp { get; set; }
		public TimeSpan CheckInterval { get; set; }

		public void Pulse()
		{
			var newPulse = GetTimestamp();

			Log.Debug("Timeout Pulsed, time since previous pulse {elapsed}ms", newPulse.Subtract(_pulsedAt).TotalMilliseconds);

			_pulsedAt = newPulse;
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			Log.Information("Stopping timer");
			_disposed = true;

			try
			{
				_background.Cancel();
				_background.Dispose();
				_task.Wait();
				_task.Dispose();
			}
			catch (AggregateException)
			{
			}
		}
	}
}
