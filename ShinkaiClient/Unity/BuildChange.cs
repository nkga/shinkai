using System;
using UnityEngine;
using ShinkaiClient.Unity;
using ShinkaiModel.Core;

namespace ShinkaiClient.Unity {
	public class BuildChange : IDisposable {
		private readonly CameraChange cameraChange;
		private readonly SubRootChange subRootChange;

		public BuildChange(Vector3 position, Quaternion rotation, string subGuid) {
			cameraChange = new CameraChange(position, rotation);

			var subRoot = GuidHelper.FindComponentInChildren<SubRoot>(subGuid);
			if (subRoot == null) {
				if (string.IsNullOrEmpty(subGuid) == false) {
					Log.Warn("Couldn't find subroot: " + subGuid);
				}
			}

			subRootChange = new SubRootChange(subRoot);
		}

		public void Dispose() {
			subRootChange.Dispose();
			cameraChange.Dispose();
		}
	}
}
