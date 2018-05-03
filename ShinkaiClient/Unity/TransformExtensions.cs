using UnityEngine;

namespace ShinkaiClient.Unity {
	public static class TransformExtensions {
		public static Transform FindInChildren(this Transform parent, string name) {
			var result = parent.Find(name);
			if (result != null) {
				return result;
			}

			foreach (Transform child in parent) {
				result = child.FindInChildren(name);
				if (result != null) {
					return result;
				}
			}

			return null;
		}
	}
}
