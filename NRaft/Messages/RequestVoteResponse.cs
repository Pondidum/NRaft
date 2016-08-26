namespace NRaft.Messages
{
	public class RequestVoteResponse
	{
		public int CandidateID { get; set; }
		public int GranterID { get; set; }
		public int Term { get; set; }
		public bool VoteGranted { get; set; }
	}
}
