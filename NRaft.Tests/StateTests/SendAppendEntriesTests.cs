﻿using System.Collections.Generic;
using System.Linq;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class SendAppendEntriesTests
	{
		private const int NodeID = 1234;

		private readonly IConnector _connector;
		private readonly State _state;
		private readonly List<AppendEntriesRequest> _messages;
		private readonly InMemoryStore _store;

		public SendAppendEntriesTests()
		{
			_store = new InMemoryStore();
			_messages = new List<AppendEntriesRequest>();

			_connector = Substitute.For<IConnector>();
			_connector
				.When(d => d.SendHeartbeat(Arg.Any<AppendEntriesRequest>()))
				.Do(cb => _messages.Add(cb.Arg<AppendEntriesRequest>()));

			_state = new State(_store,_connector, NodeID);
		}

		[Fact]
		public void When_there_are_no_nodes_to_send_to()
		{
			_state.ForceLog(
				new LogEntry { Index = 1, Term = _store.CurrentTerm },
				new LogEntry { Index = 2, Term = _store.CurrentTerm }
			);

			_state.SendAppendEntries();

			_connector.DidNotReceive().SendHeartbeat(Arg.Any<AppendEntriesRequest>());
		}

		[Fact]
		public void When_there_is_one_node_and_no_pending_entries_to_be_sent()
		{
			_state.AddNodeToCluster(456);

			_state.SendAppendEntries();

			_connector.Received().SendHeartbeat(Arg.Any<AppendEntriesRequest>());

			var message = _messages.Single();

			message.ShouldSatisfyAllConditions(
				() => message.LeaderID.ShouldBe(NodeID),
				() => message.Term.ShouldBe(_store.CurrentTerm),
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
				new LogEntry { Index = 1, Term = _store.CurrentTerm },
				new LogEntry { Index = 2, Term = _store.CurrentTerm }
			);

			_state.SendAppendEntries();

			_connector.Received().SendHeartbeat(Arg.Any<AppendEntriesRequest>());

			var message = _messages.Single();

			message.ShouldSatisfyAllConditions(
				() => message.LeaderID.ShouldBe(NodeID),
				() => message.Term.ShouldBe(_store.CurrentTerm),
				() => message.RecipientID.ShouldBe(456),
				() => message.LeaderCommit.ShouldBe(0),
				() => message.PreviousLogIndex.ShouldBe(0),
				() => message.PreviousLogTerm.ShouldBe(0),
				() => message.Entries.ShouldBe(new[]
				{
					new LogEntry { Index = 1, Term = _store.CurrentTerm },
					//currently the impl is only sending 1 entry at a time.
					//future optimisation will support multiple
					//new LogEntry { Index = 2, Term = _store.CurrentTerm }
				})
			);
		}

		[Fact]
		public void When_there_are_multiple_nodes_and_some_entries_to_be_sent()
		{
			_state.AddNodeToCluster(456);
			_state.AddNodeToCluster(789);

			_state.ForceLog(
				new LogEntry { Index = 1, Term = _store.CurrentTerm },
				new LogEntry { Index = 2, Term = _store.CurrentTerm }
			);

			_state.SendAppendEntries();

			_connector.Received(2).SendHeartbeat(Arg.Any<AppendEntriesRequest>());
		}
	}
}
