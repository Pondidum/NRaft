namespace NRaft.Storage
{
	public class InMemoryStore : IStore
	{
		public int CurrentTerm { get; set; }
	}
}
