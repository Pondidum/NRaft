using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace NRaft.Timing
{
	public class PulseMonitor : IPulseMonitor
	{
		private static readonly ILogger Log = Serilog.Log.ForContext<PulseMonitor>();

		private Action _onPulseLost;
		private TimeSpan _duration;
		private CancellationTokenSource _cancellation;
		private Task _monitor;
		private DateTime _lastPulse;

		public void StartMonitoring(TimeSpan duration)
		{
			if (_onPulseLost == null)
				throw new InvalidOperationException(".ConnectTo(Action onPulseLost) must have been called at least once.");

			Log.Information("Started monitoring for pulses, with a max interval of {elapsed}ms", duration.TotalMilliseconds);
			Stop();

			_duration = duration;
			_cancellation = new CancellationTokenSource();
			_monitor = Task.Run(() =>
			{
				_lastPulse = DateTime.UtcNow;
				Monitor();
			}, _cancellation.Token);

			_monitor.ContinueWith(InvokeCallback);
		}

		public void StopMonitoring()
		{
			Stop();
			Log.Information("Stopped monitoring for pulses");
		}

		private void Stop()
		{
			try
			{
				_cancellation?.Cancel();
				_monitor?.Wait();
			}
			catch (AggregateException)
			{
			}

			_cancellation?.Dispose();
			_monitor?.Dispose();

			_cancellation = null;
			_monitor = null;
		}

		public void Pulse()
		{
			var nextStamp = DateTime.UtcNow;
			Log.Information("Received pulse ater {elapsed} ms", (nextStamp - _lastPulse).TotalMilliseconds);

			_lastPulse = nextStamp;
		}

		public void ConnectTo(Action onPulseLost)
		{
			_onPulseLost = onPulseLost;
		}

		private void Monitor()
		{
			try
			{
				while (DateTime.UtcNow.Subtract(_lastPulse) < _duration && _cancellation.IsCancellationRequested == false)
				{
					Task
						.Delay(TimeSpan.FromMilliseconds(10), _cancellation.Token)
						.Wait(_cancellation.Token);
				}
			}
			catch (OperationCanceledException)
			{
			}
		}

		private void InvokeCallback(Task t)
		{
			if (t.IsCanceled)
				return;

			Log.Information("Pulse lost, last pulse was {elapsed}ms ago", (DateTime.UtcNow - _lastPulse).TotalMilliseconds);
			_onPulseLost();
		}
	}
}
