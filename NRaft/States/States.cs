using System;

namespace NRaft.States
{

	public class NodeInfo
	{
		
	}

	public class Follower
	{
		private readonly NodeInfo _info;

		public Follower(NodeInfo info)
		{
			_info = info;
		}

		public Candidate BecomeCandidate()
		{
			throw new NotImplementedException();
		}
	}

	public class Candidate
	{
		private readonly NodeInfo _info;

		public Candidate(NodeInfo info)
		{
			_info = info;
		}

		public Follower BecomeFollower()
		{
			throw new NotImplementedException();
		}

		public Leader BecomeLeader()
		{
			throw new NotImplementedException();
		}
	}

	public class Leader
	{
		private readonly NodeInfo _info;

		public Leader(NodeInfo info)
		{
			_info = info;
		}

		public Follower BecomeFollower()
		{
			throw new NotImplementedException();
		}
	}
}
