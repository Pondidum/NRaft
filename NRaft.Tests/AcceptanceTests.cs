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

			var first = new Node(firstStore, dispatcher, 1);
			var second = new Node(secondStore, dispatcher, 2);

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
			first.AdvanceCommitIndex();

			firstStore.Log.Last().Command.ShouldBe("testing");
			secondStore.Log.Last().Command.ShouldBe("testing");

			second.CommitIndex.ShouldBe(0);

			first.SendAppendEntries();

			second.CommitIndex.ShouldBe(1);

			first.AdvanceCommitIndex();
			first.CommitIndex.ShouldBe(1);
		}
	}
}