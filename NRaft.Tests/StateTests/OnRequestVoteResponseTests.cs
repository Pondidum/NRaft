using NSubstitute;
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
		}
	}
}
