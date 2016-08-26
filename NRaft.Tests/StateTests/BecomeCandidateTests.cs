using System.Linq;
using NRaft.Infrastructure;
using NRaft.Messages;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class BecomeCandidateTests
	{
		private const int NodeID = 123;

		private readonly IDispatcher _dispatcher;
		private readonly State _state;

		private RequestVoteRequest _response;

		public BecomeCandidateTests()
		{
			_dispatcher = Substitute.For<IDispatcher>();
			_dispatcher
				.When(d => d.RequestVotes(Arg.Any<RequestVoteRequest>()))
				.Do(cb => _response = cb.Arg<RequestVoteRequest>());

			_state = new State(_dispatcher, Substitute.For<IListener>(), NodeID);

			_state.ForceTerm(2);
			_state.ForceLog(
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 0 },
				new LogEntry { Index = 3, Term = 1 },
				new LogEntry { Index = 4, Term = 2 },
				new LogEntry { Index = 5, Term = 2 }
			);

			_state.BecomeCandidate();
		}

		[Fact]
		public void The_role_changes() => _state.Role.ShouldBe(Types.Candidate);

		[Fact]
		public void The_term_increases() => _state.CurrentTerm.ShouldBe(3);

		[Fact]
		public void The_node_responds_to_itself() => _state.VotesResponded.ShouldBe(new[] { NodeID });

		[Fact]
		public void The_node_votes_for_itself() => _state.VotesGranted.ShouldBe(new[] { NodeID });

		[Fact]
		public void The_node_requests_votes_from_others() => _dispatcher.Received(1).RequestVotes(Arg.Any<RequestVoteRequest>());

		[Fact]
		public void The_request_to_others_is_well_formed() => _response.ShouldSatisfyAllConditions(
			() => _response.CandidateID.ShouldBe(NodeID),
			() => _response.Term.ShouldBe(_state.CurrentTerm),
			() => _response.LastLogIndex.ShouldBe(_state.Log.Last().Index),
			() => _response.LastLogTerm.ShouldBe(_state.Log.Last().Term)
		);
	}
}
