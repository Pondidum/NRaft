using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace NRaft.Timing
{
	public class ElectionTimeout : IElectionTimeout
	{
		private static readonly ILogger Log = Serilog.Log.ForContext<ElectionTimeout>();

		private TimeSpan _duration;
		private Action _onElectionOver;
		private Task _timeout;
		private CancellationTokenSource _cancellation;

		public void StartElection(TimeSpan duration)
		{
			if (_onElectionOver == null)
				throw new InvalidOperationException(".ConnectTo(Action onElectionOver) must have been called at least once.");

			Log.Information("Started Election, with a duration of {elapsed}ms", duration.TotalMilliseconds);

			_duration = duration;

			if (_cancellation != null)
			{
				try
				{
					_cancellation.Cancel();
					_timeout.Wait();
				}
				catch (AggregateException)
				{
				}

				_cancellation.Dispose();
				_timeout.Dispose();
			}

			_cancellation = new CancellationTokenSource();
			_timeout = Task.Run(() => Election(), _cancellation.Token);
		}

		public void ConnectTo(Action onElectionOver)
		{
			_onElectionOver = onElectionOver;
		}

		private void Election()
		{
			try
			{
				Task.Delay(_duration, _cancellation.Token).Wait(_cancellation.Token);

				Log.Information("Election ended");
				_onElectionOver();
			}
			catch (OperationCanceledException)
			{
			}
		}
	}
}
