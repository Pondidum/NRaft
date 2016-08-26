namespace NRaft.Storage
{
	public class InMemoryStore : IStore
	{
		public int CurrentTerm { get; set; }
		public int? VotedFor { get; set; }
	}
}
