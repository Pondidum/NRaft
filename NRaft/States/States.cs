using System;
using System.Collections.Generic;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;

namespace NRaft.States
{

	public class Coordinator
	{
		private readonly IPulseable _heart;
		private State _state;

		public Coordinator(IClock clock, IStore store)
		{
			_heart = clock.CreatePulseMonitor(TimeSpan.FromMilliseconds(350), OnHeartbeatElapsed); //should be random...
			_state = new Follower(new NodeInfo(store, () =>
			{
				
				
			} ));
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
		private readonly IStore _store;
		private readonly Action _makeFollower;

		public int CurrentTerm => _store.CurrentTerm;
		public int NodeID { get; }

		public NodeInfo(IStore store, Action makeFollower, int nodeID)
		{
			_store = store;
			_makeFollower = makeFollower;
			NodeID = nodeID;
		}

		public void Store(Action<IStoreWriter> apply)
		{
			_store.Write(apply);
		}

		private void UpdateTerm(int messageTerm)
		{
			if (messageTerm <= _store.CurrentTerm)
				return;

			_makeFollower();

			_store.Write(write =>
			{
				write.CurrentTerm = messageTerm;
				write.VotedFor = null;
			});
		}
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
		private readonly HashSet<int> _votesResponded;
		private readonly HashSet<int> _votesGranted;

		private readonly NodeInfo _info;

		public Candidate(NodeInfo info)
		{
			_info = info;

			_votesResponded = new HashSet<int>();
			_votesGranted = new HashSet<int>();

			_info.Store(write =>
			{
				write.CurrentTerm = _info.CurrentTerm + 1;
				write.VotedFor = _info.NodeID;
			});

			OnRequestVoteResponse(new RequestVoteResponse
			{
				GranterID = _info.NodeID,
				Term = _info.CurrentTerm,
				VoteGranted = true
			});

			_connector.RequestVotes(new RequestVoteRequest
			{
				CandidateID = _info.NodeID,
				Term = _info.CurrentTerm,
				LastLogIndex = LastIndex(),
				LastLogTerm = LastTerm()
			});

			_election = _clock.CreateTimeout(TimeSpan.FromMilliseconds(500), OnElectionTimeout); //or whatever the electiontimeout is
		}

		public Follower BecomeFollower()
		{
			return new Follower(_info);
		}

		public Leader BecomeLeader()
		{
			return new Leader(_info);
		}

		public void OnRequestVoteResponse(RequestVoteResponse message)
		{
			_info.UpdateTerm(message.Term);

			if (message.Term != _info.CurrentTerm)
				return;

			_votesResponded.Add(message.GranterID);

			if (message.VoteGranted)
				_votesGranted.Add(message.GranterID);
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
