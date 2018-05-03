using System.Diagnostics;
using System.Threading;

namespace ShinkaiServer.Helpers {
	public class Sleeper {
		private Stopwatch stopwatch;
		private long lastMs;
		private int rateMs;

		public Sleeper(int rateMs) {
			this.rateMs = rateMs;
			stopwatch = Stopwatch.StartNew();
		}

		public void Update() {
			long nowMs = stopwatch.ElapsedMilliseconds;
			int diffMs = (int)(nowMs - lastMs);
			lastMs = nowMs;

			if (diffMs < rateMs) {
				int time = rateMs - diffMs;
				if (time > 0 && time < 10) {
					Thread.Sleep(time);
				}
			}
		}
	}
}
