using System;
using UnityEngine;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;
using ShinkaiClient.Unity;

namespace ShinkaiClient.Mono {
	public class RemotePlayer {
		public static GameObject baseObject;

		public readonly int id;
		public readonly string username;

		public Attacher attacher;
		public GameObject body;
		public GameObject playerView;
		public SmoothTransform smoothTransform;
		public RemoteInventory inventory;
		public RemotePlayerAnimator animator;

		public Vehicle activeVehicle;
		public SubRoot activeSub;
		public PilotingChair activeChair;

		public RemotePlayer(int id, string name, string inventoryGuid) {
			this.id = id;
			this.username = name;
			this.inventory = new RemoteInventory(inventoryGuid);

			Log.InGame(name + " joined the game.");

			Create();
		}

		public void Create() {
			var attachObject = new GameObject();
			attacher = attachObject.AddComponent<Attacher>();
			attacher.name = "Attacher Body (" + this.username + ")";

			body = UnityEngine.Object.Instantiate(baseObject);
			body.SetActive(true);

			body.transform.parent = attacher.transform;
			smoothTransform = body.EnsureComponent<SmoothTransform>();

			playerView = body.transform.Find("player_view").gameObject;
			animator = playerView.EnsureComponent<RemotePlayerAnimator>();
			animator.inventory = inventory;

			animator.main.ResetParameters();
			animator.main.SetBool("diving", false);
			animator.main.SetBool("diving_land", false);
			animator.main.SetBool("grab", false);
			animator.main.SetBool("bash", false);
			animator.main.SetBool("bleeder", false);
			animator.main.SetBool("jump", false);
			animator.main.SetBool("is_underwater", false);
			animator.main.SetBool("in_seamoth", false);
			animator.main.SetBool("in_exosuit", false);
			animator.main.SetBool("using_mechsuit", false);
			animator.main.SetBool("cinematics_enabled", false);
			animator.main.SetBool("on_surface", false);
			animator.main.SetFloat("verticalOffset", 0f);

			var signalBase = UnityEngine.Object.Instantiate(Resources.Load("VFX/xSignal")) as GameObject;
			signalBase.name = "signal" + username;
			signalBase.transform.localScale = new Vector3(.5f, .5f, .5f);
			signalBase.transform.localPosition += new Vector3(0, 0.8f, 0);
			signalBase.transform.SetParent(playerView.transform, false);

			var ping = signalBase.GetComponent<PingInstance>();
			ping.SetLabel(username);
			ping.pingType = PingType.Signal;
			ping.maxDist = 0.25f;
			ping.minDist = 2f;
			ping.visible = true;
		}

		public static void Initialize() {
			var realBody = GameObject.Find("body");
			var realHead = realBody.GetComponentInParent<Player>().head;

			realHead.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
			var body = UnityEngine.Object.Instantiate(realBody);
			realHead.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

			body.name = "Remote Body Base";
			body.transform.parent = null;

			SetModelRender(body, "diveSuit_head_geo", false);
			SetModelRender(body, "scubaSuit", true);

			body.SetActive(false);

			baseObject = body;
		}

		public void Destroy() {
			SetVehicle(null);
			SetPilotingChair(null);
			SetSubRoot(null);
			UnityEngine.Object.Destroy(body);

			inventory.Reset();
		}

		public void Attach(Transform transform, bool keepWorldTransform = false) {
			attacher.target = transform;

			if (!keepWorldTransform) {
				UWE.Utils.ZeroTransform(body);
			}
		}

		public void Detach() {
			attacher.target = null;
		}

		public void UpdatePlayerMovement(ClientPlayerMovement msg) {
			if (body == null) {
				Create();
			}

			body.SetActive(true);

			var arms = (ArmsController)playerView.GetComponent(typeof(ArmsController));
			if (arms != null) {
				arms.SetWorldIKTarget(null, null);
				UnityEngine.GameObject.Destroy(arms);
			}

			animator.updating = true;
			smoothTransform.SetEnabled();

			var subroot = GuidHelper.FindComponent<SubRoot>(msg.subGuid);
			var newPosition = (subroot != null) ? (msg.subPosition + subroot.transform.position) : msg.position;

			SetSubRoot(subroot);
			SetPilotingChair(null);
			SetVehicle(null);

			smoothTransform.Correct(newPosition, msg.bodyRotation, msg.velocity, msg.timestamp);
			animator.Correct(msg);
		}

		public void UpdateVehicleMovement(Vehicle vehicle, SubRoot subroot) {
			animator.updating = false;
			if (body == null) {
				Create();
			}

			body.SetActive(true);
			smoothTransform.SetDisabled();

			SetSubRoot(subroot);
			SetPilotingChair(subroot?.GetComponentInChildren<PilotingChair>());
			SetVehicle(vehicle);
		}

		public void SetPilotingChair(PilotingChair newPilotingChair) {
			if (activeSub == null) {
				return;
			}

			if (activeChair == newPilotingChair) {
				return;
			}

			activeChair = newPilotingChair;
			var syncedCyclops = activeSub.GetComponent<SyncedCyclops>();

			if (activeChair != null) {
				Attach(activeChair.sittingPosition.transform);
				syncedCyclops.Enter(this);
				// arms.SetWorldIKTarget(activeChair.leftHandPlug, activeChair.rightHandPlug);
			} else {
				SetSubRoot(activeSub);
				syncedCyclops.Exit();
				// arms.SetWorldIKTarget(null, null);
			}

			var steering = (newPilotingChair != null);
			animator.main.SetBool("cyclops_steering", steering);
			// rigidbody.isKinematic = steering;
		}

		public void SetSubRoot(SubRoot newSubRoot) {
			activeSub = newSubRoot;
		}

		public void SetVehicle(Vehicle newVehicle) {
			if (activeVehicle != newVehicle) {
				if (activeVehicle != null) {
					activeVehicle.mainAnimator.SetBool("player_in", false);
					activeVehicle.GetComponent<SyncedVehicle>().Exit();
					// arms.SetWorldIKTarget(null, null);
				}

				bool in_exosuit = false;
				bool in_seamoth = false;

				if (newVehicle != null) {
					newVehicle.mainAnimator.SetBool("player_in", true);

					Attach(newVehicle.playerPosition.transform);
					newVehicle.GetComponent<SyncedVehicle>().Enter(this);
					// arms.SetWorldIKTarget(newVehicle.leftHandPlug, newVehicle.rightHandPlug);

					in_exosuit = newVehicle is Exosuit;
					in_seamoth = newVehicle is SeaMoth;
				}

				animator.main.SetBool("is_underwater", false);
				animator.main.SetBool("in_seamoth", in_seamoth);
				animator.main.SetBool("in_exosuit", in_exosuit);
				animator.main.SetBool("using_mechsuit", in_exosuit);

				activeVehicle = newVehicle;
			}
		}

		private static void SetModelRender(GameObject target, string name, bool state) {
			var suit = target.transform.FindInChildren(name);
			if (suit != null) {
				if (state) {
					suit.gameObject.SetActive(true);
				}

				foreach (var skm in suit.GetComponentsInChildren<SkinnedMeshRenderer>()) {
					skm.shadowCastingMode = state ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
					skm.enabled = state;
				}

				if (state == false) {
					suit.gameObject.SetActive(false);
				}
			}
		}
	}
}
