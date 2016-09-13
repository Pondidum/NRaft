using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NRaft.Tests.TestInfrastructure;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.NodeTests
{
	public class OnAppendEntriesResponseTests
	{
		private readonly InMemoryStore _store;
		private readonly IConnector _connector;
		private readonly Node _node;
		private readonly ControllableClock _clock;

		public OnAppendEntriesResponseTests()
		{
			_store = new InMemoryStore();
			_store.CurrentTerm = 3;
			_clock = new ControllableClock();
			_connector = Substitute.For<IConnector>();

			_node = new Node(_store, _clock, _connector, 10);
			_clock.EndCurrentHeartbeat();
			_clock.EndCurrentElection();
		}

		[Fact]
		public void When_a_message_has_a_newer_term()
		{
			var nextTerm = _store.CurrentTerm + 1;
			_node.OnAppendEntriesResponse(new AppendEntriesResponse
			{
				Term = nextTerm,
			});

			_store.CurrentTerm.ShouldBe(nextTerm);
		}

		[Fact]
		public void When_the_terms_are_not_equal()
		{
			var currentTerm = _store.CurrentTerm;

			var message = new AppendEntriesResponse
			{
				FollowerID = 20,
				Term = currentTerm - 2,
				Success = true
			};

			_node.OnAppendEntriesResponse(message);

			_store.CurrentTerm.ShouldBe(currentTerm);
			_node.NextIndexFor(message.FollowerID).ShouldBe(1);
		}

		[Fact]
		public void When_the_message_is_not_successful()
		{
			var message = new AppendEntriesResponse
			{
				FollowerID = 20,
				Term = _store.CurrentTerm,
				Success = false,
				MatchIndex = 11,
			};

			_node.OnAppendEntriesResponse(message);

			_node.NextIndexFor(message.FollowerID).ShouldBe(10);
		}

		[Fact]
		public void When_the_message_is_not_successful_the_nextindex_cannot_drop_below_1()
		{
			var message = new AppendEntriesResponse
			{
				FollowerID = 20,
				Term = _store.CurrentTerm,
				Success = false,
				MatchIndex = 1,
			};

			_node.OnAppendEntriesResponse(message);

			_node.NextIndexFor(message.FollowerID).ShouldBe(1);
		}

		[Fact]
		public void When_the_message_is_successful()
		{
			var message = new AppendEntriesResponse
			{
				FollowerID = 20,
				Term = _store.CurrentTerm,
				Success = true,
				MatchIndex = 10,
			};

			_node.OnAppendEntriesResponse(message);

			_node.NextIndexFor(message.FollowerID).ShouldBe(11);
			_node.MatchIndexFor(message.FollowerID).ShouldBe(10);
		}
	}
}
