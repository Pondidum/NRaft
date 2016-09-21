using System;
using System.Threading.Tasks;
using NRaft.Timing;
using Shouldly;
using Xunit;

namespace NRaft.Tests.Timings
{
	public class HeartTests
	{
		private readonly IHeart _heart;

		public HeartTests()
		{
			//_heart = new Heart();
		}

		[Fact]
		public void When_started_and_not_connected()
		{
			Should.Throw<InvalidOperationException>(() => _heart.StartPulsing(TimeSpan.FromSeconds(5)));
		}

		[Fact]
		public async Task When_started()
		{
			var beats = 0;
			_heart.ConnectTo(() => beats++);
			_heart.StartPulsing(TimeSpan.FromMilliseconds(50));

			await Task.Delay(75);

			beats.ShouldBe(1);
		}

		[Fact]
		public async Task When_started_twice()
		{
			var beats = 0;
			_heart.ConnectTo(() => beats++);

			_heart.StartPulsing(TimeSpan.FromMilliseconds(50));
			_heart.StartPulsing(TimeSpan.FromMilliseconds(50));

			await Task.Delay(75);

			beats.ShouldBe(1);
		}

		[Fact]
		public async Task When_a_beating_heart_is_stopped()
		{
			var beats = 0;
			_heart.ConnectTo(() => beats++);

			_heart.StartPulsing(TimeSpan.FromMilliseconds(50));

			await Task.Delay(75);

			_heart.StopPulsing();

			await Task.Delay(75);

			beats.ShouldBe(1);
		}

		[Fact]
		public async Task A_stopped_heart_can_be_started()
		{

			var beats = 0;
			var resurrectedBeats = 0;

			_heart.ConnectTo(() => beats++);
			_heart.StartPulsing(TimeSpan.FromMilliseconds(50));

			await Task.Delay(75);

			_heart.StopPulsing();

			_heart.ConnectTo(() => resurrectedBeats++);
			_heart.StartPulsing(TimeSpan.FromMilliseconds(50));

			await Task.Delay(75);

			beats.ShouldBe(1);
			resurrectedBeats.ShouldBe(1);
		}
	}
}
