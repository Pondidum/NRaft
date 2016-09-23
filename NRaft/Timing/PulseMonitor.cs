using System;
using System.Threading;
using System.Threading.Tasks;

namespace NRaft.Timing
{
	public class PulseMonitor : IPulseMonitor
	{
		private Action _onPulseLost;
		private TimeSpan _duration;
		private CancellationTokenSource _cancellation;
		private Task _monitor;
		private DateTime _lastPulse;

		public void StartMonitoring(TimeSpan duration)
		{
			if (_onPulseLost == null)
				throw new InvalidOperationException(".ConnectTo(Action onPulseLost) must have been called at least once.");

			_duration = duration;
			_cancellation = new CancellationTokenSource();

			_monitor = Task.Run(() =>
			{
				Pulse();
				Monitor();
			}, _cancellation.Token);
		}

		public void StopMonitoring()
		{
			try
			{
				_cancellation?.Cancel();
				_monitor?.Wait();

				_cancellation?.Dispose();
				_monitor?.Dispose();
			}
			catch (AggregateException)
			{
			}
		}

		public void Pulse()
		{
			_lastPulse = DateTime.UtcNow;
		}

		public void ConnectTo(Action onPulseLost)
		{
			_onPulseLost = onPulseLost;
		}

		private void Monitor()
		{
			while (DateTime.UtcNow.Subtract(_lastPulse) < _duration && _cancellation.IsCancellationRequested == false)
			{
				Task
					.Delay(TimeSpan.FromMilliseconds(10), _cancellation.Token)
					.Wait(_cancellation.Token);
			}

			if (_cancellation.IsCancellationRequested == false)
				_onPulseLost();
		}
	}
}
