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
			return new Candidate(_info);
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
			return new Follower(_info);
		}

		public Leader BecomeLeader()
		{
			return new Leader(_info);
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
			return new Follower(_info);
		}
	}
}
