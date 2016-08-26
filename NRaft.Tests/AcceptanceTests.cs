using System.Linq;
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

			var first = new State(dispatcher, 1);
			var second = new State(dispatcher, 2);

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
			first.Log.Last().Command.ShouldBe("testing");
			second.Log.Last().Command.ShouldBe("testing");

			first.AdvanceCommitIndex();
			first.CommitIndex.ShouldBe(1);
		}
	}
}