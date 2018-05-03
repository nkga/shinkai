using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShinkaiServer.Logic {
	[Serializable]
	public class GameTime {
		public DateTime startTime;

		public GameTime() {
			startTime = DateTime.Now;
		}

		public double GetCurrentTime() {
			TimeSpan interval = DateTime.Now - startTime;
			return interval.TotalSeconds + 480.0;
		}
	}
}
