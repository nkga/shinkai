using System.Reflection;
using UnityEngine;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;
using ShinkaiClient.Unity;

namespace ShinkaiClient.Mono {
	public abstract class SyncedVehicle : MonoBehaviour {
		public RemotePlayer activePlayer;
		protected SmoothFloat smoothYaw;
		protected SmoothFloat smoothPitch;
		protected SmoothTransform smoothTransform;
		protected SyncedObject syncedObject;

		protected virtual void Awake() {
			activePlayer = null;
			smoothYaw = new SmoothFloat();
			smoothPitch = new SmoothFloat();
			smoothTransform = gameObject.EnsureComponent<SmoothTransform>();
			syncedObject = gameObject.EnsureComponent<SyncedObject>();
		}

		protected virtual void Update() {
			smoothYaw.Update();
			smoothPitch.Update();

			if (activePlayer != null) {
				var mainAnimator = GetAnimator();
				if (mainAnimator != null) {
					mainAnimator.SetFloat("view_yaw", smoothYaw.value * 70f);
					mainAnimator.SetFloat("view_pitch", smoothPitch.value * 45f);
				}

				syncedObject.updating = false;
			} else {
				syncedObject.updating = true;
			}
		}

		public void Correct(Vector3 location, Quaternion rotation, Vector3 velocity, double newTimestamp) {
			gameObject.SetActive(true);
			smoothTransform.Correct(location, rotation, velocity, newTimestamp);
		}

		public virtual void SetSteeringWheel(float yaw, float pitch) {
			smoothYaw.target = yaw;
			smoothPitch.target = pitch;
		}

		public virtual void Enter(RemotePlayer player) {
			activePlayer = player;
			smoothTransform.SetEnabled();
		}

		public virtual void Exit() {
			activePlayer = null;
			smoothTransform.SetDisabled();
		}

		protected virtual Animator GetAnimator() {
			return null;
		}

		public abstract void SetAnimation(ClientVehicleMovement msg);
	}
}
