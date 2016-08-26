namespace NRaft.Messages
{
	public class RequestVoteRequest
	{
		public int Term { get; set; }
		public int CandidateID { get; set; }
		public int LastLogIndex { get; set; }
		public int LastLogTerm { get; set; }
	}
}
