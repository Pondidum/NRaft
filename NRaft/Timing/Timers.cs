namespace NRaft.Timing
{
	public class Timers : ITimers
	{
		public IHeart Heart { get; }
		public IElectionTimeout Election { get; }
		public IPulseMonitor PulseMonitor { get; }

		public Timers()
		{
			Heart = new Heart();
			Election = new ElectionTimeout();
			PulseMonitor = new PulseMonitor();
		}
	}
}
