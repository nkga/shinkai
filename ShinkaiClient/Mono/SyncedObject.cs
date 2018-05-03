using UnityEngine;
using ShinkaiModel.Networking;
using ShinkaiClient.Mono;
using ShinkaiClient.Unity;

namespace ShinkaiClient.Mono {
	public class SyncedObject : MonoBehaviour {
		public const float rate = 1.0f;
		public const float maxDist = 0.50f;
		public const float maxAngle = 15.0f;
		public const float maxRange = 15.0f;

		public bool updating;
		private Vector3 lastPosition;
		private Quaternion lastRotation;
		private Rigidbody rigidbody;
		private RateLimiter rateLimiter = new RateLimiter();
		private ClientObjectUpdate res = new ClientObjectUpdate();

		public void Awake() {
			rigidbody = GetComponent<Rigidbody>();
			lastPosition = transform.position;
			lastRotation = transform.rotation;
			updating = true;

			res.targetGuid = GuidHelper.Get(gameObject);
		}

		public void Update() {
			if (updating == false) {
				return;
			}
			
			if (rateLimiter.Update(rate) == false) {
				return;
			}

			if (Vector3.SqrMagnitude(Player.main.transform.position - transform.position) > maxRange * maxRange) {
				return;
			}
				
			if (Vector3.SqrMagnitude(transform.position - lastPosition) > maxDist * maxDist || Quaternion.Angle(transform.rotation, lastRotation) > maxAngle) {
				res.timestamp = DayNightCycle.main.timePassedAsDouble;
				res.position = transform.position;
				res.rotation = transform.rotation;
				Multiplayer.main.Send(res);

				lastPosition = transform.position;
				lastRotation = transform.rotation;
			}
		}

		public void Correct(Vector3 position, Quaternion rotation) {
			lastPosition = position;
			lastRotation = rotation;

			if (rigidbody != null) {
				rigidbody.MovePosition(position);
				rigidbody.MoveRotation(rotation);
			} else {
				transform.position = position;
				transform.rotation = rotation;
			}

			rateLimiter.Reset();
		}

		public static void ApplyTo(GameObject gameObject) {
			gameObject.EnsureComponent<SyncedObject>();
		}

		public static void RemoveFrom(GameObject gameObject) {
			var component = gameObject.GetComponent<SyncedObject>();
			if (component != null) {
				UnityEngine.Object.Destroy(component);
			}
		}
	}
}
