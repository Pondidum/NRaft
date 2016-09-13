using System.Linq;
using NRaft.Infrastructure;
using NRaft.Storage;
using NRaft.Tests.TestInfrastructure;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests
{
	public class AcceptanceTests
	{
		[Fact]
		public void When_testing_two_nodes()
		{
			var dispatcher = new InMemoryConnector();

			var firstStore = new InMemoryStore();
			var secondStore = new InMemoryStore();

			var first = new Node(firstStore, Substitute.For<IClock>(), dispatcher, 1);
			var second = new Node(secondStore, Substitute.For<IClock>(), dispatcher, 2);

			first.AddNodeToCluster(2);
			second.AddNodeToCluster(1);

			first.BecomeCandidate();

			first.VotesGranted.ShouldBe(new[] { 1, 2 });
			first.VotesResponded.ShouldBe(new[] { 1, 2 });

			//election timeout elapses...

			first.BecomeLeader();
			first.Role.ShouldBe(Types.Leader);

			first.OnClientRequest("testing");
			first.CommitIndex.ShouldBe(0);

			first.SendAppendEntries();

			first.CommitIndex.ShouldBe(1);
			firstStore.Log.Last().Command.ShouldBe("testing");

			second.CommitIndex.ShouldBe(1);
			secondStore.Log.Last().Command.ShouldBe("testing");
		}
	}
}