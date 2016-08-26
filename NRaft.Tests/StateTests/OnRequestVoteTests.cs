using System.Linq;
using NRaft.Infrastructure;
using NRaft.Messages;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class OnRequestVoteTests
	{

		private const int CurrentTerm = 5;

		private readonly State _state;
		private readonly IConnector _connector;

		public OnRequestVoteTests()
		{
			_connector = Substitute.For<IConnector>();

			_state = new State(_connector, Substitute.For<IListener>(), 10);
			_state.ForceTerm(CurrentTerm);
			_state.ForceLog(
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 1 },
				new LogEntry { Index = 3, Term = 2 },
				new LogEntry { Index = 4, Term = 3 },
				new LogEntry { Index = 5, Term = 3 },
				new LogEntry { Index = 6, Term = 4 },
				new LogEntry { Index = 7, Term = 5 }
			);
			_state.ForceCommitIndex(7);
		}

		[Fact]
		public void When_a_message_has_a_newer_term()
		{
			_state.OnRequestVote(new RequestVoteRequest
			{
				Term = CurrentTerm + 1,
			});

			_state.CurrentTerm.ShouldBe(CurrentTerm + 1);
		}

		[Fact]
		public void When_the_requested_term_is_less_than_the_nodes()
		{
			var message = new RequestVoteRequest
			{
				Term = CurrentTerm - 2,
				CandidateID = 20,
				LastLogIndex = _state.Log.Last().Index,
				LastLogTerm = _state.Log.Last().Term
			};

			_state.OnRequestVote(message);

			_connector
				.Received()
				.SendReply(Arg.Is<RequestVoteResponse>(m => m.VoteGranted == false && m.Term == CurrentTerm));
		}

		[Fact]
		public void When_the_requested_term_is_equal_and_a_vote_has_already_cast_for_another_candidate()
		{
			var message = new RequestVoteRequest
			{
				Term = CurrentTerm,
				CandidateID = 20,
				LastLogIndex = _state.Log.Last().Index,
				LastLogTerm = _state.Log.Last().Term
			};

			_state.ForceVotedFor(15);
			_state.OnRequestVote(message);

			_connector
				.Received()
				.SendReply(Arg.Is<RequestVoteResponse>(m => m.VoteGranted == false && m.Term == CurrentTerm));
		}

		[Fact]
		public void When_the_requested_term_is_equal_and_the_candidates_log_is_not_up_to_date()
		{
			var message = new RequestVoteRequest
			{
				Term = CurrentTerm,
				CandidateID = 20,
				LastLogIndex = 5
			};

			_state.OnRequestVote(message);

			_connector
				.Received()
				.SendReply(Arg.Is<RequestVoteResponse>(m => m.VoteGranted == false && m.Term == CurrentTerm));
		}

		[Fact]
		public void When_the_term_is_equal_and_the_log_is_up_to_date()
		{
			var message = new RequestVoteRequest
			{
				Term = CurrentTerm,
				CandidateID = 20,
				LastLogIndex = _state.Log.Last().Index,
				LastLogTerm = _state.Log.Last().Term
			};

			_state.OnRequestVote(message);

			_connector
				.Received()
				.SendReply(Arg.Is<RequestVoteResponse>(m => m.VoteGranted && m.Term == CurrentTerm));
		}

	}
}
