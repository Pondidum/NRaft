using System;
using System.Linq;
using NRaft.Infrastructure;
using Shouldly;
using Xunit;

namespace NRaft.Tests.Infrastructure
{
	public class QuorumTests
	{
		[Theory]
		[InlineData("10", "10")]
		[InlineData("10,20", "10,20")]
		[InlineData("10,20,30", "10,20 - 10,30 - 20,30 - 10,20,30")]
		public void When(string input, string result)
		{
			var nodes = input
				.Split(',')
				.Select(n => Convert.ToInt32(n))
				.ToArray();

			var expected = result
				.Split(new[] { " - " },StringSplitOptions.None)
				.Select(line => line.Split(',')
				.Select(n => Convert.ToInt32(n)));

			Quorum
				.GenerateAllPossibilities(nodes)
				.ShouldBe(expected);
		}
	}
}