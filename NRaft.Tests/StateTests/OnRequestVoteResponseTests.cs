using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class OnRequestVoteResponseTests
	{
		private const int CurrentTerm = 5;

		private readonly InMemoryStore _store;
		private readonly State _state;

		public OnRequestVoteResponseTests()
		{
			_store = new InMemoryStore();
			var dispatcher = Substitute.For<IConnector>();

			_state = new State(_store, dispatcher, 10);
			_state.ForceTerm(CurrentTerm);
			_state.ForceType(Types.Candidate);
		}

		[Fact]
		public void When_a_message_has_a_newer_term()
		{
			_state.OnRequestVoteResponse(new RequestVoteResponse
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

			_state.OnRequestVoteResponse(message);

			_state.VotesResponded.ShouldBeEmpty();
			_state.VotesGranted.ShouldBeEmpty();
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

			_state.OnRequestVoteResponse(message);

			_state.VotesResponded.ShouldBe(new[] { message.GranterID });
			_state.VotesGranted.ShouldBeEmpty();
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

			_state.OnRequestVoteResponse(message);

			_state.VotesResponded.ShouldBe(new[] { message.GranterID });
			_state.VotesGranted.ShouldBe(new[] { message.GranterID });
		}
	}
}
