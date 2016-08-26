namespace NRaft.Messages
{
	public class RequestVoteResponse
	{
		public int NodeID { get; set; }
		public int Term { get; set; }
		public bool VoteGranted { get; set; }
	}
}
