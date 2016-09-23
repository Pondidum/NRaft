using System;
using NRaft.Timing;
using NSubstitute;

namespace NRaft.Tests.TestInfrastructure
{
	public class ControllableTimers : ITimers
	{
		public IHeart Heart { get; }
		public IElectionTimeout Election { get; }
		public IPulseMonitor PulseMonitor { get; }

		private Action _beatHeart;
		private Action _endElecton;
		private Action _pulseLost;

		public ControllableTimers()
		{
			Heart = Substitute.For<IHeart>();
			Heart
				.When(h => h.ConnectTo(Arg.Any<Action>()))
				.Do(ci => _beatHeart = ci.Arg<Action>());

			Election = Substitute.For<IElectionTimeout>();
			Election
				.When(h => h.ConnectTo(Arg.Any<Action>()))
				.Do(ci => _endElecton = ci.Arg<Action>());

			PulseMonitor = Substitute.For<IPulseMonitor>();
			PulseMonitor
				.When(h => h.ConnectTo(Arg.Any<Action>()))
				.Do(ci => _pulseLost = ci.Arg<Action>());
		}

		public void BeatHeart() => _beatHeart();
		public void EndElection() => _endElecton();
		public void LoosePulse() => _pulseLost();
	}
}
