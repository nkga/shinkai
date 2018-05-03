using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using LiteNetLib;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;
using ShinkaiClient.Mono;
using ShinkaiClient.Unity;

namespace ShinkaiClient.Logic {
	static public class Building {
		private static RateLimiter rateLimiter = new RateLimiter();

		public static Base buildBase = null;
		public static Base.Face buildBaseFace = new Base.Face();
		public static Int3 buildBaseAnchor = Int3.zero;
		public static int buildDirection = 0;
		public static bool buildPlaceActive = false;
		public static bool buildConstructActive = false;

		private static GameObject lastBaseObject;
		private static GameObject lastConstructableObject;
		private static GameObject lastSpawnedModule;

		static public void Place(GameObject gameObject, GameObject target, BaseGhost baseGhost, TechType tech, bool furniture) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientBuildConstruct();
			res.tech = tech;
			res.furniture = furniture;
			res.objectGuid = GuidHelper.Get(gameObject);
			res.targetGuid = GuidHelper.Get(target);
			res.subGuid = GuidHelper.Get(Player.main.GetCurrentSub()?.gameObject);
			res.cameraPosition = MainCamera.camera.transform.position;
			res.cameraRotation = MainCamera.camera.transform.rotation;
			res.additiveRotation = Builder.additiveRotation;

			if (furniture == false) {
				res.objectPosition = gameObject.transform.position;
				res.objectRotation = gameObject.transform.rotation;
			} else {
				res.objectPosition = gameObject.transform.localPosition;
				res.objectRotation = gameObject.transform.localRotation;
			}

			if (baseGhost != null) {
				Log.Info("Base ghost: type:{0}, target:{1}", 
					baseGhost.GetType().FullName, 
					baseGhost.targetOffset);

				res.ghostGuid = GuidHelper.Get(baseGhost.gameObject);

				var corridorGhost = baseGhost as BaseAddCorridorGhost;
				if (corridorGhost != null) {
					res.direction = (int)corridorGhost.ReflectionGet("rotation");
				}

				var addModuleGhost = baseGhost as BaseAddModuleGhost;
				if (addModuleGhost != null) {
					res.direction = (int)(Base.Direction)addModuleGhost.ReflectionGet("direction");
				}

				var addMapRoom = baseGhost as BaseAddMapRoomGhost;
				if (addMapRoom != null) {
					res.direction = (int)(Base.CellType)addMapRoom.ReflectionGet("cellType");
				}

				var addFaceGhost = baseGhost as BaseAddFaceGhost;
				if (addFaceGhost != null) {
					res.anchor = addFaceGhost.TargetBase.GetAnchor();
					res.face = addFaceGhost.anchoredFace.GetValueOrDefault();
					res.hasFace = addFaceGhost.anchoredFace.HasValue;
				}
			}

			// Log.Info("Sending ClientBuildConstruct: {0}, {1}, {2}, {3}", res.tech, res.objectGuid, res.targetGuid, res.objectPosition);

			Multiplayer.main.Send(res);

			// ShinkaiPatcher.Unity.SceneDumper.DumpScene();
		}

		static public void PlaceBase(ConstructableBase component, BaseGhost baseGhost, TechType tech) {
			Place(component.gameObject, baseGhost?.TargetBase?.gameObject, baseGhost, tech, false);
		}

		static public void PlaceFurniture(GameObject component, GameObject target, TechType tech) {
			target = target?.GetComponentInParent<SubRoot>()?.gameObject;
			Place(component, target, null, tech, true);
		}

		static public void ChangeConstructionAmount(Constructable target, bool constructing) {
			if (Multiplayer.main.blocked) {
				return;
			}

			if (buildConstructActive) {
				return;
			}

			var res = new ClientBuildConstructChange();
			res.objectGuid = GuidHelper.Get(target.gameObject);
			res.baseGuid = GuidHelper.Get(GetNewBase());
			res.moduleGuid = GuidHelper.Get(GetNewModule());
			res.tech = CraftData.GetTechType(target.gameObject);
			res.objectPosition = target.gameObject.transform.position;
			res.amount = target.constructedAmount;
			res.state = target.constructed;
			res.constructing = constructing;

			if (res.amount == 0f && constructing) {
				return;
			}

			if (res.state || (res.amount == 0f && !constructing) || (res.amount == 1f && constructing) || rateLimiter.Update(0.500f)) {
				Multiplayer.main.Send(res);
			}
		}

