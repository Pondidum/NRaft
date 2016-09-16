using System;

namespace NRaft
{
	public class Timeouts
	{
		private static readonly Random Random = new Random();

		public static TimeSpan GetMaxPulseInterval()
		{
			return TimeSpan.FromMilliseconds(Random.Next(150, 300));
		}

		public static TimeSpan GetElectionTimeout()
		{
			return TimeSpan.FromMilliseconds(Random.Next(150, 300));
		}

		public static TimeSpan GetHeartRate()
		{
			return TimeSpan.FromMilliseconds(100);
		}
	}
}
