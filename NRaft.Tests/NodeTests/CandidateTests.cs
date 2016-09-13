using System;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.NodeTests
{
	public class CandidateTests
	{
		private const int OtherNodeID = 5678;
		private const int NodeID = 1234;

		private readonly IDisposable _heart;
		private readonly Node _node;

		private Action _elapsed;
		private readonly IClock _clock;
		private readonly IStore _store;
		private readonly IConnector _connector;

		public CandidateTests()
		{
			_store = Substitute.For<IStore>();
			_connector = Substitute.For<IConnector>();

			_clock = Substitute.For<IClock>();
			_heart = Substitute.For<IDisposable>();

			_clock
				.CreateElectionTimeout(Arg.Any<TimeSpan>(), Arg.Any<Action>())
				.Returns(_heart)
				.AndDoes(cb => _elapsed = cb.Arg<Action>());

			_node = new Node(_store, _clock, _connector, NodeID);
			_node.AddNodeToCluster(OtherNodeID);
		}

		[Fact]
		public void The_timeout_isnt_started_if_the_node_doesnt_become_a_candidate()
		{
			_clock.DidNotReceive().CreateElectionTimeout(Arg.Any<TimeSpan>(), Arg.Any<Action>());
		}

		[Fact]
		public void Becoming_a_candidate_starts_an_election_timeout()
		{
			_node.BecomeCandidate();

			_clock.Received().CreateElectionTimeout(Arg.Any<TimeSpan>(), Arg.Any<Action>());
		}

		[Fact]
		public void When_the_election_times_out_with_no_consensus()
		{
			_node.BecomeCandidate();
			_connector.ClearReceivedCalls();

			_elapsed();

			_node.Role.ShouldBe(Types.Candidate);
			_heart.Received().Dispose();
			_connector.Received().RequestVotes(Arg.Any<RequestVoteRequest>());
		}

		[Fact]
		public void When_the_election_times_out_with_consensus()
		{
			_node.BecomeCandidate();
			_node.OnRequestVoteResponse(new RequestVoteResponse
			{
				CandidateID = NodeID,
				GranterID = OtherNodeID,
				Term = _store.CurrentTerm,
				VoteGranted = true,
			});

			_elapsed();

			_node.Role.ShouldBe(Types.Leader);
			_heart.Received().Dispose();
		}
	}
}
