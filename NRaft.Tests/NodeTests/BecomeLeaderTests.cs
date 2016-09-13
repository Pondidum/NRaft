using System;
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
	public class BecomeLeaderTests
	{
		private const int NodeID = 10;

		private AppendEntriesRequest _heartbeat;

		private readonly InMemoryStore _store;
		private readonly IConnector _connector;
		private readonly Node _node;
		private readonly ControllableClock _clock;

		public BecomeLeaderTests()
		{
			_store = new InMemoryStore();
			_store.CurrentTerm = 2;

			_clock = new ControllableClock();

			_connector = Substitute.For<IConnector>();
			_connector
				.When(d => d.SendHeartbeat(Arg.Any<AppendEntriesRequest>()))
				.Do(cb => _heartbeat = cb.Arg<AppendEntriesRequest>());

			_node = new Node(_store, _clock, _connector, NodeID);

			_node.BecomeCandidate();

			_store.Log = new[] {
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 0 },
				new LogEntry { Index = 3, Term = 1 },
				new LogEntry { Index = 4, Term = 2 },
				new LogEntry { Index = 5, Term = 2 }
			};

			_node.AddNodeToCluster(15);
			_node.OnRequestVoteResponse(new RequestVoteResponse
			{
				GranterID = 15,
				Term = _store.CurrentTerm,
				VoteGranted = true
			});
		}

		[Fact]
		public void If_the_node_is_not_a_candidate()
		{
			_node.BecomeFollower();
			_clock.EndCurrentElection();

			//when an election fails, we start a new one, thus becoming a candidate instead of a follower
			_node.Role.ShouldBe(Types.Candidate);
		}

		[Fact]
		public void When_a_vote_has_not_been_granted()
		{
			_node.ResetVotes();

			_clock.EndCurrentElection();

			_node.Role.ShouldBe(Types.Candidate);
		}

		[Fact]
		public void The_role_changes()
		{
			_clock.EndCurrentElection();
			_node.Role.ShouldBe(Types.Leader);
		}

		[Fact]
		public void The_next_index_for_every_other_client_is_set_to_last_log_index()
		{
			_clock.EndCurrentElection();

			var nextLogIndex = _store.Log.Last().Index + 1;

			var outcomes = _node
				.KnownNodes
				.Select(node => new Action(() => _node.NextIndexFor(node).ShouldBe(nextLogIndex)))
				.ToArray();

			_node.ShouldSatisfyAllConditions(outcomes);
		}

		[Fact]
		public void The_match_index_for_every_other_client_is_set_to_zero()
		{
			_clock.EndCurrentElection();

			var outcomes = _node
				.KnownNodes
				.Select(node => new Action(() => _node.MatchIndexFor(node).ShouldBe(0)))
				.ToArray();

			_node.ShouldSatisfyAllConditions(outcomes);
		}

		[Fact]
		public void It_sends_append_entries_to_all()
		{
			_clock.EndCurrentElection();

			var index = _node.NextIndexFor(_heartbeat.RecipientID) - 1;

			_heartbeat.ShouldSatisfyAllConditions(
				() => _heartbeat.LeaderID.ShouldBe(NodeID),
				() => _heartbeat.Term.ShouldBe(_store.CurrentTerm),
				() => _heartbeat.PreviousLogIndex.ShouldBe(index),
				() => _heartbeat.PreviousLogTerm.ShouldBe(_store.Log.Single(e => e.Index == index).Term),
				() => _heartbeat.LeaderCommit.ShouldBe(_node.CommitIndex),
				() => _heartbeat.Entries.ShouldBeEmpty()
			);
		}
	}
}
