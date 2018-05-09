using System;
using NRaft.Messages;
using Serilog;

namespace NRaft.Infrastructure
{
	public class LoggingConnector : IConnector
	{
		private static readonly ILogger Log = Serilog.Log.ForContext<LoggingConnector>();

		private readonly IConnector _other;

		public LoggingConnector(IConnector other)
		{
			_other = other;
		}

		public void Register(int nodeID, Action<AppendEntriesRequest> handler)
		{
			Log.Debug("Node {nodeID} registered for AppendEntriesRequest", nodeID);
			_other.Register(nodeID, handler);
		}

		public void Register(int nodeID, Action<AppendEntriesResponse> handler)
		{
			Log.Debug("Node {nodeID} registered for AppendEntriesResponse", nodeID);
			_other.Register(nodeID, handler);
		}

		public void Register(int nodeID, Action<RequestVoteRequest> handler)
		{
			Log.Debug("Node {nodeID} registered for RequestVoteRequest", nodeID);
			_other.Register(nodeID, handler);
		}

		public void Register(int nodeID, Action<RequestVoteResponse> handler)
		{
			Log.Debug("Node {nodeID} registered for RequestVoteResponse", nodeID);
			_other.Register(nodeID, handler);
		}

		public void Deregister(int nodeID, Action<AppendEntriesRequest> handler)
		{
			Log.Debug("Node {nodeID} deregistered for AppendEntriesRequest", nodeID);
			_other.Deregister(nodeID, handler);
		}

		public void Deregister(int nodeID, Action<AppendEntriesResponse> handler)
		{
			Log.Debug("Node {nodeID} deregistered for AppendEntriesResponse", nodeID);
			_other.Deregister(nodeID, handler);
		}

		public void Deregister(int nodeID, Action<RequestVoteRequest> handler)
		{
			Log.Debug("Node {nodeID} deregistered for RequestVoteRequest", nodeID);
			_other.Deregister(nodeID, handler);
		}

		public void Deregister(int nodeID, Action<RequestVoteResponse> handler)
		{
			Log.Debug("Node {nodeID} deregistered for RequestVoteResponse", nodeID);
			_other.Deregister(nodeID, handler);
		}

		public void SendReply(AppendEntriesResponse message)
		{
			Log.Information("Node {followerID} AppendEntriesResponse => {leaderID}", message.FollowerID, message.LeaderID);
			_other.SendReply(message);
		}

		public void SendReply(RequestVoteResponse message)
		{
			Log.Information("Node {granterID} RequestVoteResponse => {candidateID}", message.GranterID, message.CandidateID);
			_other.SendReply(message);
		}

		public void RequestVotes(RequestVoteRequest message)
		{
			Log.Information("Node {candidateID} RequestVoteRequest => All", message.CandidateID);
			_other.RequestVotes(message);
		}

		public void SendHeartbeat(AppendEntriesRequest message)
		{
			Log.Information("Node {eaderID} AppendEntriesRequest => {recipientID}", message.LeaderID, message.RecipientID);
			_other.SendHeartbeat(message);
		}
	}
}
