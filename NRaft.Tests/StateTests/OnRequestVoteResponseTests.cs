﻿using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class OnRequestVoteResponseTests
	{
		private const int CurrentTerm = 5;

		private readonly State _state;

		public OnRequestVoteResponseTests()
		{
			var dispatcher = Substitute.For<IDispatcher>();

			_state = new State(dispatcher, 10);
			_state.ForceTerm(CurrentTerm);
			_state.ForceType(Types.Candidate);
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
				NodeID = 30,
				Term = CurrentTerm,
				VoteGranted = false
			};

			_state.OnRequestVoteResponse(message);

			_state.VotesResponded.ShouldBe(new[] { message.NodeID });
			_state.VotesGranted.ShouldBeEmpty();
		}

		[Fact]
		public void When_the_terms_match_and_the_vote_was_granted()
		{
			var message = new RequestVoteResponse
			{
				NodeID = 30,
				Term = CurrentTerm,
				VoteGranted = true
			};

			_state.OnRequestVoteResponse(message);

			_state.VotesResponded.ShouldBe(new[] { message.NodeID });
			_state.VotesGranted.ShouldBe(new[] { message.NodeID });
		}
	}
}