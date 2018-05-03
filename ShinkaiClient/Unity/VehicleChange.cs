using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShinkaiModel.Core;

namespace ShinkaiClient.Unity {
	public class VehicleChange : IDisposable {
		public readonly Vehicle backupVehicle;

		public VehicleChange() {
			if (Player.main != null) {
				var vehicle = Player.main.GetVehicle();
				if (vehicle.GetComponent<SubRoot>() == null) {
					backupVehicle = vehicle;
					Player.main.ExitLockedMode(false, false);
				}
			}
		}

		public void Dispose() {
			if (Player.main != null) {
				if (backupVehicle != null) {
					backupVehicle.ReflectionCall("EnterVehicle", false, false, new object[] { Player.main, true, false });
				}
			}
		}
	}
}
