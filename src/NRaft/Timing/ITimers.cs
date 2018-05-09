namespace NRaft.Timing
{
	public interface ITimers
	{
		IHeart Heart { get; }
		IElectionTimeout Election { get; }
		IPulseMonitor PulseMonitor { get; }
	}
}
