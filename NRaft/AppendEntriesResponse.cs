namespace NRaft
{
	public class AppendEntriesResponse
	{
		public int FollowerID { get; set; }
		public int Term { get; set; }
		public bool Success { get; set; }
		public int MatchIndex { get; set; }
	}
}
