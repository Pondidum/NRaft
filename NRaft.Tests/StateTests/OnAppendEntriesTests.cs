using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class OnAppendEntriesTests
	{
		private const int CurrentTerm = 5;

		private readonly State _state;
		private readonly IDispatcher _dispatcher;

		public OnAppendEntriesTests()
		{
			_dispatcher = Substitute.For<IDispatcher>();

			_state = new State(_dispatcher, 10);
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
		public void When_a_messages_term_is_less_than_the_nodes()
		{
			var message = new AppendEntriesRpc
			{
				Term = 3
			};

			_state.OnAppendEntries(message);

			_dispatcher
				.Received()
				.SendReply(Arg.Is<AppendEntriesResponse>(m => m.Success == false && m.Term == CurrentTerm));
		}

		[Fact]
		public void When_log_does_not_contain_entry_at_previousLogIndex_with_matching_term()
		{
			var message = new AppendEntriesRpc
			{
				Term = CurrentTerm,
				PreviousLogIndex = 2,
			};

			_state.OnAppendEntries(message);

			_dispatcher
				.Received()
				.SendReply(Arg.Is<AppendEntriesResponse>(m => m.Success == false && m.Term == CurrentTerm));
		}

		[Fact]
		public void When_an_existing_entry_conflicts_with_a_new_entry()
		{
			var message = new AppendEntriesRpc
			{
				Term = CurrentTerm,
				PreviousLogIndex = 7,
				Entries = new[]
				{
					new LogEntry { Index = 5, Term = 4}
				}
			};

			_state.OnAppendEntries(message);

			_state.Log.ShouldBe(new []
			{
				new LogEntry { Index = 1, Term = 0 },
				new LogEntry { Index = 2, Term = 1 },
				new LogEntry { Index = 3, Term = 2 },
				new LogEntry { Index = 4, Term = 3 },
				new LogEntry { Index = 5, Term = 4 },
			});

			_dispatcher
				.Received()
				.SendReply(Arg.Is<AppendEntriesResponse>(m => m.Success && m.Term == CurrentTerm));
		}

		[Fact]
		public void When_the_leader_has_a_newer_commit_index_and_there_are_no_new_entries()
		{
			var message = new AppendEntriesRpc
			{
				Term = CurrentTerm,
				PreviousLogIndex = 7,
				LeaderCommit = 7
			};

			_state.OnAppendEntries(message);

			_state.CommitIndex.ShouldBe(7);

			_dispatcher
				.Received()
				.SendReply(Arg.Is<AppendEntriesResponse>(m => m.Success && m.Term == CurrentTerm));
		}

		[Fact]
		public void When_the_leader_has_a_newer_commit_index_and_there_are_new_entries()
		{
			var message = new AppendEntriesRpc
			{
				Term = CurrentTerm,
				PreviousLogIndex = 7,
				LeaderCommit = 8,
				Entries = new[]
				{
					new LogEntry { Index = 8, Term = CurrentTerm },
					new LogEntry { Index = 9, Term = CurrentTerm }
				}
			};

			_state.OnAppendEntries(message);

			_state.CommitIndex.ShouldBe(8);

			_dispatcher
				.Received()
				.SendReply(Arg.Is<AppendEntriesResponse>(m => m.Success && m.Term == CurrentTerm));
		}

		[Fact]
		public void When_the_leader_has_a_newer_commit_index_and_there_are_less_new_entries()
		{
			var message = new AppendEntriesRpc
			{
				Term = CurrentTerm,
				PreviousLogIndex = 7,
				LeaderCommit = 15,
				Entries = new[]
				{
					new LogEntry { Index = 8, Term = CurrentTerm },
					new LogEntry { Index = 9, Term = CurrentTerm }
				}
			};

			_state.OnAppendEntries(message);

			_state.CommitIndex.ShouldBe(9);

			_dispatcher
				.Received()
				.SendReply(Arg.Is<AppendEntriesResponse>(m => m.Success && m.Term == CurrentTerm));
		}
	}
}
