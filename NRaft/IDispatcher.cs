namespace NRaft
{
	public interface IDispatcher
	{
		void SendReply(AppendEntriesResponse message);
		void SendReply(RequestVoteResponse message);
	}
}
