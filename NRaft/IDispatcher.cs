namespace NRaft
{
	public interface IDispatcher
	{
		void SendReply(AppendEntriesResponse message);
	}
}
