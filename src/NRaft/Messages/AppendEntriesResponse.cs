namespace NRaft.Messages
{
	public class AppendEntriesResponse
	{
		public int LeaderID { get; set; }
		public int FollowerID { get; set; }
		public int Term { get; set; }
		public bool Success { get; set; }
		public int MatchIndex { get; set; }
	}
}