		static public void DeconstructBase(BaseDeconstructable target, Base targetBase, GameObject constructable, TechType tech) {
			lastConstructableObject = constructable;

			if (Multiplayer.main.blocked) {
				return;
			}

			if (buildConstructActive) {
				return;
			}

			var res = new ClientBuildDeconstructBase();
			res.deconstructableGuid = GuidHelper.Get(target.gameObject);
			res.constructableGuid = GuidHelper.Get(GetNewConstructable());
			res.baseGuid = GuidHelper.Get(targetBase?.gameObject);
			res.tech = tech;
			res.position = target.transform.position;
			res.faceType = target.faceType;
			res.boundsMin = target.bounds.mins;
			res.boundsMax = target.bounds.maxs;

			var face = target.face.GetValueOrDefault();
			res.faceDirection = face.direction;
			res.faceCell = face.cell;

			Multiplayer.main.Send(res);
		}

		static public GameObject GetNewConstructable() {
			var result = lastConstructableObject;
			lastConstructableObject = null;
			return result;
		}


		static public GameObject GetNewBase() {
			var result = lastBaseObject;
			lastBaseObject = null;
			return result;
		}

		static public void SetNewBase(GameObject gameObject) {
			lastBaseObject = gameObject;
		}

		static public GameObject GetNewModule() {
			var result = lastSpawnedModule;
			lastSpawnedModule = null;
			return result;
		}

