using System;
using NRaft.Messages;

namespace NRaft.Infrastructure
{
	public class LoggingConnector : IConnector
	{
		private readonly IConnector _other;
		private readonly Action<string> _write;

		public LoggingConnector(IConnector other, Action<string> write)
		{
			_other = other;
			_write = write;
		}

		public void Register(int nodeID, Action<AppendEntriesRequest> handler)
		{
			_write($"Node {nodeID} registered for AppendEntriesRequest");
			_other.Register(nodeID, handler);
		}

		public void Register(int nodeID, Action<AppendEntriesResponse> handler)
		{
			_write($"Node {nodeID} registered for AppendEntriesResponse");
			_other.Register(nodeID, handler);
		}

		public void Register(int nodeID, Action<RequestVoteRequest> handler)
		{
			_write($"Node {nodeID} registered for RequestVoteRequest");
			_other.Register(nodeID, handler);
		}

		public void Register(int nodeID, Action<RequestVoteResponse> handler)
		{
			_write($"Node {nodeID} registered for RequestVoteResponse");
			_other.Register(nodeID, handler);
		}

		public void Deregister(int nodeID, Action<AppendEntriesRequest> handler)
		{
			_write($"Node {nodeID} deregistered for AppendEntriesRequest");
			_other.Deregister(nodeID, handler);
		}

		public void Deregister(int nodeID, Action<AppendEntriesResponse> handler)
		{
			_write($"Node {nodeID} deregistered for AppendEntriesResponse");
			_other.Deregister(nodeID, handler);
		}

		public void Deregister(int nodeID, Action<RequestVoteRequest> handler)
		{
			_write($"Node {nodeID} deregistered for RequestVoteRequest");
			_other.Deregister(nodeID, handler);
		}

		public void Deregister(int nodeID, Action<RequestVoteResponse> handler)
		{
			_write($"Node {nodeID} deregistered for RequestVoteResponse");
			_other.Deregister(nodeID, handler);
		}

		public void SendReply(AppendEntriesResponse message)
		{
			_write($"Node {message.FollowerID} AppendEntriesResponse => {message.LeaderID}");
			_other.SendReply(message);
		}

		public void SendReply(RequestVoteResponse message)
		{
			_write($"Node {message.GranterID} RequestVoteResponse => {message.CandidateID}");
			_other.SendReply(message);
		}

		public void RequestVotes(RequestVoteRequest message)
		{
			_write($"Node {message.CandidateID} RequestVoteRequest => All");
			_other.RequestVotes(message);
		}

		public void SendHeartbeat(AppendEntriesRequest message)
		{
			_write($"Node {message.LeaderID} AppendEntriesRequest => {message.RecipientID}");
			_other.SendHeartbeat(message);
		}
	}
}
