using System;
using UnityEngine;

namespace ShinkaiClient.Unity {
	public class ParentChange : IDisposable {
		public readonly GameObject target;
		public readonly Transform backupParent;

		public ParentChange(GameObject obj, Transform parent) {
			if (obj != null) {
				target = obj;
				backupParent = obj.transform.parent;
			}
		}

		public void Dispose() {
			if (target != null) {
				target.transform.parent = backupParent;
			}
		}
	}
}
