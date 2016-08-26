using System.Linq;
using NRaft.Storage;
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

			var first = new State(firstStore, dispatcher, 1);
			var second = new State(secondStore, dispatcher, 2);

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

			first.AdvanceCommitIndex();
			first.CommitIndex.ShouldBe(0);

			first.SendAppendEntries();
			firstStore.Log.Last().Command.ShouldBe("testing");
			secondStore.Log.Last().Command.ShouldBe("testing");

			first.AdvanceCommitIndex();
			first.CommitIndex.ShouldBe(1);
		}
	}
}