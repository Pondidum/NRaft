using System;
using System.Collections.Generic;
using System.Linq;

namespace NRaft.States
{
	public class Fsm
	{
		private readonly Dictionary<Types, IState> _states;
		private readonly Dictionary<Types, HashSet<Types>> _transitions;
		private IState _current;

		public Fsm(IEnumerable<IState> states)
		{
			_states = states.ToDictionary(s => s.Type, s => s);
			_transitions = new Dictionary<Types, HashSet<Types>>();
		}

		public IState CurrentState => _current;

		public void ResetTo(Types state)
		{
			_current = _states[state];
		}

		public void AddTransition(Types from, Types to)
		{
			_transitions[to] = _transitions[to] ?? new HashSet<Types>();
			_transitions[to].Add(from);
		}

		public void TransitionTo(Types type)
		{
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
}
