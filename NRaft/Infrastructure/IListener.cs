using System;
using NRaft.Messages;

namespace NRaft.Infrastructure
{
	public interface IListener
	{
		void Register(int nodeID, Action<AppendEntriesRequest> handler);
		void Register(int nodeID, Action<AppendEntriesResponse> handler);
		void Register(int nodeID, Action<RequestVoteRequest> handler);
		void Register(int nodeID, Action<RequestVoteResponse> handler);
	}
}
