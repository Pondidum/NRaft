using System;
using System.Linq;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class BecomeLeaderTests
	{
		private const int NodeID = 10;

		private AppendEntriesRpc _heartbeat;

		private readonly IDispatcher _dispatcher;
		private readonly State _state;

		public BecomeLeaderTests()
		{
			_dispatcher = Substitute.For<IDispatcher>();
			_dispatcher
				.When(d => d.SendHeartbeat(Arg.Any<AppendEntriesRpc>()))
				.Do(cb => _heartbeat = cb.Arg<AppendEntriesRpc>());

			_state = new State(_dispatcher, NodeID);

			_state.ForceType(Types.Candidate);
			_state.ForceCommitIndex(3);
			_state.ForceTerm(2);
			_state.ForceLog(
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 0 },
				new LogEntry { Index = 3, Term = 1 },
				new LogEntry { Index = 4, Term = 2 },
				new LogEntry { Index = 5, Term = 2 }
			);

			_state.AddNodeToCluster(15);
			_state.OnRequestVoteResponse(new RequestVoteResponse
			{
				NodeID = 15,
				Term = _state.CurrentTerm,
				VoteGranted = true
			});
		}

		[Fact]
		public void If_the_node_is_not_a_candidate()
		{
			_state.ForceType(Types.Follower);
			_state.BecomeLeader();

			_state.Role.ShouldBe(Types.Follower);
		}

		[Fact]
		public void When_a_vote_has_not_been_granted()
		{
			_state.ResetVotes();

			_state.BecomeLeader();

			_state.Role.ShouldBe(Types.Candidate);
		}

		[Fact]
		public void The_role_changes()
		{
			_state.BecomeLeader();
			_state.Role.ShouldBe(Types.Leader);
		}

		[Fact]
		public void The_next_index_for_every_other_client_is_set_to_last_log_index()
		{
			_state.BecomeLeader();

			var nextLogIndex = _state.Log.Last().Index + 1;

			var outcomes = _state
				.KnownNodes
				.Select(node => new Action(() => _state.NextIndexFor(node).ShouldBe(nextLogIndex)))
				.ToArray();

			_state.ShouldSatisfyAllConditions(outcomes);
		}

		[Fact]
		public void The_match_index_for_every_other_client_is_set_to_zero()
		{
			_state.BecomeLeader();

			var outcomes = _state
				.KnownNodes
				.Select(node => new Action(() => _state.MatchIndexFor(node).ShouldBe(0)))
				.ToArray();

			_state.ShouldSatisfyAllConditions(outcomes);
		}

		[Fact]
		public void It_sends_append_entries_to_all()
		{
			_state.BecomeLeader();

			var index = _state.NextIndexFor(_heartbeat.RecipientID) - 1;

			_heartbeat.ShouldSatisfyAllConditions(
				() => _heartbeat.LeaderID.ShouldBe(NodeID),
				() => _heartbeat.Term.ShouldBe(_state.CurrentTerm),
				() => _heartbeat.PreviousLogIndex.ShouldBe(index),
				() => _heartbeat.PreviousLogTerm.ShouldBe(_state.Log.Single(e => e.Index == index).Term),
				() => _heartbeat.LeaderCommit.ShouldBe(_state.CommitIndex),
				() => _heartbeat.Entries.ShouldBeEmpty()
			);
		}
	}
}
