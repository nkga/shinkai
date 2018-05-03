using System;
using UnityEngine;

namespace ShinkaiClient.Unity {
	public class CameraChange : IDisposable {
		public readonly Vector3 backupPosition;
		public readonly Quaternion backupRotation;

		public CameraChange(Vector3 position, Quaternion rotation) {
			if (MainCamera.camera != null) {
				backupPosition = MainCamera.camera.transform.position;
				backupRotation = MainCamera.camera.transform.rotation;

				MainCamera.camera.transform.position = position;
				MainCamera.camera.transform.rotation = rotation;
			}
		}

		public void Dispose() {
			if (MainCamera.camera != null) {
				MainCamera.camera.transform.position = backupPosition;
				MainCamera.camera.transform.rotation = backupRotation;
			}
		}
	}
}
