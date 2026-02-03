using System.Diagnostics;

namespace Practice.Utils
{
	public static class NetworkTimer
	{
		private static readonly Stopwatch sw = Stopwatch.StartNew();

		public static long NowMs() => sw.ElapsedMilliseconds;
		public static long NowTicks() => sw.ElapsedTicks;
	}
}
