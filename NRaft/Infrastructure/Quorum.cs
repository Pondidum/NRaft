using System.Collections.Generic;
using System.Linq;

namespace NRaft.Infrastructure
{
	public static class Quorum
	{
		public static IEnumerable<int[]> GenerateAllPossibilities(int[] nodeIDs)
		{
			var ps = PowerSet(nodeIDs);
			var quorum = ps.Where(i => i.Length * 2 > nodeIDs.Length);

			return quorum;
		}

		private static List<T[]> PowerSet<T>(T[] input)
		{
			var n = input.Length;
			var powerSetCount = 1 << n; // Power set contains 2^N subsets.

			var ans = new List<T[]>();

			for (var setMask = 0; setMask < powerSetCount; setMask++)
			{
				var s = new List<T>();
				for (var i = 0; i < n; i++)
				{
					// Checking whether i'th element of input collection should go to the current subset.
					if ((setMask & (1 << i)) > 0)
					{
						s.Add(input[i]);
					}
				}
				ans.Add(s.ToArray());
			}

			return ans;

		}
	}
}
