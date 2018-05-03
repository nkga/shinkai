using System.Reflection;
using UnityEngine;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;

namespace ShinkaiClient.Mono {
	class SyncedExosuit : SyncedVehicle {
		static FieldInfo leftArmField = typeof(Exosuit).GetField("leftArm", BindingFlags.NonPublic | BindingFlags.Instance);
		static FieldInfo rightArmField = typeof(Exosuit).GetField("rightArm", BindingFlags.NonPublic | BindingFlags.Instance);

		private bool lastThrottle = false;
		private bool lastLeft = false;
		private bool lastRight = false;

		private float timeJetsChanged;
		public Exosuit exosuit;
		public Rigidbody rigidbody;
		public Vector3 smoothVelocity;
		public Vector3 targetVelocity;

		protected override void Awake() {
			exosuit = GetComponent<Exosuit>();
			rigidbody = GetComponent<Rigidbody>();

			base.Awake();
		}

		protected override void Update() {
			base.Update();

			if (activePlayer != null) {
				var animator = GetAnimator();
				if (animator != null) {
					Vector3 velocity = targetVelocity;
					Vector3 vector = base.transform.InverseTransformVector(velocity);

					smoothVelocity = UWE.Utils.SlerpVector(smoothVelocity, vector, Vector3.Normalize(vector - smoothVelocity) * 4f * Time.deltaTime);

					animator.SetFloat("move_speed_x", smoothVelocity.x);
					animator.SetFloat("move_speed_y", smoothVelocity.y);
					animator.SetFloat("move_speed_z", smoothVelocity.z);
				}
			}
		}

		public override void Enter(RemotePlayer player) {
			rigidbody.freezeRotation = false;
			exosuit.ReflectionCall("SetIKEnabled", false, false, new object[] { true });
			exosuit.ReflectionSet("thrustIntensity", 0f);
			exosuit.ambienceSound.Play();
			base.Enter(player);
		}

		public override void Exit() {
			targetVelocity = Vector3.zero;

			rigidbody.freezeRotation = true;

			exosuit.ReflectionCall("SetIKEnabled", false, false, new object[] { false });
			exosuit.loopingJetSound.Stop();
			exosuit.fxcontrol.Stop(0);
			exosuit.ambienceSound.Stop();

			SetLeftArm(false);
			SetRightArm(false);

			base.Exit();
		}

		public override void SetAnimation(ClientVehicleMovement msg) {
			targetVelocity = msg.velocity;

			if (timeJetsChanged + 3f <= Time.time && lastThrottle != msg.throttle) {
				timeJetsChanged = Time.time;
				lastThrottle = msg.throttle;
				if (msg.throttle) {
					exosuit.loopingJetSound.Play();
					exosuit.fxcontrol.Play(0);
				} else {
					exosuit.loopingJetSound.Stop();
					exosuit.fxcontrol.Stop(0);
				}

				rigidbody.velocity = msg.velocity;
			}

			var animator = GetAnimator();
			if (animator != null) {
				animator.SetBool("onGround", msg.grounded);
				animator.SetFloat("thrustIntensity", msg.thrust);
			}

			SetLeftArm(msg.useLeft);
			SetRightArm(msg.useRight);
		}

		protected void SetLeftArm(bool state) {
			var leftArm = (IExosuitArm)leftArmField.GetValue(exosuit);
			if (leftArm != null) {
				float num;

				if (state) {
					lastLeft = true;
					leftArm.OnUseDown(out num);
				} else {
					if (lastLeft) {
						lastLeft = false;
						leftArm.OnUseUp(out num);
					}
				}
			}

			if (exosuit.mainAnimator != null) {
				exosuit.mainAnimator.SetBool("use_tool_left", state);
			}

			if (activePlayer != null) {
				activePlayer.animator.main.SetBool("exosuit_use_left", state);
			}
		}

		protected void SetRightArm(bool state) {
			var rightArm = (IExosuitArm)rightArmField.GetValue(exosuit);
			if (rightArm != null) {
				float num;

				if (state) {
					lastRight = true;
					rightArm.OnUseDown(out num);
				} else {
					if (lastRight) {
						lastRight = false;
						rightArm.OnUseUp(out num);
					}
				}
			}

			if (exosuit.mainAnimator != null) {
				exosuit.mainAnimator.SetBool("use_tool_right", state);
			}

			if (activePlayer != null) {
				activePlayer.animator.main.SetBool("exosuit_use_right", state);
			}
		}

		protected override Animator GetAnimator() {
			return exosuit.mainAnimator;
		}
	}
}