		static public void SetNewModule(GameObject gameObject) {
			lastSpawnedModule = gameObject;
		}
	}

	public static class Containers {
		public static void AddItem(Pickupable pickupable, GameObject owner) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientContainerAddItem();
			res.ownerGuid = GuidHelper.Get(owner);
			res.itemGuid = GuidHelper.Get(pickupable.gameObject);
			res.tech = pickupable.GetTechType();
			res.position = owner.transform.position;

			GuidHelper.GetChildGuids(pickupable.gameObject);
			res.data = ObjectSerializer.GetBytes(pickupable.gameObject, true);
			if (res.data == null) {
				return;
			}

			Multiplayer.main.Send(res);
		}

		public static void RemoveItem(Pickupable pickupable, GameObject owner) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientContainerRemoveItem();
			res.ownerGuid = GuidHelper.Get(owner);
			res.itemGuid = GuidHelper.Get(pickupable.gameObject);
			res.tech = pickupable.GetTechType();
			res.position = owner.transform.position;

			Multiplayer.main.Send(res);
		}
	}

	public static class Crafting {
		public static void FabricatorStart(Fabricator fabricator, TechType tech, float duration) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientFabricatorStart();
			res.fabricatorGuid = GuidHelper.Get(fabricator.gameObject);
			res.tech = tech;
			res.duration = duration;

			Multiplayer.main.Send(res);
		}

		public static void FabricatorPickup(GameObject gameObject, TechType tech) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientFabricatorPickup();
			res.fabricatorGuid = GuidHelper.Get(gameObject);
			res.tech = tech;

			Multiplayer.main.Send(res);
		}

		public static void ConstructorCraft(ConstructorInput input, TechType tech) {
			if (!CrafterLogic.ConsumeResources(tech)) {
				return;
			}

			uGUI.main.craftingMenu.Close(input);
			input.cinematicController.DisengageConstructor();

			Transform spawn = input.constructor.GetItemSpawnPoint(tech);
			var position = spawn.position;
			var rotation = spawn.rotation;

			GameObject gameObject;
			if (tech == TechType.Cyclops) {
				SubConsoleCommand.main.SpawnSub("cyclops", position, rotation);
				gameObject = SubConsoleCommand.main.GetLastCreatedSub();
			} else {
				gameObject = CraftData.InstantiateFromPrefab(tech, false);
				Transform component = gameObject.GetComponent<Transform>();
				component.position = position;
				component.rotation = rotation;
			}

			LargeWorldEntity.Register(gameObject);
			gameObject.SendMessage("StartConstruction", SendMessageOptions.DontRequireReceiver);
			CrafterLogic.NotifyCraftEnd(gameObject, CraftData.GetTechType(gameObject));
			Story.ItemGoalTracker.OnConstruct(tech);

			Commands.SendSpawn(gameObject, tech);
		}
	}

	public static class Equipment {
		public static void Equip(Pickupable pickupable, GameObject owner, string slot) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientEquipmentAddItem();
			res.ownerGuid = GuidHelper.Get(owner);
			res.itemGuid = GuidHelper.Get(pickupable.gameObject);
			res.slot = slot;
			res.position = owner.transform.position;
			res.data = ObjectSerializer.GetBytes(pickupable.gameObject, true);
			if (res.data == null) {
				return;
			}

			Multiplayer.main.Send(res);
		}

		public static void Unequip(Pickupable pickupable, GameObject owner, string slot) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientEquipmentRemoveItem();
			res.ownerGuid = GuidHelper.Get(owner);
			res.itemGuid = GuidHelper.Get(pickupable.gameObject);
			res.slot = slot;
			res.position = owner.transform.position;

			Multiplayer.main.Send(res);
		}
	}

	public static class Interior {
		public static void OpenableStateChanged(GameObject gameObject, bool openState, float duration) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientOpenableStateChanged();
			res.objectGuid = GuidHelper.Get(gameObject);
			res.state = openState;
			res.duration = duration;

			Multiplayer.main.Send(res);
		}
	}

	public static class Item {
		public static void Dropped(GameObject gameObject, TechType tech, Vector3 position) {
			var res = new ClientItemDropped();
			res.itemGuid = GuidHelper.Get(gameObject);
			res.waterParkGuid = GuidHelper.Get(Player.main?.currentWaterPark?.gameObject);
			res.tech = tech;
			res.position = position;
			res.data = ObjectSerializer.GetBytes(gameObject);
			if (res.data == null) {
				return;
			}

			Multiplayer.main.Send(res);
			SyncedObject.ApplyTo(gameObject);
		}

		public static void Grabbed(GameObject gameObject, TechType tech, Vector3 position) {
			var res = new ClientItemGrabbed();
			res.itemGuid = GuidHelper.Get(gameObject);
			res.tech = tech;
			res.position = position;
			Multiplayer.main.Send(res);
		}
	}

	public static class Movement {
		public const float updateRate = 0.04f;

		private static RateLimiter rateLimiter = new RateLimiter();

		public static void Update() {
			var player = Player.main;
			if (player == null) {
				return;
			}

			if (rateLimiter.Update(updateRate)) {
				var vehicle = player.GetVehicle();
				var sub = player.GetCurrentSub();
				var chair = player.GetPilotingChair();

				if (vehicle != null) {
					UpdateVehicle(vehicle);
				} else if (sub != null && chair != null) {
					UpdateSub(sub);
				} else {
					UpdatePlayer(player);
				}
			}
		}

		private static void UpdatePlayer(Player player) {
			var res = new ClientPlayerMovement();
			res.timestamp = DayNightCycle.main.timePassedAsDouble;
			res.id = Multiplayer.main.self.id;
			res.position = player.transform.position;
			res.velocity = player.playerController.velocity;
			res.lookRotation = Player.main.camRoot.GetAimingTransform().rotation;

			var subroot = Player.main.currentSub;
			if (subroot != null) {
				res.subGuid = GuidHelper.Get(subroot.gameObject);
				res.subPosition = player.transform.position - subroot.transform.position;
			}

			if (MainCameraControl.main != null) {
				res.bodyRotation = MainCameraControl.main.viewModel.transform.rotation;
			}

			var tool = Inventory.main.GetHeldTool();
			if (tool != null) {
				res.handTool = tool.pickupable.GetTechType();
				res.usingTool = tool.isInUse;
				res.handGuid = GuidHelper.Get(tool.gameObject);
			} else {
				res.handTool = TechType.None;
			}

			res.mode = player.GetMode();
			res.motorMode = player.motorMode;
			res.underwater = player.IsUnderwater();
			res.falling = player.GetPlayFallingAnimation();
			res.usingPda = player.GetPDA().isInUse;

			Multiplayer.main.Send(res, DeliveryMethod.Sequenced);
		}

		private static void UpdateVehicle(Vehicle vehicle) {
			if (vehicle.docked) {
				return;
			}

			var gameObject = vehicle.gameObject;

			var res = new ClientVehicleMovement();
			res.timestamp = DayNightCycle.main.timePassedAsDouble;
			res.id = Multiplayer.main.self.id;
			res.vehicleGuid = GuidHelper.Get(gameObject);
			res.vehicleTech = CraftData.GetTechType(gameObject);
			res.position = gameObject.transform.position;
			res.rotation = gameObject.transform.rotation;

			var rigidbody = vehicle.gameObject.GetComponent<Rigidbody>();
			if (rigidbody != null) {
				res.velocity = rigidbody.velocity;
				res.angularVelocity = rigidbody.angularVelocity;
			}

			// Exosuit and Seamoth both have steering components.
			res.steeringWheelYaw = (float)vehicle.ReflectionGet<Vehicle, Vehicle>("steeringWheelYaw");
			res.steeringWheelPitch = (float)vehicle.ReflectionGet<Vehicle, Vehicle>("steeringWheelPitch");

			// Determine whether the vehicle movement is active or not.
			if (vehicle && AvatarInputHandler.main.IsEnabled()) {
				if (res.vehicleTech == TechType.Seamoth) {
					bool flag = vehicle.transform.position.y < Ocean.main.GetOceanLevel() && vehicle.transform.position.y < vehicle.worldForces.waterDepth && !vehicle.precursorOutOfWater;
					res.throttle = flag && GameInput.GetMoveDirection().sqrMagnitude > .1f;
				} else if (res.vehicleTech == TechType.Exosuit) {
					Exosuit exosuit = vehicle as Exosuit;
					if (exosuit) {
						res.throttle = (bool)exosuit.ReflectionGet("_jetsActive");
						res.thrust = exosuit.mainAnimator.GetFloat("thrustIntensity");
						res.grounded = exosuit.mainAnimator.GetBool("onGround");
						res.useLeft = exosuit.mainAnimator.GetBool("use_tool_left");
						res.useRight = exosuit.mainAnimator.GetBool("use_tool_right");
					}
				}
			}

			Multiplayer.main.Send(res, DeliveryMethod.Sequenced);
		}

		private static void UpdateSub(SubRoot sub) {
			var gameObject = sub.gameObject;

			var res = new ClientVehicleMovement();
			res.id = Multiplayer.main.self.id;
			res.vehicleGuid = GuidHelper.Get(gameObject);
			res.vehicleTech = TechType.Cyclops;
			res.position = gameObject.transform.position;
			res.rotation = gameObject.transform.rotation;

			var rigidbody = sub.GetComponent<Rigidbody>();
			if (rigidbody != null) {
				res.velocity = rigidbody.velocity;
				res.angularVelocity = rigidbody.angularVelocity;
			}

			var subControl = sub.GetComponent<SubControl>();
			if (subControl != null) {
				res.steeringWheelYaw = (float)subControl.ReflectionGet("steeringWheelYaw");
				res.steeringWheelPitch = (float)subControl.ReflectionGet("steeringWheelPitch");
				res.throttle = subControl.appliedThrottle && (bool)subControl.ReflectionGet("canAccel");
			}

			Multiplayer.main.Send(res, DeliveryMethod.Sequenced);
		}
	}

	public static class Scanning {
		public static void AddProgress(TechType tech, int progress) {
			if (tech == TechType.None || Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientScanProgress();
			res.tech = tech;
			res.progress = progress;
			Multiplayer.main.Send(res);
		}

		public static void AddKnownTech(TechType tech) {
			if (tech == TechType.None || Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientScanKnownTech();
			res.tech = tech;
			Multiplayer.main.Send(res);
		}

		public static void AddEncyclopedia(string key) {
			if (string.IsNullOrEmpty(key) || Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientScanEncyclopedia();
			res.key = key;
			Multiplayer.main.Send(res);
		}
	}

	public static class VehicleLogic {
		public static void SendDocked(VehicleDockingBay bay, Vehicle vehicle, bool status) {
			if (bay == null || vehicle == null || Multiplayer.main.blocked) {
				return;
			}

			var sync = vehicle.GetComponent<SyncedVehicle>();
			if (sync != null && sync.activePlayer != null) {
				return;
			}

			var res = new ClientVehicleDocking();
			res.id = Multiplayer.main.self.id;
			res.vehicleGuid = GuidHelper.Get(vehicle.gameObject);
			res.bayGuid = GuidHelper.Get(bay.gameObject);
			res.subGuid = GuidHelper.Get(bay.subRoot?.gameObject);
			res.bayPosition = bay.transform.position;
			res.docked = status;

			Multiplayer.main.Send(res);
		}
	}

	public static class Vitals {
		public const float updateRate = 1.0f;
		private static RateLimiter rateLimiter = new RateLimiter();

		public static void Update() {
			if (rateLimiter.Update(updateRate) == false) {
				return;
			}

			var player = Player.main;
			if (player == null) {
				return;
			}

			var res = new ClientPlayerVitals();
			res.oxygen = player.GetOxygenAvailable();

			if (player.liveMixin != null) {
				res.health = player.liveMixin.health;
			}

			var survival = player.GetComponent<Survival>();
			if (survival != null) {
				res.food = survival.food;
				res.water = survival.water;
			}

			Multiplayer.main.Send(res, DeliveryMethod.Sequenced);
		}
	}

	public static class Commands {
		private static string setGuid;
		private static ChildGuid[] setChildren;
		private static Vector3 setPosition;
		private static Quaternion setRotation;

		public static void SendVehicleKill(GameObject gameObject) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientVehicleKill();
			res.vehicleGuid = GuidHelper.Get(gameObject);
			Multiplayer.main.Send(res);
		}

		public static void SendLiveMixinChange(LiveMixin mixin, float amount, Vector3 position = default(Vector3), DamageType type = DamageType.Normal) {
			if (amount == 0f) {
				return;
			}

			if (Multiplayer.main.blocked) {
				return;
			}

			if (mixin.health < 0f) {
				return;
			}

			if (mixin.invincible) {
				return;
			}

			if (mixin.health == mixin.maxHealth && amount > 0f) {
				return;
			}

			if (GameModeUtils.IsInvisible() && mixin.invincibleInCreative) {
				return;
			}
			
			if (mixin.gameObject.GetComponent<Creature>() != null) {
				return;
			}

			var uid = mixin.gameObject.GetComponent<UniqueIdentifier>();
			if (uid == null) {
				return;
			}

			var sync = mixin.gameObject.GetComponent<SyncedVehicle>();
			if (sync != null && sync.activePlayer != null) {
				return;
			}

			var res = new ClientLiveMixinChange();
			res.targetGuid = uid.Id;
			res.health = mixin.health;
			res.amount = amount;
			res.type = type;
			res.position = position;
			res.force = false;

			Multiplayer.main.Send(res);
		}

		public static void SendStoryGoal(string key, Story.GoalType goal) {
			var res = new ClientStoryGoal();
			res.timestamp = DayNightCycle.main.timePassedAsDouble;
			res.key = key;
			res.goal = goal;
			Multiplayer.main.Send(res);
		}

		public static void SendChat(string text) {
			if (Multiplayer.main.blocked == false) {
				var res = new ClientCommandChat();
				res.id = Multiplayer.main.self.id;
				res.text = text;
				Multiplayer.main.Send(res);
			}
		}

		public static void SendSpawn(GameObject gameObject, TechType tech) {
			if (gameObject == null || tech == TechType.None || Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientCommandSpawn();
			res.objectGuid = GuidHelper.Get(gameObject);
			res.tech = tech;
			res.spawnPosition = gameObject.transform.position;
			res.spawnRotation = gameObject.transform.rotation;
			res.children = GuidHelper.GetChildGuids(gameObject);

			Multiplayer.main.Send(res);
		}

		public static void SendCyclopsHorn(SubRoot cyclops) {
			if (cyclops == null || Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientCyclopsHorn();
			res.vehicleGuid = GuidHelper.Get(cyclops.gameObject);
			Multiplayer.main.Send(res);
		}

		public static void SendCyclopsState(SubRoot cyclops) {
			if (cyclops == null || Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientCyclopsState();
			res.vehicleGuid = GuidHelper.Get(cyclops.gameObject);

			var shield = cyclops.GetComponentInChildren<CyclopsShieldButton>();
			if (shield != null) {
				res.shield = (bool)shield.ReflectionGet("active");
			}

			var engineState = cyclops.GetComponentInChildren<CyclopsEngineChangeState>();
			if (engineState != null) {
				res.engineOn = engineState.motorMode.engineOn;
				res.engineStarting = (bool)engineState.ReflectionGet("startEngine");
				res.motorMode = engineState.motorMode.cyclopsMotorMode;
			}

			var lighting = cyclops.GetComponentInChildren<CyclopsLightingPanel>();
			if (lighting != null) {
				res.floodLights = lighting.floodlightsOn;
				res.internalLights = lighting.lightingOn;
			}

			var silentRunning = cyclops.GetComponentInChildren<CyclopsSilentRunningAbilityButton>();
			if (silentRunning != null) {
				res.silentRunning = (bool)silentRunning.ReflectionGet("active");
			}

			Multiplayer.main.Send(res);
		}

		public static void SpawnPrefab(string objectGuid, ChildGuid[] children, TechType tech, Vector3 position, Quaternion rotation) {
			if (tech == TechType.None) {
				return;
			}

			if (tech == TechType.Cyclops) {
				SpawnSub(objectGuid, children, position, rotation);
				return;
			}

			var prefab = CraftData.GetPrefabForTechType(tech);
			if (prefab == null) {
				return;
			}

			var gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, position, rotation);
			gameObject.SetActive(true);

			GuidHelper.Set(gameObject, objectGuid);
			GuidHelper.SetChildGuids(gameObject, children);

			LargeWorldEntity.Register(gameObject);
			gameObject.SendMessage("StartConstruction", SendMessageOptions.DontRequireReceiver);
			CrafterLogic.NotifyCraftEnd(gameObject, tech);

			SyncedObject.ApplyTo(gameObject);
		}

		private static void SpawnSub(string objectGuid, ChildGuid[] children, Vector3 position, Quaternion rotation) {
			setGuid = objectGuid;
			setChildren = children;
			setPosition = position;
			setRotation = rotation;
			LightmappedPrefabs.main.RequestScenePrefab("cyclops", new LightmappedPrefabs.OnPrefabLoaded(OnSubLoaded));
		}

		private static void OnSubLoaded(GameObject prefab) {
			var gameObject = Utils.SpawnPrefabAt(prefab, null, setPosition);
			gameObject.transform.rotation = setRotation;
			gameObject.SetActive(true);

			LargeWorldEntity.Register(gameObject);
			gameObject.SendMessage("StartConstruction", SendMessageOptions.DontRequireReceiver);
			CrafterLogic.NotifyCraftEnd(gameObject, CraftData.GetTechType(gameObject));

			GuidHelper.Set(gameObject, setGuid);
			GuidHelper.SetChildGuids(gameObject, setChildren);
			
			setGuid = null;
			setChildren = null;

			SyncedObject.ApplyTo(gameObject);
		}
	}
}