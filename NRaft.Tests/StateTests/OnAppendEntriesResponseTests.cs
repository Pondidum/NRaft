﻿using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NRaft.Tests.StateTests
{
	public class OnAppendEntriesResponseTests
	{
		private const int CurrentTerm = 3;

		private readonly InMemoryStore _store;
		private readonly IConnector _connector;
		private readonly State _state;

		public OnAppendEntriesResponseTests()
		{
			_store = new InMemoryStore();
			_store.CurrentTerm = CurrentTerm;

			_connector = Substitute.For<IConnector>();

			_state = new State(_store, _connector, 10);
			_state.ForceType(Types.Leader);
		}

		[Fact]
		public void When_a_message_has_a_newer_term()
		{
			_state.OnAppendEntriesResponse(new AppendEntriesResponse
			{
				Term = CurrentTerm + 1,
			});

			_store.CurrentTerm.ShouldBe(CurrentTerm + 1);
		}

		[Fact]
		public void When_the_terms_are_not_equal()
		{
			var message = new AppendEntriesResponse
			{
				FollowerID = 20,
				Term = CurrentTerm - 2,
				Success = true
			};

			_state.OnAppendEntriesResponse(message);

			_store.CurrentTerm.ShouldBe(CurrentTerm);
			_state.NextIndexFor(message.FollowerID).ShouldBe(1);
		}

		[Fact]
		public void When_the_message_is_not_successful()
		{
			var message = new AppendEntriesResponse
			{
				FollowerID = 20,
				Term = CurrentTerm,
				Success = false,
				MatchIndex = 11,
			};

			_state.OnAppendEntriesResponse(message);

			_state.NextIndexFor(message.FollowerID).ShouldBe(10);
		}

		[Fact]
		public void When_the_message_is_not_successful_the_nextindex_cannot_drop_below_1()
		{
			var message = new AppendEntriesResponse
			{
				FollowerID = 20,
				Term = CurrentTerm,
				Success = false,
				MatchIndex = 1,
			};

			_state.OnAppendEntriesResponse(message);

			_state.NextIndexFor(message.FollowerID).ShouldBe(1);
		}

		[Fact]
		public void When_the_message_is_successful()
		{
			var message = new AppendEntriesResponse
			{
				FollowerID = 20,
				Term = CurrentTerm,
				Success = true,
				MatchIndex = 10,
			};

			_state.OnAppendEntriesResponse(message);

			_state.NextIndexFor(message.FollowerID).ShouldBe(11);
			_state.MatchIndexFor(message.FollowerID).ShouldBe(10);
		}
	}
}
