using System.Collections.Generic;
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
				new LogEntry { Term = 0 },
				new LogEntry { Term = 1 },
				new LogEntry { Term = 2 },
				new LogEntry { Term = 3 },
				new LogEntry { Term = 3 },
				new LogEntry { Term = 4 },
				new LogEntry { Term = 5 },
				new LogEntry { Term = 6 }
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
				.SendReply(Arg.Is<AppendEntriesResponse>(m => m.Success == false));
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
				.SendReply(Arg.Is<AppendEntriesResponse>(m => m.Success == false));
		}

		[Fact]
		public void When_an_existing_entry_conflicts_with_a_new_entry()
		{
			var message = new AppendEntriesRpc
			{
				Term = CurrentTerm,
				PreviousLogIndex = 7,
				Entries = new Dictionary<int, LogEntry>
				{
					[4] = new LogEntry {Term = 4}
				}
			};

			_state.OnAppendEntries(message);

			_state.Log.ShouldBe(new []
			{
				new LogEntry { Term = 0 },
				new LogEntry { Term = 1 },
				new LogEntry { Term = 2 },
				new LogEntry { Term = 3 },
				new LogEntry { Term = 4 },
			});
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
		}

		[Fact]
		public void When_the_leader_has_a_newer_commit_index_and_there_are_new_entries()
		{
			var message = new AppendEntriesRpc
			{
				Term = CurrentTerm,
				PreviousLogIndex = 7,
				LeaderCommit = 8,
				Entries = new Dictionary<int, LogEntry>
				{
					[8] = new LogEntry { Term = CurrentTerm },
					[9] = new LogEntry { Term = CurrentTerm }
				}
			};

			_state.OnAppendEntries(message);

			_state.CommitIndex.ShouldBe(8);
		}

		[Fact]
		public void When_the_leader_has_a_newer_commit_index_and_there_are_less_new_entries()
		{
			var message = new AppendEntriesRpc
			{
				Term = CurrentTerm,
				PreviousLogIndex = 7,
				LeaderCommit = 15,
				Entries = new Dictionary<int, LogEntry>
				{
					[8] = new LogEntry { Term = CurrentTerm },
					[9] = new LogEntry { Term = CurrentTerm }
				}
			};

			_state.OnAppendEntries(message);

			_state.CommitIndex.ShouldBe(9);
		}
	}
}
