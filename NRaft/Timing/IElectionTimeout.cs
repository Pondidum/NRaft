using System;

namespace NRaft.Timing
{
	public interface IElectionTimeout
	{
		void StartElection(TimeSpan duration);

		void ConnectTo(Action onElectionOver);
	}
}
