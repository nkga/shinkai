using UnityEngine;

namespace ShinkaiClient.Unity {
	public class Attacher : MonoBehaviour {
		public Transform target;

		private void Awake() {
			target = null;
		}

		private void LateUpdate() {
			if (target) {
				transform.position = target.position;
				transform.rotation = target.rotation;
			}
		}
	}
}
