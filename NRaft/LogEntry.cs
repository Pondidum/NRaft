using System;

namespace NRaft
{
	public class LogEntry : IEquatable<LogEntry>
	{
		public int Index { get; set; }
		public int Term { get; set; }
		public string Command { get; set; }

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;

			return Equals((LogEntry)obj);
		}

		public bool Equals(LogEntry other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Index == other.Index && Term == other.Term;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Index * 397) ^ Term;
			}
		}

		public override string ToString()
		{
			return $"Index {Index}, Term {Term}";
		}

		public static bool operator ==(LogEntry left, LogEntry right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(LogEntry left, LogEntry right)
		{
			return !Equals(left, right);
		}
	}
}
