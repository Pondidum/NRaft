using System.Linq;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.NodeTests
{
	public class OnRequestVoteTests
	{
		private const int CurrentTerm = 5;

		private readonly Node _node;
		private readonly InMemoryStore _store;
		private readonly IConnector _connector;

		public OnRequestVoteTests()
		{
			_store = new InMemoryStore();
			_store.CurrentTerm = CurrentTerm;

			_connector = Substitute.For<IConnector>();

			_node = new Node(_store, _connector, 10);
			_store.Log = new[] {
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 1 },
				new LogEntry { Index = 3, Term = 2 },
				new LogEntry { Index = 4, Term = 3 },
				new LogEntry { Index = 5, Term = 3 },
				new LogEntry { Index = 6, Term = 4 },
				new LogEntry { Index = 7, Term = 5 }
			};
			//_node.ForceCommitIndex(7);
		}

		[Fact]
		public void When_a_message_has_a_newer_term()
		{
			_node.OnRequestVote(new RequestVoteRequest
			{
				Term = CurrentTerm + 1,
			});

			_store.CurrentTerm.ShouldBe(CurrentTerm + 1);
		}

		[Fact]
		public void When_the_requested_term_is_less_than_the_nodes()
		{
			var message = new RequestVoteRequest
			{
				Term = CurrentTerm - 2,
				CandidateID = 20,
				LastLogIndex = _store.Log.Last().Index,
				LastLogTerm = _store.Log.Last().Term
			};

			_node.OnRequestVote(message);

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
				LastLogIndex = _store.Log.Last().Index,
				LastLogTerm = _store.Log.Last().Term
			};

			_store.VotedFor = 15;
			_node.OnRequestVote(message);

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

			_node.OnRequestVote(message);

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
				LastLogIndex = _store.Log.Last().Index,
				LastLogTerm = _store.Log.Last().Term
			};

			_node.OnRequestVote(message);

			_connector
				.Received()
				.SendReply(Arg.Is<RequestVoteResponse>(m => m.VoteGranted && m.Term == CurrentTerm));
		}

	}
}
