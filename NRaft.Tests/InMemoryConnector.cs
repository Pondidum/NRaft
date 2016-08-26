using System;
using System.Collections.Generic;
using System.Linq;
using NRaft.Infrastructure;
using NRaft.Messages;

namespace NRaft.Tests
{
	public class InMemoryConnector : IConnector
	{
		private readonly LightweightCache<int, List<Action<AppendEntriesRequest>>> _appendEntriesRpcHandlers;
		private readonly LightweightCache<int, List<Action<AppendEntriesResponse>>> _appendEntriesResponseHandlers;

		private readonly LightweightCache<int, List<Action<RequestVoteRequest>>> _requestVotesHandlers;
		private readonly LightweightCache<int, List<Action<RequestVoteResponse>>> _requestVoteResponseHandlers;
		

		public InMemoryConnector()
		{
			_appendEntriesRpcHandlers = new LightweightCache<int, List<Action<AppendEntriesRequest>>>(
				key => new List<Action<AppendEntriesRequest>>()
			);

			_appendEntriesResponseHandlers = new LightweightCache<int, List<Action<AppendEntriesResponse>>>(
				key => new List<Action<AppendEntriesResponse>>()
			);

			_requestVotesHandlers = new LightweightCache<int, List<Action<RequestVoteRequest>>>(
				key => new List<Action<RequestVoteRequest>>()
			);

			_requestVoteResponseHandlers = new LightweightCache<int, List<Action<RequestVoteResponse>>>(
				key => new List<Action<RequestVoteResponse>>()
			);
		}

		private IEnumerable<Action<T>> Others<T>(LightweightCache<int, List<Action<T>>> collection, int current)
		{
			return collection
				.Dictionary
				.Where(pair => pair.Key != current)
				.SelectMany(pair => pair.Value);
		}

		public void Register(int nodeID, Action<AppendEntriesRequest> handler)
		{
			_appendEntriesRpcHandlers[nodeID].Add(handler);
		}

		public void Register(int nodeID, Action<AppendEntriesResponse> handler)
		{
			_appendEntriesResponseHandlers[nodeID].Add(handler);
		}

		public void Register(int nodeID, Action<RequestVoteRequest> handler)
		{
			_requestVotesHandlers[nodeID].Add(handler);
		}

		public void Register(int nodeID, Action<RequestVoteResponse> handler)
		{
			_requestVoteResponseHandlers[nodeID].Add(handler);
		}

		public void SendReply(AppendEntriesResponse message)
		{
			_appendEntriesResponseHandlers[message.LeaderID].ForEach(handler => handler(message));
		}

		public void SendReply(RequestVoteResponse message)
		{
			_requestVoteResponseHandlers[message.CandidateID].ForEach(handler => handler(message));
		}

		public void RequestVotes(RequestVoteRequest message)
		{
			Others(_requestVotesHandlers, message.CandidateID)
				.ToList()
				.ForEach(handler => handler(message));
		}

		public void SendHeartbeat(AppendEntriesRequest message)
		{
			Others(_appendEntriesRpcHandlers, message.LeaderID)
				.ToList()
				.ForEach(handler => handler(message));
		}
	}
}
