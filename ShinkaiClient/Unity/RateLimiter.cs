using UnityEngine;

namespace ShinkaiClient.Unity {
	public class RateLimiter {
		private float lastTime = 0f;
		private float accum = 0f;

		public void Reset() {
			accum = 0f;
		}

		public bool Update(float rate) {
			var time = Time.time;

			if (lastTime != time) {
				lastTime = time;

				accum += Time.deltaTime;

				if (accum > rate) {
					accum = 0f;
					return true;
				}
			}

			return false;
		}
	}
}
