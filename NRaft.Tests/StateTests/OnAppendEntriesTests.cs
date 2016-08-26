using System.Linq;
using System.Runtime.InteropServices;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class OnAppendEntriesTests
	{
		private const int CurrentTerm = 5;

		private AppendEntriesResponse _response;

		private readonly State _state;
		private readonly InMemoryStore _store;
		private readonly IConnector _connector;

		public OnAppendEntriesTests()
		{
			_store = new InMemoryStore();
			_connector = Substitute.For<IConnector>();
			_connector
				.When(d => d.SendReply(Arg.Any<AppendEntriesResponse>()))
				.Do(cb => _response = cb.Arg<AppendEntriesResponse>());

			_state = new State(_store, _connector, 10);
			_state.ForceTerm(CurrentTerm);
			_state.ForceLog(
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 1 },
				new LogEntry { Index = 3, Term = 2 },
				new LogEntry { Index = 4, Term = 3 },
				new LogEntry { Index = 5, Term = 3 },
				new LogEntry { Index = 6, Term = 4 },
				new LogEntry { Index = 7, Term = 5 },
				new LogEntry { Index = 8, Term = 6 }
			);
			_state.ForceCommitIndex(7);
		}

		[Fact]
		public void When_a_message_has_a_newer_term()
		{
			_state.OnAppendEntries(new AppendEntriesRequest
			{
				Term = CurrentTerm + 1,
			});

			_store.CurrentTerm.ShouldBe(CurrentTerm + 1);
		}

		[Fact]
		public void When_a_messages_term_is_less_than_the_nodes()
		{
			var message = new AppendEntriesRequest
			{
				Term = 3
			};

			_state.OnAppendEntries(message);

			_response.ShouldSatisfyAllConditions(
				() => _response.Success.ShouldBeFalse(),
				() => _response.Term.ShouldBe(CurrentTerm),
				() => _response.MatchIndex.ShouldBe(0)
			);
		}

		[Fact]
		public void When_log_does_not_contain_entry_at_previousLogIndex_with_matching_term()
		{
			var message = new AppendEntriesRequest
			{
				Term = CurrentTerm,
				PreviousLogIndex = 2,
			};

			_state.OnAppendEntries(message);

			_response.ShouldSatisfyAllConditions(
				() => _response.Success.ShouldBeFalse(),
				() => _response.Term.ShouldBe(CurrentTerm),
				() => _response.MatchIndex.ShouldBe(0)
			);
		}

		[Fact]
		public void When_an_existing_entry_conflicts_with_a_new_entry()
		{
			var lastCommonEntry = _state.Log.Single(e => e.Index == 4);

			var message = new AppendEntriesRequest
			{
				Term = CurrentTerm,
				PreviousLogIndex = lastCommonEntry.Index,
				PreviousLogTerm = lastCommonEntry.Term,
				Entries = new[]
				{
					new LogEntry { Index = 5, Term = 4}
				}
			};

			_state.OnAppendEntries(message);

			_state.Log.ShouldBe(new[]
			{
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 1 },
				new LogEntry { Index = 3, Term = 2 },
				new LogEntry { Index = 4, Term = 3 },
				new LogEntry { Index = 5, Term = 4 },
			});

			_response.ShouldSatisfyAllConditions(
				() => _response.Success.ShouldBeTrue(),
				() => _response.Term.ShouldBe(CurrentTerm),
				() => _response.MatchIndex.ShouldBe(lastCommonEntry.Index + message.Entries.Length)
			);
		}

		[Fact]
		public void When_the_leader_has_a_newer_commit_index_and_there_are_no_new_entries()
		{
			var lastCommonEntry = _state.Log.Last();

			var message = new AppendEntriesRequest
			{
				Term = CurrentTerm,
				PreviousLogIndex = lastCommonEntry.Index,
				PreviousLogTerm = lastCommonEntry.Term,
				LeaderCommit = 7
			};

			_state.OnAppendEntries(message);

			_state.CommitIndex.ShouldBe(7);

			_response.ShouldSatisfyAllConditions(
				() => _response.Success.ShouldBeTrue(),
				() => _response.Term.ShouldBe(CurrentTerm),
				() => _response.MatchIndex.ShouldBe(lastCommonEntry.Index)
			);
		}

		[Fact]
		public void When_the_leader_has_a_newer_commit_index_and_there_are_new_entries()
		{
			var lastCommonEntry = _state.Log.Last();

			var message = new AppendEntriesRequest
			{
				Term = CurrentTerm,
				PreviousLogIndex = lastCommonEntry.Index,
				PreviousLogTerm = lastCommonEntry.Term,
				LeaderCommit = 8,
				Entries = new[]
				{
					new LogEntry { Index = 8, Term = CurrentTerm },
					new LogEntry { Index = 9, Term = CurrentTerm }
				}
			};

			_state.OnAppendEntries(message);

			_state.CommitIndex.ShouldBe(8);

			_response.ShouldSatisfyAllConditions(
				() => _response.Success.ShouldBeTrue(),
				() => _response.Term.ShouldBe(CurrentTerm),
				() => _response.MatchIndex.ShouldBe(lastCommonEntry.Index + message.Entries.Length)
			);
		}

		[Fact]
		public void When_the_leader_has_a_newer_commit_index_and_there_are_less_new_entries()
		{
			var lastCommonEntry = _state.Log.Last();

			var message = new AppendEntriesRequest
			{
				Term = CurrentTerm,
				PreviousLogIndex = lastCommonEntry.Index,
				PreviousLogTerm = lastCommonEntry.Term,
				LeaderCommit = 15,
				Entries = new[]
				{
					new LogEntry { Index = 8, Term = CurrentTerm },
					new LogEntry { Index = 9, Term = CurrentTerm }
				}
			};

			_state.OnAppendEntries(message);

			_state.CommitIndex.ShouldBe(9);

			_response.ShouldSatisfyAllConditions(
				() => _response.Success.ShouldBeTrue(),
				() => _response.Term.ShouldBe(CurrentTerm),
				() => _response.MatchIndex.ShouldBe(lastCommonEntry.Index + message.Entries.Length)
			);
		}

		[Fact]
		public void When_the_node_is_a_candidate_and_the_terms_are_equal()
		{
			var message = new AppendEntriesRequest
			{
				Term = CurrentTerm,
				PreviousLogIndex = _state.Log.Last().Index,
				PreviousLogTerm = _state.Log.Last().Term,
				LeaderCommit = _state.CommitIndex
			};

			_state.ForceType(Types.Candidate);
			_state.OnAppendEntries(message);

			_state.Role.ShouldBe(Types.Follower);
		}
	}
}
