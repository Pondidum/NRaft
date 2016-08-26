namespace NRaft.Storage
{
	public interface IStore
	{
		int CurrentTerm { get; set; }
	}
}
