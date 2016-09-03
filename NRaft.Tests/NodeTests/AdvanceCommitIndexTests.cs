using System.Linq;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
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

		public AdvanceCommitIndexTests()
		{
			_store = new InMemoryStore();
			_connector = Substitute.For<IConnector>();

			_node = new Node(_store, _connector, NodeID);
			_node.BecomeCandidate();
			_node.BecomeLeader();
			_node.ForceCommitIndex(2);
			_store.Log = new[] {
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 0 },
				new LogEntry { Index = 3, Term = 1 }
			};
		}

		[Fact]
		public void When_not_a_leader()
		{
			_node.BecomeFollower();

			_node.AdvanceCommitIndex();

			_node.CommitIndex.ShouldBe(2);
		}

		[Fact]
		public void When_single_node_and_there_are_no_more_entries_to_commit()
		{
			_node.ForceCommitIndex(3);

			_node.AdvanceCommitIndex();

			_node.CommitIndex.ShouldBe(3);
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
			_node.AddNodeToCluster(22);
			_node.AddNodeToCluster(33);
			_node.ForceCommitIndex(3);

			_node.AdvanceCommitIndex();

			_node.CommitIndex.ShouldBe(3);
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
