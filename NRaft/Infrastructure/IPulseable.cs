using System;

namespace NRaft.Infrastructure
{
	public interface IPulseable : IDisposable
	{
		void Pulse();
	}
}