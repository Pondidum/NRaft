using System.Linq;
using NRaft.Infrastructure;
using NRaft.Messages;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class AdvanceCommitIndexTests
	{
		private const int NodeID = 11;

		private readonly IDispatcher _dispatcher;
		private readonly State _state;

		public AdvanceCommitIndexTests()
		{
			_dispatcher = Substitute.For<IDispatcher>();

			_state = new State(_dispatcher, NodeID);
			_state.BecomeCandidate();
			_state.ForceType(Types.Leader);
			_state.ForceCommitIndex(2);
			_state.ForceLog(
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 0 },
				new LogEntry { Index = 3, Term = 1 }
			);
		}

		[Fact]
		public void When_not_a_leader()
		{
			_state.ForceType(Types.Follower);

			_state.AdvanceCommitIndex();

			_state.CommitIndex.ShouldBe(2);
		}

		[Fact]
		public void When_single_node_and_there_are_no_more_entries_to_commit()
		{
			_state.ForceCommitIndex(3);

			_state.AdvanceCommitIndex();

			_state.CommitIndex.ShouldBe(3);
		}

		//not sure if this is a valid case - can raft operate with 1 node only?
		[Fact]
		public void When_single_node_and_there_is_an_entry_to_commit()
		{
			_state.AdvanceCommitIndex();
			_state.CommitIndex.ShouldBe(_state.Log.Last().Index);
		}

		[Fact]
		public void When_three_nodes_and_there_are_no_more_entries_to_commit()
		{
			_state.AddNodeToCluster(22);
			_state.AddNodeToCluster(33);
			_state.ForceCommitIndex(3);

			_state.AdvanceCommitIndex();

			_state.CommitIndex.ShouldBe(3);
		}

		[Fact]
		public void When_three_nodes_and_there_is_an_entry_to_commit_but_has_not_been_written_to_the_quorum()
		{
			_state.AddNodeToCluster(22);
			_state.AddNodeToCluster(33);

			_state.OnAppendEntriesResponse(new AppendEntriesResponse
			{
				FollowerID = 22,
				MatchIndex = 1,
				Success = true,
				Term = _state.CurrentTerm
			});

			_state.OnAppendEntriesResponse(new AppendEntriesResponse
			{
				FollowerID = 33,
				MatchIndex = 1,
				Success = true,
				Term = _state.CurrentTerm
			});

			_state.AdvanceCommitIndex();

			_state.CommitIndex.ShouldBe(2);
		}

		[Fact]
		public void When_three_nodes_and_there_is_an_entry_to_commit_and_the_quorum_matches()
		{
			_state.AddNodeToCluster(22);
			_state.AddNodeToCluster(33);

			_state.OnAppendEntriesResponse(new AppendEntriesResponse
			{
				FollowerID = 22,
				MatchIndex = 3,
				Success = true,
				Term = _state.CurrentTerm
			});

			_state.OnAppendEntriesResponse(new AppendEntriesResponse
			{
				FollowerID = 33,
				MatchIndex = 3,
				Success = true,
				Term = _state.CurrentTerm
			});

			_state.AdvanceCommitIndex();

			_state.CommitIndex.ShouldBe(3);

		}
	}
}
