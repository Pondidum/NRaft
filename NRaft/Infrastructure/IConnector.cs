using NRaft.Messages;

namespace NRaft.Infrastructure
{
	public interface IConnector
	{
		void SendReply(AppendEntriesResponse message);
		void SendReply(RequestVoteResponse message);

		void RequestVotes(RequestVoteRequest message);
		void SendHeartbeat(AppendEntriesRequest message);
	}
}
