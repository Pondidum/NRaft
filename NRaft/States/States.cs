using System;
using NRaft.Infrastructure;

namespace NRaft.States
{

	public class Coordinator
	{
		private readonly IPulseable _heart;
		private State _state;

		public Coordinator(IClock clock)
		{
			_heart = clock.CreatePulseMonitor(TimeSpan.FromMilliseconds(350), OnHeartbeatElapsed); //should be random...
			_state = new Follower(new NodeInfo());
		}

		//I wanted to call this OnHeartFailure...
		private void OnHeartbeatElapsed()
		{
			var follower = _state as Follower;

			if (follower != null)
				_state = follower.BecomeCandidate();
		}
	}

	public class NodeInfo
	{
	}

	public class State { }

	public class Follower : State
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

	public class Candidate : State
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

	public class Leader : State
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
