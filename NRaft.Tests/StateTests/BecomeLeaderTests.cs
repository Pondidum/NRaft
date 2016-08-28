using System;
using System.Linq;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class BecomeLeaderTests
	{
		private const int NodeID = 10;

		private AppendEntriesRequest _heartbeat;

		private readonly InMemoryStore _store;
		private readonly IConnector _connector;
		private readonly State _state;

		public BecomeLeaderTests()
		{
			_store = new InMemoryStore();
			_store.CurrentTerm = 2;

			_connector = Substitute.For<IConnector>();
			_connector
				.When(d => d.SendHeartbeat(Arg.Any<AppendEntriesRequest>()))
				.Do(cb => _heartbeat = cb.Arg<AppendEntriesRequest>());

			_state = new State(_store, _connector, NodeID);

			_state.BecomeCandidate();
			_state.ForceCommitIndex(3);
			_store.Log = new[] {
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 0 },
				new LogEntry { Index = 3, Term = 1 },
				new LogEntry { Index = 4, Term = 2 },
				new LogEntry { Index = 5, Term = 2 }
			};

			_state.AddNodeToCluster(15);
			_state.OnRequestVoteResponse(new RequestVoteResponse
			{
				GranterID = 15,
				Term = _store.CurrentTerm,
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

			var nextLogIndex = _store.Log.Last().Index + 1;

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
				() => _heartbeat.Term.ShouldBe(_store.CurrentTerm),
				() => _heartbeat.PreviousLogIndex.ShouldBe(index),
				() => _heartbeat.PreviousLogTerm.ShouldBe(_store.Log.Single(e => e.Index == index).Term),
				() => _heartbeat.LeaderCommit.ShouldBe(_state.CommitIndex),
				() => _heartbeat.Entries.ShouldBeEmpty()
			);
		}
	}
}
