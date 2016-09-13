using System;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NRaft.Tests.TestInfrastructure;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.NodeTests
{
	public class CandidateTests
	{
		private const int OtherNodeID = 5678;
		private const int NodeID = 1234;

		private readonly Node _node;

		private readonly ControllableClock _clock;
		private readonly IStore _store;
		private readonly IConnector _connector;

		public CandidateTests()
		{
			_store = Substitute.For<IStore>();
			_connector = Substitute.For<IConnector>();
			_clock = new ControllableClock();

			_node = new Node(_store, _clock, _connector, NodeID);
			_node.AddNodeToCluster(OtherNodeID);
		}

		[Fact]
		public void The_timeout_isnt_started_if_the_node_doesnt_become_a_candidate()
		{
			_clock.LastElectionTimer.ShouldBeNull();
		}

		[Fact]
		public void Becoming_a_candidate_starts_an_election_timeout()
		{
			_clock.EndCurrentHeartbeat();

			_clock.LastElectionTimer.ShouldNotBeNull();
		}

		[Fact]
		public void When_the_election_times_out_with_no_consensus()
		{
			_clock.EndCurrentHeartbeat();
			_connector.ClearReceivedCalls();

			var currentElection = _clock.LastElectionTimer;

			_clock.EndCurrentElection();

			_node.Role.ShouldBe(Types.Candidate);
			currentElection.WasDisposed.ShouldBeTrue();
			_connector.Received().RequestVotes(Arg.Any<RequestVoteRequest>());
		}

		[Fact]
		public void When_the_election_times_out_with_consensus()
		{
			_clock.EndCurrentHeartbeat();
			_node.OnRequestVoteResponse(new RequestVoteResponse
			{
				CandidateID = NodeID,
				GranterID = OtherNodeID,
				Term = _store.CurrentTerm,
				VoteGranted = true,
			});

			var currentElection = _clock.LastElectionTimer;
			_clock.EndCurrentElection();

			_node.Role.ShouldBe(Types.Leader);
			currentElection.WasDisposed.ShouldBeTrue();
		}
	}
}
