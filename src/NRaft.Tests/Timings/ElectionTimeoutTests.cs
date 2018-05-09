using System;
using System.Threading.Tasks;
using NRaft.Timing;
using Shouldly;
using Xunit;

namespace NRaft.Tests.Timings
{
	public class ElectionTimeoutTests
	{
		private readonly IElectionTimeout _election;

		public ElectionTimeoutTests()
		{
			_election = new ElectionTimeout();
		}

		[Fact]
		public void When_started_and_not_connected()
		{
			Should.Throw<InvalidOperationException>(() => _election.StartElection(TimeSpan.FromMilliseconds(10)));
		}

		[Fact]
		public async Task When_started_twice_the_second_election_is_the_one_which_runs()
		{
			var endings = 0;
			_election.ConnectTo(() => endings++);

			_election.StartElection(TimeSpan.FromMilliseconds(50));

			await Task.Delay(25);

			_election.StartElection(TimeSpan.FromMilliseconds(50));

			await Task.Delay(40);

			endings.ShouldBe(0);

			await Task.Delay(25);

			endings.ShouldBe(1);
		}

		[Fact]
		public async Task When_started_after_expiry()
		{
			var endings = 0;
			_election.ConnectTo(() => endings++);

			_election.StartElection(TimeSpan.FromMilliseconds(50));

			await Task.Delay(75);
			endings.ShouldBe(1);

			_election.StartElection(TimeSpan.FromMilliseconds(50));

			await Task.Delay(75);
			endings.ShouldBe(2);
		}
	}
}
