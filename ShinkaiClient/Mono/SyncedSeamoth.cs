using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;

namespace ShinkaiClient.Mono {
	public class SyncedSeamoth : SyncedVehicle {
		private bool lastThrottle = false;
		private SeaMoth seamoth;

		protected override void Awake() {
			seamoth = GetComponent<SeaMoth>();

			base.Awake();
		}

		public override void Exit() {
			seamoth.bubbles.Stop();
			base.Exit();
		}

		public override void SetAnimation(ClientVehicleMovement msg) {
			bool state = msg.throttle;

			if (state != lastThrottle) {
				if (state) {
					seamoth.bubbles.Play();
				} else {
					seamoth.bubbles.Stop();
				}

				lastThrottle = state;
			}
		}
	}
}
