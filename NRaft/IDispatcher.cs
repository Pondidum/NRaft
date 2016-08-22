namespace NRaft
{
	public interface IDispatcher
	{
		void SendReply(AppendEntriesResponse message);
		void SendReply(RequestVoteResponse message);

		void RequestVotes(RequestVoteRpc message);
		void SendHeartbeat(AppendEntriesRpc message);
	}
}
