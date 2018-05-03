using UnityEngine;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;

namespace ShinkaiClient.Mono {
	public class SyncedCyclops : SyncedVehicle {
		private SubRoot subroot;
		private Rigidbody rigidbody;
		private Stabilizer stabilizer;
		private SubControl control;
		private ISubTurnHandler[] subTurnHandlers;
		private ISubThrottleHandler[] subThrottleHandlers;
		private float previousAbsYaw = 0f;

		private Vector3 velocity;
		private Vector3 angularVelocity;

		protected override void Awake() {
			rigidbody = GetComponent<Rigidbody>();
			if (rigidbody != null) {
				rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			}

			subroot = gameObject.GetComponent<SubRoot>();
			control = GetComponent<SubControl>();
			stabilizer = GetComponent<Stabilizer>();

			subTurnHandlers = (ISubTurnHandler[])control.ReflectionGet("turnHandlers");
			subThrottleHandlers = (ISubThrottleHandler[])control.ReflectionGet("throttleHandlers");

			base.Awake();
		}

		protected override void Update() {
			base.Update();

			if (activePlayer != null) {
				activePlayer.animator.main.SetFloat("cyclops_yaw", smoothYaw.value);
				activePlayer.animator.main.SetFloat("cyclops_pitch", smoothPitch.value);

				smoothTransform.SetEnabled();
			} else {
				smoothTransform.SetDisabled();
			}
		}

		public override void SetSteeringWheel(float yaw, float pitch) {
			base.SetSteeringWheel(yaw, pitch);

			ShipSide useShipSide = yaw > 0 ? ShipSide.Port : ShipSide.Starboard;
			yaw = Mathf.Abs(yaw);
			if (yaw > .1f && yaw >= previousAbsYaw) {
				if (subTurnHandlers != null) {
					subTurnHandlers.ForEach(turnHandler => turnHandler.OnSubTurn(useShipSide));
				}
			}

			previousAbsYaw = yaw;
		}

		public override void SetAnimation(ClientVehicleMovement msg) {
			velocity = msg.velocity;
			angularVelocity = msg.angularVelocity;

			if (msg.throttle) {
				if (subThrottleHandlers != null) {
					foreach (var handler in subThrottleHandlers) {
						handler.OnSubAppliedThrottle();
					}
				}
			}
		}

		protected override Animator GetAnimator() {
			return control.mainAnimator;
		}
	}
}
