using System;
using System.Collections.Generic;
using System.Linq;
using NRaft.Infrastructure;
using NRaft.Messages;
using NRaft.Storage;

namespace NRaft.States
{
	public class Fsm
	{
		private readonly Dictionary<Types, IState> _states;
		private readonly Dictionary<Types, HashSet<Types>> _transitions;
		private IState _current;

		public Fsm(IState[] states)
		{
			_states = states.ToDictionary(s => s.Type, s => s);
			_transitions = new Dictionary<Types, HashSet<Types>>();

			_current = states.First();
		}

		public IState CurrentState => _current;

		public void AddTransition(Types from, Types to)
		{
			_transitions[to] = _transitions[to] ?? new HashSet<Types>();
			_transitions[to].Add(from);
		}

		public void TransitionTo(Types type)
		{
			if (_current.Type == type)
				return;

			var validTransitions = _transitions[type];

			if (validTransitions.Contains(_current.Type) == false)
				throw new InvalidOperationException($"Cannot transition from {_current.Type} to {type}");

			_current.OnExit();
			_current = _states[type];
			_current.OnEnter();
		}
	}

	public interface IState
	{
		Types Type { get; }

		void OnEnter();
		void OnExit();
	}

	public class RaftCoupling : IDisposable
	{
		private readonly IStore _store;
		private readonly Fsm _fsm;
		private readonly IPulseable _heart;

		public RaftCoupling(IClock clock, IConnector connector, IStore store, int nodeID)
		{
			_store = store;
			_fsm = new Fsm(new IState[]
			{
				new FollowerState(connector), 
				new CandidateState(connector), 
				new LeaderState(connector)
			});

			_fsm.AddTransition(Types.Follower, Types.Candidate);
			_fsm.AddTransition(Types.Candidate, Types.Follower);
			_fsm.AddTransition(Types.Candidate, Types.Leader);
			_fsm.AddTransition(Types.Leader, Types.Follower);

			_heart = clock.CreatePulseMonitor(
				TimeSpan.FromMilliseconds(350), //should be random...
				() => _fsm.TransitionTo(Types.Candidate));

			connector.Register(nodeID, OnAppendEntriesRequest);
			connector.Register(nodeID, OnAppendEntriesResponse);
			connector.Register(nodeID, OnRequestVoteRequest);
			connector.Register(nodeID, OnRequestVodeResponse);
		}

		private void OnAppendEntriesRequest(AppendEntriesRequest message)
		{
			_heart.Pulse();

			UpdateTerm(message.Term);
		}

		private void OnAppendEntriesResponse(AppendEntriesResponse message)
		{
			UpdateTerm(message.Term);
		}

		private void OnRequestVoteRequest(RequestVoteRequest message)
		{
			UpdateTerm(message.Term);
		}

		private void OnRequestVodeResponse(RequestVoteResponse message)
		{
			UpdateTerm(message.Term);
		}

		private void UpdateTerm(int messageTerm)
		{
			if (messageTerm <= _store.CurrentTerm)
				return;

			_fsm.TransitionTo(Types.Follower);

			_store.Write(write =>
			{
				write.CurrentTerm = messageTerm;
				write.VotedFor = null;
			});
		}

		public void Dispose()
		{
			_heart.Dispose();
		}
	}

	public class FollowerState : IState
	{
		private readonly IConnector _connector;

		public FollowerState(IConnector connector)
		{
			_connector = connector;
		}

		public Types Type => Types.Follower;

		public void OnEnter()
		{
			throw new NotImplementedException();
		}

		public void OnExit()
		{
			throw new NotImplementedException();
		}
	}

	public class CandidateState : IState
	{
		private readonly IConnector _connector;

		public CandidateState(IConnector connector)
		{
			_connector = connector;
			throw new NotImplementedException();
		}

		public Types Type => Types.Candidate;

		public void OnEnter()
		{
			throw new NotImplementedException();
		}

		public void OnExit()
		{
			throw new NotImplementedException();
		}
	}

	public class LeaderState : IState
	{
		private readonly IConnector _connector;

		public LeaderState(IConnector connector)
		{
			_connector = connector;
			throw new NotImplementedException();
		}

		public Types Type => Types.Leader;

		public void OnEnter()
		{
			throw new NotImplementedException();
		}

		public void OnExit()
		{
			throw new NotImplementedException();
		}
	}
}
