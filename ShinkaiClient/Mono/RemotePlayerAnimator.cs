using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;
using ShinkaiClient.Unity;

namespace ShinkaiClient.Mono {
	public class RemotePlayerAnimator : MonoBehaviour {
		public bool updating;
		public Animator main;
		public RemoteInventory inventory;

		private Player.Mode mode;
		private Vector3 smoothVelocity;
		private Vector3 targetVelocity;
		private Quaternion lookRotation;
		private float smoothViewPitch;
		private bool underwater;

		private Transform handAttachPoint;
		public GameObject toolActive;

		private void Awake() {
			main = base.gameObject.GetComponent<Animator>();
			main.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
			
			/*
			foreach (var param in main.parameters) {
				if (param.type == AnimatorControllerParameterType.Bool) {
					Log.Info("anim: {0}, {1}, {2}", param.name, param.type, main.GetBool(param.name));
				} else if (param.type == AnimatorControllerParameterType.Float) {
					Log.Info("anim: {0}, {1}, {2}", param.name, param.type, main.GetFloat(param.name));
				} else if (param.type == AnimatorControllerParameterType.Int) {
					Log.Info("anim: {0}, {1}, {2}", param.name, param.type, main.GetInteger(param.name));
				} else if (param.type == AnimatorControllerParameterType.Trigger) {
					Log.Info("anim: {0}, {1}", param.name, param.type);
				}
			}
			*/

			handAttachPoint = gameObject.transform.FindInChildren("attach1"); 

			foreach (var component in handAttachPoint.GetComponentsInChildren<Pickupable>()) {
				component.gameObject.SetActive(false);
			}

			toolActive = null;
		}

		private void Update() {
			if (updating == false) {
				return;
			}

			Vector3 relativeVelocity = gameObject.transform.rotation.GetInverse() * targetVelocity;

			float d = underwater ? 4f : 8f;
			smoothVelocity = UWE.Utils.SlerpVector(smoothVelocity, relativeVelocity, Vector3.Normalize(relativeVelocity - smoothVelocity) * d * Time.deltaTime);

			main.SetFloat("move_speed", smoothVelocity.magnitude);
			main.SetFloat("move_speed_x", smoothVelocity.x);
			main.SetFloat("move_speed_y", smoothVelocity.y);
			main.SetFloat("move_speed_z", smoothVelocity.z);

			float viewPitch = lookRotation.eulerAngles.x;
			if (viewPitch > 180.0f) {
				viewPitch -= 360.0f;
			}
			viewPitch = -viewPitch;
			smoothViewPitch = Mathf.Lerp(smoothViewPitch, viewPitch, d * Time.deltaTime);
			main.SetFloat("view_pitch", smoothViewPitch);
		}

		public void Correct(ClientPlayerMovement msg) {
			underwater = msg.underwater;
			targetVelocity = msg.velocity;
			lookRotation = msg.lookRotation;
			mode = msg.mode;

			main.SetBool("is_underwater", msg.underwater && msg.motorMode != Player.MotorMode.Vehicle);
			main.SetBool("using_pda", msg.usingPda);
			main.SetBool("using_tool", msg.usingTool);
			main.SetBool("holding_tool", msg.handTool != TechType.None);
			main.SetBool("cyclops_steering", msg.mode == Player.Mode.Piloting);
			main.SetBool("jump", msg.falling);

			if (toolActive != null) {
				SetToolAnimation(toolActive, false);
				toolActive = null;
			}

			if (inventory != null) {
				toolActive = inventory.Get(msg.handGuid);
			}

			if (toolActive != null) {
				SetToolAnimation(toolActive, true);
			}
		}

		private void SetToolAnimation(GameObject gameObject, bool state) {
			if (gameObject == null) {
				return;
			}

			gameObject.SetActive(state);

			var playerTool = toolActive.GetComponent<PlayerTool>();
			if (playerTool != null) {
				if (string.IsNullOrEmpty(playerTool.animToolName) == false) {
					main.SetBool("holding_" + playerTool.animToolName, state);
				}
			}

			var pickupable = toolActive.GetComponent<Pickupable>();
			if (pickupable != null) {
				pickupable.Reparent(handAttachPoint);
			}
		}
	}
}
