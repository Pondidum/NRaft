using System.Linq;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NRaft.Tests.TestInfrastructure;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.NodeTests
{
	public class AdvanceCommitIndexTests
	{
		private const int NodeID = 11;

		private readonly IConnector _connector;
		private readonly Node _node;
		private readonly InMemoryStore _store;
		private readonly ControllableClock _clock;

		public AdvanceCommitIndexTests()
		{
			_store = new InMemoryStore();
			_clock = new ControllableClock();
			_connector = Substitute.For<IConnector>();

			_node = new Node(_store, _clock, _connector, NodeID);
			_clock.EndCurrentHeartbeat();
			_clock.EndCurrentElection();

			//create committed log
			_store.Log = new[] {
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 1 },
			};
			_node.AdvanceCommitIndex();

			//push on a new un-committed item
			_node.OnClientRequest(null);
		}

		[Fact]
		public void When_not_a_leader()
		{
			_node.OnAppendEntries(new AppendEntriesRequest
			{
				Term = _store.CurrentTerm + 1,
				LeaderCommit = _node.CommitIndex
			});

			_node.AdvanceCommitIndex();

			_node.CommitIndex.ShouldBe(2);
		}

		//not sure if this is a valid case - can raft operate with 1 node only?
		[Fact]
		public void When_single_node_and_there_is_an_entry_to_commit()
		{
			_node.AdvanceCommitIndex();
			_node.CommitIndex.ShouldBe(_store.Log.Last().Index);
		}

		[Fact]
		public void When_three_nodes_and_there_are_no_more_entries_to_commit()
		{
			_store.Log = _store.Log.Take(2).ToArray();

			_node.AddNodeToCluster(22);
			_node.AddNodeToCluster(33);

			_node.AdvanceCommitIndex();

			_node.CommitIndex.ShouldBe(2);
		}

		[Fact]
		public void When_three_nodes_and_there_is_an_entry_to_commit_but_has_not_been_written_to_the_quorum()
		{
			_node.AddNodeToCluster(22);
			_node.AddNodeToCluster(33);

			_node.OnAppendEntriesResponse(new AppendEntriesResponse
			{
				FollowerID = 22,
				MatchIndex = 1,
				Success = true,
				Term = _store.CurrentTerm
			});

			_node.OnAppendEntriesResponse(new AppendEntriesResponse
			{
				FollowerID = 33,
				MatchIndex = 1,
				Success = true,
				Term = _store.CurrentTerm
			});

			_node.AdvanceCommitIndex();

			_node.CommitIndex.ShouldBe(2);
		}

		[Fact]
		public void When_three_nodes_and_there_is_an_entry_to_commit_and_the_quorum_matches()
		{
			_node.AddNodeToCluster(22);
			_node.AddNodeToCluster(33);

			_node.OnAppendEntriesResponse(new AppendEntriesResponse
			{
				FollowerID = 22,
				MatchIndex = 3,
				Success = true,
				Term = _store.CurrentTerm
			});

			_node.OnAppendEntriesResponse(new AppendEntriesResponse
			{
				FollowerID = 33,
				MatchIndex = 3,
				Success = true,
				Term = _store.CurrentTerm
			});

			_node.AdvanceCommitIndex();

			_node.CommitIndex.ShouldBe(3);

		}
	}
}
