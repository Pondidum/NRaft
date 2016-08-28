using System.Linq;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class BecomeCandidateTests
	{
		private const int NodeID = 123;

		private readonly InMemoryStore _store;
		private readonly IConnector _connector;
		private readonly State _state;

		private RequestVoteRequest _response;

		public BecomeCandidateTests()
		{
			_store = new InMemoryStore();
			_store.CurrentTerm = 2;

			_connector = Substitute.For<IConnector>();
			_connector
				.When(d => d.RequestVotes(Arg.Any<RequestVoteRequest>()))
				.Do(cb => _response = cb.Arg<RequestVoteRequest>());

			_state = new State(_store, _connector, NodeID);

			_state.OnRequestVoteResponse(new RequestVoteResponse
			{
				CandidateID = NodeID,
				GranterID = 456,
				Term = _store.CurrentTerm,
				VoteGranted = true
			});

			_store.Log = new[] {
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 0 },
				new LogEntry { Index = 3, Term = 1 },
				new LogEntry { Index = 4, Term = 2 },
				new LogEntry { Index = 5, Term = 2 }
			};

			_state.BecomeCandidate();
		}

		[Fact]
		public void The_role_changes() => _state.Role.ShouldBe(Types.Candidate);

		[Fact]
		public void The_term_increases() => _store.CurrentTerm.ShouldBe(3);

		[Fact]
		public void The_node_responds_to_itself() => _state.VotesResponded.ShouldBe(new[] { NodeID });

		[Fact]
		public void The_node_grants_a_vote_for_itself() => _state.VotesGranted.ShouldBe(new[] { NodeID });

		[Fact]
		public void The_node_votes_for_itself() => _store.VotedFor.ShouldBe(NodeID);

		[Fact]
		public void The_node_requests_votes_from_others() => _connector.Received(1).RequestVotes(Arg.Any<RequestVoteRequest>());

		[Fact]
		public void The_request_to_others_is_well_formed() => _response.ShouldSatisfyAllConditions(
			() => _response.CandidateID.ShouldBe(NodeID),
			() => _response.Term.ShouldBe(_store.CurrentTerm),
			() => _response.LastLogIndex.ShouldBe(_store.Log.Last().Index),
			() => _response.LastLogTerm.ShouldBe(_store.Log.Last().Term)
		);
	}
}
