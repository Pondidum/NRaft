using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NRaft.Tests.TestInfrastructure;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.NodeTests
{
	public class OnRequestVoteResponseTests
	{
		private const int NodeID = 10;
		private const int CurrentTerm = 5;

		private readonly InMemoryStore _store;
		private readonly Node _node;

		public OnRequestVoteResponseTests()
		{
			_store = new InMemoryStore();
			_store.CurrentTerm = CurrentTerm - 1;

			var clock = new ControllableClock();
			var dispatcher = Substitute.For<IConnector>();

			_node = new Node(_store, clock, dispatcher, NodeID);
			clock.EndCurrentHeartbeat();
		}

		[Fact]
		public void When_a_message_has_a_newer_term()
		{
			_node.OnRequestVoteResponse(new RequestVoteResponse
			{
				Term = CurrentTerm + 1,
			});

			_store.CurrentTerm.ShouldBe(CurrentTerm + 1);
		}

		[Fact]
		public void When_the_terms_are_different()
		{
			var message = new RequestVoteResponse
			{
				Term = CurrentTerm - 2,
				VoteGranted = true
			};

			_node.OnRequestVoteResponse(message);

			_node.VotesResponded.ShouldBe(new[] { NodeID });
			_node.VotesGranted.ShouldBe(new[] { NodeID });
		}

		[Fact]
		public void When_the_terms_match_but_the_vote_was_not_granted()
		{
			var message = new RequestVoteResponse
			{
				GranterID = 30,
				Term = CurrentTerm,
				VoteGranted = false
			};

			_node.OnRequestVoteResponse(message);

			_node.VotesResponded.ShouldBe(new[] { NodeID, message.GranterID });
			_node.VotesGranted.ShouldBe(new[] { NodeID });
		}

		[Fact]
		public void When_the_terms_match_and_the_vote_was_granted()
		{
			var message = new RequestVoteResponse
			{
				GranterID = 30,
				Term = CurrentTerm,
				VoteGranted = true
			};

			_node.OnRequestVoteResponse(message);

			_node.VotesResponded.ShouldBe(new[] { NodeID, message.GranterID });
			_node.VotesGranted.ShouldBe(new[] { NodeID, message.GranterID });
		}
	}
}
