﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace NRaft.Infrastructure
{
	public class Timeout : IPulseable
	{
		private DateTime _pulsedAt;

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

				if (_background.IsCancellationRequested == false)
					_onTimeout();

			}, _background.Token);

		}

		public Func<DateTime> GetTimestamp { get; set; }
		public TimeSpan CheckInterval { get; set; }

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
