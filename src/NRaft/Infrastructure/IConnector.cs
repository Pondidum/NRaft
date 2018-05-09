using System;
using NRaft.Messages;

namespace NRaft.Infrastructure
{
	public interface IConnector
	{
		void Register(int nodeID, Action<AppendEntriesRequest> handler);
		void Register(int nodeID, Action<AppendEntriesResponse> handler);
		void Register(int nodeID, Action<RequestVoteRequest> handler);
		void Register(int nodeID, Action<RequestVoteResponse> handler);


		void Deregister(int nodeID, Action<AppendEntriesRequest> handler);
		void Deregister(int nodeID, Action<AppendEntriesResponse> handler);
		void Deregister(int nodeID, Action<RequestVoteRequest> handler);
		void Deregister(int nodeID, Action<RequestVoteResponse> handler);

		void SendReply(AppendEntriesResponse message);
		void SendReply(RequestVoteResponse message);

		void RequestVotes(RequestVoteRequest message);
		void SendHeartbeat(AppendEntriesRequest message);
	}
}
