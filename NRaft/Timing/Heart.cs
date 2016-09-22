using System;
using System.Threading;
using System.Threading.Tasks;

namespace NRaft.Timing
{
	public class Heart : IHeart
	{
		private Action _onHeartbeat;
		private TimeSpan _interval;
		private Task _emitter;
		private CancellationTokenSource _cancellation;

		public void StartPulsing(TimeSpan interval)
		{
			if (_onHeartbeat == null)
				throw new InvalidOperationException(".ConnectTo(Action onHeartbeat) must have been called at least once.");

			_interval = interval;

			if (_emitter == null || _emitter.IsCanceled || _emitter.IsCompleted || _cancellation.IsCancellationRequested)
			{
				_cancellation?.Dispose();
				_cancellation = new CancellationTokenSource();

				_emitter?.Dispose();
				_emitter = Task.Run(() => Beat(), _cancellation.Token);
			}
		}

		public void StopPulsing()
		{
			try
			{
				_cancellation.Cancel();
				_emitter.Wait();
			}
			catch (AggregateException)
			{
			}
		}

		public void ConnectTo(Action onHeartbeat)
		{
			_onHeartbeat = onHeartbeat;
		}

		private void Beat()
		{
			while (_cancellation.IsCancellationRequested == false)
			{
				_onHeartbeat();

				Task.Delay(_interval, _cancellation.Token).Wait(_cancellation.Token);
			}
		}
	}
}
