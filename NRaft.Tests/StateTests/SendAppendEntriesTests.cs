using System.Collections.Generic;
using System.Linq;
using NRaft.Messages;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class SendAppendEntriesTests
	{
		private const int NodeID = 1234;

		private readonly IDispatcher _dispatcher;
		private readonly State _state;
		private readonly List<AppendEntriesRpc> _messages;

		public SendAppendEntriesTests()
		{
			_messages = new List<AppendEntriesRpc>();

			_dispatcher = Substitute.For<IDispatcher>();
			_dispatcher
				.When(d => d.SendHeartbeat(Arg.Any<AppendEntriesRpc>()))
				.Do(cb => _messages.Add(cb.Arg<AppendEntriesRpc>()));

			_state = new State(_dispatcher, NodeID);
		}

		[Fact]
		public void When_there_are_no_nodes_to_send_to()
		{
			_state.ForceLog(
				new LogEntry { Index = 1, Term = _state.CurrentTerm },
				new LogEntry { Index = 2, Term = _state.CurrentTerm }
			);

			_state.SendAppendEntries();

			_dispatcher.DidNotReceive().SendHeartbeat(Arg.Any<AppendEntriesRpc>());
		}

		[Fact]
		public void When_there_is_one_node_and_no_pending_entries_to_be_sent()
		{
			_state.AddNodeToCluster(456);

			_state.SendAppendEntries();

			_dispatcher.Received().SendHeartbeat(Arg.Any<AppendEntriesRpc>());

			var message = _messages.Single();

			message.ShouldSatisfyAllConditions(
				() => message.LeaderID.ShouldBe(NodeID),
				() => message.Term.ShouldBe(_state.CurrentTerm),
				() => message.RecipientID.ShouldBe(456),
				() => message.LeaderCommit.ShouldBe(0),
				() => message.PreviousLogIndex.ShouldBe(0),
				() => message.PreviousLogTerm.ShouldBe(0),
				() => message.Entries.ShouldBeEmpty()
			);
		}

		[Fact]
		public void When_there_is_one_node_and_some_entries_to_be_sent()
		{
			_state.AddNodeToCluster(456);
			_state.ForceLog(
				new LogEntry { Index = 1, Term = _state.CurrentTerm },
				new LogEntry { Index = 2, Term = _state.CurrentTerm }
			);

			_state.SendAppendEntries();

			_dispatcher.Received().SendHeartbeat(Arg.Any<AppendEntriesRpc>());

			var message = _messages.Single();

			message.ShouldSatisfyAllConditions(
				() => message.LeaderID.ShouldBe(NodeID),
				() => message.Term.ShouldBe(_state.CurrentTerm),
				() => message.RecipientID.ShouldBe(456),
				() => message.LeaderCommit.ShouldBe(0),
				() => message.PreviousLogIndex.ShouldBe(0),
				() => message.PreviousLogTerm.ShouldBe(0),
				() => message.Entries.ShouldBe(new[]
				{
					new LogEntry { Index = 1, Term = _state.CurrentTerm },
					//currently the impl is only sending 1 entry at a time.
					//future optimisation will support multiple
					//new LogEntry { Index = 2, Term = _state.CurrentTerm }
				})
			);
		}

		[Fact]
		public void When_there_are_multiple_nodes_and_some_entries_to_be_sent()
		{
			_state.AddNodeToCluster(456);
			_state.AddNodeToCluster(789);

			_state.ForceLog(
				new LogEntry { Index = 1, Term = _state.CurrentTerm },
				new LogEntry { Index = 2, Term = _state.CurrentTerm }
			);

			_state.SendAppendEntries();

			_dispatcher.Received(2).SendHeartbeat(Arg.Any<AppendEntriesRpc>());
		}
	}
}
