using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UWE;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;
using ShinkaiClient.Overrides;
using ShinkaiClient.Unity;

namespace ShinkaiClient.Mono {
	public class Multiplayer : MonoBehaviour, INetEventListener {
		public class MessageBlocker : IDisposable {
			public MessageBlocker() {
				Multiplayer.main.blocked = true;
			}

			public void Dispose() {
				Multiplayer.main.blocked = false;
			}
		}

		public struct PlayerJoinInfo {
			public int id;
			public int seed;
			public string username;
			public string password;
			public string hostname;
			public bool loaded;

			public void Reset() {
				id = 0;
				seed = 0;
				username = "Player";
				password = "";
				loaded = false;
			}
		}

		static public Multiplayer main;

		private Processor processor;
		private Serializer serializer;
		private NetManager manager;
		private NetPeer host;
		public bool blocked;

		public PlayerJoinInfo self;
		public Dictionary<int, RemotePlayer> remotePlayers;
		public PlayerChat playerChat;
		public PlayerChatInput playerChatInput;

		private const float objectSearchDistance = 0.1f;

		public void Awake() {
			main = this;
			DontDestroyOnLoad(this);

			serializer = new Serializer();
			MessageExtensions.Setup(serializer);
			processor = new Processor(serializer);

			self = new PlayerJoinInfo();
			remotePlayers = new Dictionary<int, RemotePlayer>();

			AddHostHandler<ServerJoinInfo>(Process);
			AddHostHandler<ServerJoinReject>(Process);
			AddHostHandler<ServerTimeUpdate>(Process);
			AddHostHandler<ServerSyncRequest>(Process);
			AddHostHandler<ServerSyncFinish>(Process);
			AddHostHandler<ClientPlayerJoin>(Process);
			AddHostHandler<ClientPlayerLeave>(Process);
			AddHostHandler<ClientPlayerDeath>(Process);
			AddHostHandler<ClientPlayerMovement>(Process);
			AddHostHandler<ClientPlayerVitals>(Process);
			AddHostHandler<ClientVehicleMovement>(Process);
			AddHostHandler<ClientVehicleKill>(Process);
			AddHostHandler<ClientVehicleColorChange>(Process);
			AddHostHandler<ClientVehicleNameChange>(Process);
			AddHostHandler<ClientCreatureUpdate>(Process);
			AddHostHandler<ClientLiveMixinChange>(Process);
			AddHostHandler<ClientBuildConstruct>(Process);
			AddHostHandler<ClientBuildConstructChange>(Process);
			AddHostHandler<ClientBuildDeconstructBase>(Process);
			AddHostHandler<ClientItemDropped>(Process);
			AddHostHandler<ClientItemGrabbed>(Process);
			AddHostHandler<ClientFabricatorStart>(Process);
			AddHostHandler<ClientFabricatorPickup>(Process);
			AddHostHandler<ClientOpenableStateChanged>(Process);
			AddHostHandler<ClientObjectUpdate>(Process);
			AddHostHandler<ClientEquipmentAddItem>(Process);
			AddHostHandler<ClientEquipmentRemoveItem>(Process);
			AddHostHandler<ClientContainerAddItem>(Process);
			AddHostHandler<ClientContainerRemoveItem>(Process);
			AddHostHandler<ClientScanProgress>(Process);
			AddHostHandler<ClientScanEncyclopedia>(Process);
			AddHostHandler<ClientStoryGoal>(Process);
			AddHostHandler<ClientScanKnownTech>(Process);
			AddHostHandler<ClientVehicleDocking>(Process);
			AddHostHandler<ClientConstructorCraft>(Process);
			AddHostHandler<ClientCommandSpawn>(Process);
			AddHostHandler<ClientCommandChat>(Process);
			AddHostHandler<ClientToggleLight>(Process);
			AddHostHandler<ClientPowerChange>(Process);
			AddHostHandler<ClientCyclopsHorn>(Process);
			AddHostHandler<ClientCyclopsState>(Process);
			AddHostHandler<ClientItemLabel>(Process);

			foreach (var type in typeof(Message).Assembly.GetTypes()) {
				if (typeof(Message).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass) {
					if (processor.Has(type) == false) {
						Log.Warn("Unhandled message type: " + type.FullName);
					}
				}
			}

			playerChat = gameObject.EnsureComponent<PlayerChat>();
			playerChatInput = gameObject.EnsureComponent<PlayerChatInput>();
			playerChatInput.SetManager(playerChat);

			typeof(DevConsole).GetField("keyCodes", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, new KeyCode[] { KeyCode.BackQuote });
		}

		public void OnDestroy() {
			Disconnect();
		}

		public void Update() {
			if (manager != null) {
				manager.PollEvents();

				if (manager.IsRunning && this.self.loaded) {
					Logic.Movement.Update();
					Logic.Vitals.Update();

					UpdateChat();
				}
			}
		}

		public bool IsRunning() {
			return (host != null);
		}

		public void Connect(string username, string password, string hostname) {
			if (manager != null) {
				Disconnect();
			}

			manager = new NetManager(this, Config.maxConnections);
			manager.PingInterval = Config.pingInterval;
			manager.DisconnectTimeout = Config.disconnectTimeout;
			manager.MergeEnabled = true;
			// manager.SimulateLatency = true;
			// manager.SimulatePacketLoss = true;

			if (manager.Start() == false) {
				Log.InGame("Error starting network interface.");
				Disconnect();
				return;
			}

			blocked = true;

			self.Reset();
			self.username = username;
			self.password = password;
			self.hostname = hostname;

			host = manager.Connect(hostname, Config.connectPort, Config.connectKey);
			if (host == null) {
				Log.InGame("Error connecting to host: " + hostname);
				Disconnect();
				return;
			}
		}

		public void Disconnect() {
			if (manager == null) {
				return;
			}

			Log.Warn("Disconnecting from host.");

			foreach (var item in remotePlayers) {
				var player = item.Value;
				player.Destroy();
			}
			remotePlayers.Clear();

			blocked = true;
			host = null;
			self.Reset();

			manager.DisconnectAll();
			manager.Stop();
			manager = null;
		}

		public void Send(Message message, DeliveryMethod method = DeliveryMethod.ReliableOrdered) {
			if (host != null) {
				LogMessage("Send", message);

				processor.Serialize(message);
				processor.Send(host, method);
			}
		}

		private void AddHostHandler<T>(Action<T> onReceive) where T : Message, new() {
			processor.Add(typeof(T), (peer, reader) => {
				var inst = new T();
				if (serializer.Deserialize(reader, inst)) {
					try {
						LogMessage("Recv", inst);
						onReceive(inst);
					} catch (Exception exception) {
						UnityEngine.Debug.LogException(exception);
					}
				}
			});
		}

		public void OnNetworkError(NetEndPoint endpoint, int socketErrorCode) {
			Log.InGame("Socket error: " + socketErrorCode);
		}

		public void OnNetworkReceive(NetPeer peer, NetDataReader reader, DeliveryMethod channel) {
			if (peer == host) {
				try {
					processor.ReadAll(peer, reader);
				} catch (Exception ex) {
					Log.Warn("Error occured while processing peer: {0}, {1}", peer, ex.ToString());
				}
			}
		}

		public void OnNetworkReceiveUnconnected(NetEndPoint endpoint, NetDataReader reader, UnconnectedMessageType type) {
		}

		public void OnNetworkLatencyUpdate(NetPeer peer, int latency) {
		}

		public void OnConnectionRequest(ConnectionRequest request) {
			request.Accept();
		}

		public virtual void OnPeerConnected(NetPeer peer) {
			Log.Debug("Peer connected: " + peer.EndPoint);

			if (peer == host) {
				Send(new ServerJoinRequest {
					username = self.username,
					password = self.password
				});
			}
		}

		public virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
			Log.InGame("Disconnected from server: " + peer.EndPoint + ", " + disconnectInfo.Reason);
		}

		private void Process(ServerJoinInfo msg) {
			Log.InGame("Joining server.");

			self.id = msg.id;
			self.seed = msg.seed;
			Main.Patch();

			StartCoroutine(LaunchGameAsync(msg));
		}

		private void Process(ServerJoinReject msg) {
			Log.InGame("Server refused connection: " + msg.reason);
		}
		
		private void Process(ServerTimeUpdate msg) {
			UpdateServerTime(msg.timestamp);
		}

		private void Process(ServerSyncRequest msg) {
			using (new MessageBlocker()) {
				GameReset();
			}
		}

		private void Process(ServerSyncFinish msg) {
			StartCoroutine(SyncFinishAsync());
		}

		private IEnumerator SyncFinishAsync() {
			var lws = LargeWorldStreamer.main;
			while (!lws.IsWorldSettled()) {
				yield return CoroutineUtils.waitForNextFrame;
			}

			var root = lws.globalRoot;
			if (root != null) {
				foreach (Transform child in root.transform) {
					var target = child.GetComponent<Base>();
					if (target != null) {
						target.RebuildGeometry();
					}
				}

				// SkyEnvironmentChanged.Broadcast(root, Player.main.GetSkyEnvironment());
			}

			yield break;
		}

		private void Process(ClientPlayerJoin msg) {
			if (msg.id != self.id) {
				if (remotePlayers.ContainsKey(msg.id) == false) {
					remotePlayers.Add(msg.id, new RemotePlayer(msg.id, msg.username, msg.inventoryGuid));
				}
			}
		}

		private void Process(ClientPlayerLeave msg) {
			RemotePlayer player;
			if (remotePlayers.TryGetValue(msg.id, out player)) {
				Log.InGame(player.username + " left the game.");
				player.Destroy();
				remotePlayers.Remove(msg.id);
			}
		}

		private void Process(ClientPlayerDeath msg) {
			RemotePlayer remotePlayer;
			if (remotePlayers.TryGetValue(msg.id, out remotePlayer)) {
				playerChat.WriteMessage(remotePlayer.username + " has died.");
			}
		}

		private void Process(ClientPlayerMovement msg) {
			RemotePlayer remotePlayer;
			if (remotePlayers.TryGetValue(msg.id, out remotePlayer)) {
				remotePlayer.UpdatePlayerMovement(msg);
			} else if (msg.id == self.id) {
				var player = Player.main;
				player.SetPosition(msg.position, msg.lookRotation);
				player.OnPlayerPositionCheat();

				var subroot = GuidHelper.FindComponent<SubRoot>(msg.subGuid);
				if (subroot != null) {
					var respawn = subroot.gameObject.GetComponentInChildren<RespawnPoint>();
					if (respawn != null) {
						player.SetPosition(respawn.GetSpawnPosition());
						player.SetCurrentSub(subroot);
					}
				}
			}
		}

		private void Process(ClientVehicleMovement msg) {
			var gameObject = GuidHelper.Find(msg.vehicleGuid, false);
			if (gameObject == null) {
				return;
			}

			SyncedVehicle mvc = null;
			var vehicle = (Vehicle)gameObject.GetComponent(typeof(Vehicle));
			var subroot = (SubRoot)gameObject.GetComponent(typeof(SubRoot));

			if (vehicle != null) {
				var seamoth = vehicle as SeaMoth;
				var exosuit = vehicle as Exosuit;

				if (seamoth != null) {
					mvc = seamoth.gameObject.EnsureComponent<SyncedSeamoth>();
				} else if (exosuit != null) {
					mvc = exosuit.gameObject.EnsureComponent<SyncedExosuit>();
				}
			} else if (subroot != null) {
				if (subroot.GetComponent<SubControl>() != null) {
					mvc = subroot.gameObject.EnsureComponent<SyncedCyclops>();
				}
			}

			if (mvc != null) {
				mvc.Correct(msg.position, msg.rotation, msg.velocity, msg.timestamp);
			}

			RemotePlayer remotePlayer;
			if (remotePlayers.TryGetValue(msg.id, out remotePlayer)) {
				using (new MessageBlocker()) {
					remotePlayer.UpdateVehicleMovement(vehicle, subroot);
				}

				if (mvc != null) {
					mvc.SetAnimation(msg);
					mvc.SetSteeringWheel(msg.steeringWheelYaw, msg.steeringWheelPitch);
				}
			}
		}

		private void Process(ClientVehicleDocking msg) {
			var vehicle = GuidHelper.FindComponent<Vehicle>(msg.vehicleGuid);
			if (vehicle == null) {
				Log.Info("Couldn't find vehicle.");
				return;
			}

			var bay = FindDockingBay(msg.bayGuid, msg.subGuid, msg.bayPosition);

			using (new MessageBlocker()) {
				if (msg.docked) {
					if (bay != null && bay.GetDockedVehicle() == null) {
						/*
						bay.ReflectionSet("timeDockingStarted", Time.time);

						if (vehicle is Exosuit) {
							vehicle.transform.position = bay.dockingEndPosExo.position;
							vehicle.transform.rotation = bay.dockingEndPosExo.rotation;
						} else {
							vehicle.transform.position = bay.dockingEndPos.position;
							vehicle.transform.rotation = bay.dockingEndPos.rotation;
						}

						bay.DockVehicle(vehicle, false);

						Log.Info("Vehicle docked.");
						*/
						bay.SetVehicleDocked(vehicle);
					}
				}
			}
		}

		private void Process(ClientVehicleKill msg) {
			var gameObject = GuidHelper.Find(msg.vehicleGuid);
			if (gameObject == null) {
				return;
			}

			var live = gameObject.GetComponent<LiveMixin>();
			if (live != null) {
				live.Kill();
			}

			UnityEngine.GameObject.Destroy(gameObject);
		}

		private void Process(ClientVehicleColorChange msg) {
			var subname = GuidHelper.FindComponentInChildren<SubName>(msg.vehicleGuid);
			if (subname != null) {
				using (new MessageBlocker()) {
					subname.SetColor(msg.index, msg.hsb, msg.color);
				}
			}
		}

		private void Process(ClientVehicleNameChange msg) {
			var subname = GuidHelper.FindComponentInChildren<SubName>(msg.vehicleGuid);
			if (subname != null) {
				using (new MessageBlocker()) {
					subname.SetName(msg.name);
				}
			}
		}

		private void Process(ClientCreatureUpdate msg) {
			var target = GuidHelper.Find(msg.creatureGuid, false);
			if (target == null) {
				if (LargeWorld.main == null) {
					return;
				}

				if (Vector3.SqrMagnitude(Player.main.transform.position - msg.position) > SyncedCreature.maxSpawnDistSq) {
					return;
				}

				var prefab = CraftData.GetPrefabForTechType(msg.tech, false);
				if (prefab == null) {
					return;
				}

				target = UnityEngine.Object.Instantiate(prefab, msg.position, msg.rotation);
				GuidHelper.Set(target, msg.creatureGuid);
				var component = (LargeWorldEntity)target.AddComponent(typeof(LargeWorldEntity));
				component.cellLevel = LargeWorldEntity.CellLevel.Medium;

				LargeWorld.main.streamer.cellManager.RegisterEntity(component);
				target.SetActive(true);
			}

			var sync = (SyncedCreature)target.EnsureComponent(typeof(SyncedCreature));
			sync.ownership = false;
			sync.Correct(msg);
		}

		private void Process(ClientLiveMixinChange msg) {
			var target = GuidHelper.FindComponent<LiveMixin>(msg.targetGuid, false);
			if (target == null) {
				return;
			}

			using (new MessageBlocker()) {
				if (msg.force || msg.health == 0f) {
					target.health = msg.health;
				}

				if (Mathf.Abs(target.health - msg.health) > target.maxHealth * 0.15f) {
					target.health = msg.health;
				}

				if (msg.amount < 0f) {
					target.TakeDamage(msg.amount, msg.position, msg.type);
				} else if (msg.amount > 0f) {
					target.AddHealth(msg.amount);
				}

				if (target.health > target.maxHealth) {
					target.health = target.maxHealth;
				}
			}
		}

		private void Process(ClientPlayerVitals msg) {
			if (msg.id == self.id) {
				var player = Player.main;
				if (player != null) {
					if (player.liveMixin != null) {
						player.liveMixin.health = msg.health;
					}

					var survival = player.GetComponent<Survival>();
					if (survival != null) {
						survival.food = msg.food;
						survival.water = msg.water;
					}
				}
			}
		}

		private void Process(ClientBuildConstruct msg) {
			var prefab = CraftData.GetBuildPrefab(msg.tech);
			if (prefab == null) {
				return;
			}

			Logic.Building.buildBase = GuidHelper.FindComponent<Base>(msg.targetGuid);
			Logic.Building.buildDirection = msg.direction;
			Logic.Building.buildBaseFace = msg.face;
			Logic.Building.buildBaseAnchor = msg.anchor;

			Logic.Building.buildPlaceActive = true;

			using (var change = new BuildChange(msg.cameraPosition, msg.cameraRotation, msg.subGuid)) {
				try {
					MultiplayerBuilder.baseGhostGuid = msg.ghostGuid;
					MultiplayerBuilder.targetGuid = msg.targetGuid;
					MultiplayerBuilder.targetGuid = msg.targetGuid;
					MultiplayerBuilder.overridePosition = msg.objectPosition;
					MultiplayerBuilder.overrideRotation = msg.objectRotation;
					MultiplayerBuilder.additiveRotation = msg.additiveRotation;
					Builder.additiveRotation = msg.additiveRotation;
					MultiplayerBuilder.Begin(prefab);
					MultiplayerBuilder.Update();

					GameObject gameObject;
					Constructable constructable;

					if (MultiplayerBuilder.TryPlace(out gameObject, out constructable)) {
						if (gameObject != null) {
							GuidHelper.Set(gameObject, msg.objectGuid);
						}

						if (constructable != null) {
							var method = typeof(Constructable).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
							method.Invoke(constructable, new object[] { });
						}
					} else {
						Log.Warn("Couldn't place piece: " + msg.tech);
					}

					MultiplayerBuilder.End();
				} catch (Exception exception) {
					UnityEngine.Debug.LogException(exception);
				}
			}

			Logic.Building.buildPlaceActive = false;
		}

		private void Process(ClientBuildConstructChange msg) {
			var target = GuidHelper.FindComponent<Constructable>(msg.objectGuid);
			if (target == null) {
				Log.Info("Couldn't find constructable: " + msg.tech);
				return;
			}

			Logic.Building.buildConstructActive = true;
			Logic.Building.SetNewModule(null);

			try {
				if (target.constructed) {
					target.SetState(false, false);
				}

				target.constructedAmount = msg.amount;

				if (msg.constructing) {
					target.Construct();
				} else {
					target.Deconstruct();
				}

				if (msg.state) {
					target.SetState(true, true);

					GuidHelper.Set(Logic.Building.GetNewBase(), msg.baseGuid);
					GuidHelper.Set(Logic.Building.GetNewModule(), msg.moduleGuid);
				}
			} catch (Exception exception) {
				UnityEngine.Debug.LogException(exception);
			}

			Logic.Building.buildConstructActive = false;
		}

		private void Process(ClientBuildDeconstructBase msg) {
			var target = FindBaseDeconstructable(msg.deconstructableGuid, msg.baseGuid, msg.faceType, new Base.Face(msg.faceCell, msg.faceDirection), new Int3.Bounds(msg.boundsMin, msg.boundsMax));
			if (target == null) {
				return;
			}

			Logic.Building.buildConstructActive = true;

			try {
				target.Deconstruct();
				GuidHelper.Set(Logic.Building.GetNewConstructable(), msg.constructableGuid);
			} catch (Exception exception) {
				UnityEngine.Debug.LogException(exception);
			}

			Logic.Building.buildConstructActive = false;
		}

		private void Process(ClientItemDropped msg) {
			var gameObject = ObjectSerializer.GetGameObject(msg.data);
			gameObject.transform.position = msg.position;

			GuidHelper.Set(gameObject, msg.itemGuid);

			var waterParkObject = GuidHelper.Find(msg.waterParkGuid);
			if (waterParkObject != null) {
				var waterPark = waterParkObject.GetComponent<WaterPark>();
				if (waterPark != null) {
					var pickupable = gameObject.GetComponent<Pickupable>();
					if (pickupable != null) {
						waterPark.AddItem(pickupable);
					}
				}
			}

			var rigidbody = gameObject.GetComponent<Rigidbody>();
			if (rigidbody != null) {
				rigidbody.isKinematic = false;
			}

			SyncedObject.ApplyTo(gameObject);

			var constructor = gameObject.GetComponent<Constructor>();
			if (constructor != null) {
				var method = typeof(Constructor).GetMethod("Deploy", BindingFlags.NonPublic | BindingFlags.Instance);
				method.Invoke(constructor, new object[] { true });
				constructor.OnDeployAnimationStart();
				LargeWorldEntity.Register(constructor.gameObject);

				Utils.PlayEnvSound(constructor.releaseSound, constructor.transform.position, 20f);
			}
		}

		private void Process(ClientItemGrabbed msg) {
			var gameObject = GuidHelper.Find(msg.itemGuid);
			if (gameObject != null) {
				UnityEngine.Object.Destroy(gameObject);
			}
		}

		private void Process(ClientObjectUpdate msg) {
			var target = GuidHelper.FindComponent<SyncedObject>(msg.targetGuid);
			if (target != null) {
				target.Correct(msg.position, msg.rotation);
			}
		}

		private void Process(ClientEquipmentAddItem msg) {
			var owner = GuidHelper.Find(msg.ownerGuid);
			if (owner == null) {
				FindRemoteInventory(msg.ownerGuid)?.Add(msg.itemGuid, msg.data);
				return;
			}

			var gameObject = ObjectSerializer.GetGameObject(msg.data);
			var pickupable = gameObject.GetComponent<Pickupable>();

			GuidHelper.Set(gameObject, msg.itemGuid);

			var equipment = Helpers.GetEquipment(owner);
			if (equipment == null) {
				Log.Info("Couldn't find equipment: " + msg.ownerGuid);
				return;
			}

			using (new MessageBlocker()) {
				var inventoryItem = new InventoryItem(pickupable);
				inventoryItem.container = equipment;
				inventoryItem.item.Reparent(equipment.tr);

				var itemsBySlot = (Dictionary<string, InventoryItem>)equipment.ReflectionGet("equipment");
				itemsBySlot[msg.slot] = inventoryItem;

				equipment.ReflectionCall("UpdateCount", false, false, new object[] { pickupable.GetTechType(), true });
				Equipment.SendEquipmentEvent(pickupable, 0, owner, msg.slot); // equip event is = 0
				equipment.ReflectionCall("NotifyEquip", false, false, new object[] { msg.slot, inventoryItem });
			}
		}

		private void Process(ClientEquipmentRemoveItem msg) {
			var owner = GuidHelper.Find(msg.ownerGuid);
			if (owner == null) {
				FindRemoteInventory(msg.ownerGuid)?.Remove(msg.itemGuid);
				return;
			}

			var pickupable = GuidHelper.FindComponent<Pickupable>(msg.itemGuid);
			if (pickupable != null) {
				var equipment = Helpers.GetEquipment(owner);
				if (equipment != null) {
					var itemsBySlot = (Dictionary<string, InventoryItem>)equipment.ReflectionGet("equipment");
					var inventoryItem = itemsBySlot[msg.slot];
					itemsBySlot[msg.slot] = null;

					equipment.ReflectionCall("UpdateCount", false, false, new object[] { pickupable.GetTechType(), false });
					Equipment.SendEquipmentEvent(pickupable, 1, owner, msg.slot); // unequip event is = 1
					equipment.ReflectionCall("NotifyUnequip", false, false, new object[] { msg.slot, inventoryItem });
				}
			}

			UnityEngine.Object.Destroy(pickupable.gameObject);
		}

		private void Process(ClientFabricatorStart msg) {
			var fabricator = GuidHelper.FindComponentInChildren<Fabricator>(msg.fabricatorGuid, true);
			if (fabricator == null) {
				Log.Info("Couldn't find fabricator: " + msg.fabricatorGuid);
				return;
			}

			using (var x = new MessageBlocker()) {
				fabricator.crafterLogic?.Craft(msg.tech, msg.duration + 0.2f);
			}
		}

		private void Process(ClientFabricatorPickup msg) {
			var fabricator = GuidHelper.FindComponentInChildren<Fabricator>(msg.fabricatorGuid, true);
			if (fabricator == null) {
				Log.Info("Couldn't find fabricator: " + msg.fabricatorGuid);
				return;
			}

			var crafterLogic = fabricator.crafterLogic;
			if (crafterLogic == null) {
				return;
			}

			if (crafterLogic.numCrafted > 0) {
				crafterLogic.numCrafted -= 1;

				if (crafterLogic.numCrafted == 0) {
					crafterLogic.Reset();
				}
			}
		}

		private void Process(ClientOpenableStateChanged msg) {
			var openable = GuidHelper.FindComponent<Openable>(msg.objectGuid);
			if (openable != null) {
				using (var x = new MessageBlocker()) {
					openable.PlayOpenAnimation(msg.state, msg.duration);
				}
			}
		}

		private void Process(ClientContainerAddItem msg) {
			var owner = GuidHelper.Find(msg.ownerGuid);
			var container = Helpers.GetItemsContainer(owner);
			if (container == null) {
				FindRemoteInventory(msg.ownerGuid)?.Add(msg.itemGuid, msg.data);
				return;
			}

			using (new MessageBlocker()) {
				var item = ObjectSerializer.GetGameObject(msg.data);
				GuidHelper.Set(item, msg.itemGuid);

				var pickupable = item.GetComponent<Pickupable>();
				if (pickupable == null) {
					return;
				}

				container.AddItem(pickupable);
			}
		}

		private void Process(ClientContainerRemoveItem msg) {
			var owner = GuidHelper.Find(msg.ownerGuid);
			var container = Helpers.GetItemsContainer(owner);
			if (container == null) {
				FindRemoteInventory(msg.ownerGuid)?.Remove(msg.itemGuid);
				return;
			}

			var pickupable = GuidHelper.FindComponent<Pickupable>(msg.itemGuid);
			if (pickupable == null) {
				return;
			}

			using (new MessageBlocker()) {
				container.RemoveItem(pickupable, true);
			}
		}

		private void Process(ClientScanProgress msg) {
			var entryData = PDAScanner.GetEntryData(msg.tech);
			if (entryData == null) {
				return;
			}

			using (new MessageBlocker()) {
				PDAScanner.Entry entry;
				if (!PDAScanner.GetPartialEntryByKey(msg.tech, out entry)) {
					var methodAdd = typeof(PDAScanner).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(TechType), typeof(int) }, null);
					entry = (PDAScanner.Entry)methodAdd.Invoke(null, new object[] { msg.tech, 0 });
				}

				if (entry != null) {
					entry.unlocked = msg.progress;
					if (entry.unlocked >= entryData.totalFragments) {
						var partial = (List<PDAScanner.Entry>)(typeof(PDAScanner).GetField("partial", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
						var complete = (HashSet<TechType>)(typeof(PDAScanner).GetField("complete", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));

						partial.Remove(entry);
						complete.Add(entry.techType);
					}
				}
			}
		}

		private void Process(ClientScanEncyclopedia msg) {
			using (new MessageBlocker()) {
				PDAEncyclopedia.Add(msg.key, false);
			}
		}

		private void Process(ClientScanKnownTech msg) {
			using (new MessageBlocker()) {
				KnownTech.Add(msg.tech, false);
			}
		}

		private void Process(ClientStoryGoal msg) {
			using (new MessageBlocker()) {
				if (msg.key == "AuroraRadiationFixed") {
					var radiation = LeakingRadiation.main;
					radiation.ReflectionCall("OnConsoleCommand_fixleaks");
					radiation.ReflectionCall("OnConsoleCommand_decontaminate");
				}

				if (msg.key == "Infection_Progress5") {
					Player.main.infectedMixin.RemoveInfection();
				}

				// Story.StoryGoalManager.main.completedGoals.Add(msg.key);
				Story.StoryGoalManager.main.OnGoalComplete(msg.key);

				if (msg.goal == Story.GoalType.PDA) {
					var entries = PDALog.Serialize();
					if (!entries.ContainsKey(msg.key)) {
						PDALog.EntryData data;
						if (PDALog.GetEntryData(msg.key, out data)) {
							PDALog.Entry entry = new PDALog.Entry();
							entry.data = data;
							entry.timestamp = (float)msg.timestamp;
							entries.Add(data.key, entry);
						}
					}
				}
			}
		}

		private void Process(ClientToggleLight msg) {
			using (new MessageBlocker()) {
				var toggleLights = GuidHelper.FindComponentInChildren<ToggleLights>(msg.objectGuid);
				if (toggleLights == null) {
					return;
				}

				if (msg.state == toggleLights.GetLightsActive()) {
					return;
				}

				toggleLights.SetLightsActive(msg.state);
			}
		}

		private void Process(ClientPowerChange msg) {
			var target = GuidHelper.FindComponent<PowerMonitor>(msg.targetGuid);
			if (target == null) {
				Log.Warn("Couldn't find power monitor: " + msg.targetGuid);
				return;
			}

			using (new MessageBlocker()) {
				if (msg.force) {
					target.Force(msg.total);
				} else {
					target.Correct(msg.total);
				}
			}
		}

		private void Process(ClientCyclopsHorn msg) {
			var horn = GuidHelper.FindComponent<CyclopsHornControl>(msg.vehicleGuid);
			if (horn == null) {
				return;
			}

			Utils.PlayEnvSound(horn.hornSound, horn.hornSound.gameObject.transform.position, 40f);
		}

		private void Process(ClientCyclopsState msg) {
			var cyclops = GuidHelper.Find(msg.vehicleGuid);
			if (cyclops == null) {
				return;
			}

			using (new MessageBlocker()) {
				var shield = cyclops.GetComponentInChildren<CyclopsShieldButton>();
				if (shield != null) {
					bool active = (bool)shield.ReflectionGet("active");
					if (active != msg.shield) {
						shield.OnClick();
					}
				}

				var engineState = cyclops.GetComponentInChildren<CyclopsEngineChangeState>();
				if (engineState != null) {
					if (msg.engineOn == engineState.motorMode.engineOn) {
						if (msg.engineStarting != (bool)engineState.ReflectionGet("startEngine")) {
							if (Player.main.currentSub != engineState.subRoot) {
								engineState.ReflectionSet("startEngine", !msg.engineOn);
								engineState.ReflectionSet("invalidButton", true);
								engineState.Invoke("ResetInvalidButton", 2.5f);
								engineState.subRoot.BroadcastMessage("InvokeChangeEngineState", !msg.engineOn, SendMessageOptions.RequireReceiver);
							} else {
								engineState.ReflectionSet("invalidButton", false);
								engineState.OnClick();
							}
						}
					} else {
						engineState.motorMode.ReflectionSet("engineOnOldState", msg.engineOn);
						engineState.motorMode.RestoreEngineState();
					}
				}

				var lighting = cyclops.GetComponentInChildren<CyclopsLightingPanel>();
				if (lighting != null) {
					if (msg.floodLights != lighting.floodlightsOn) {
						lighting.ToggleFloodlights();
					}

					if (msg.internalLights != lighting.lightingOn) {
						lighting.ToggleInternalLighting();
					}
				}

				var silentRunning = cyclops.GetComponentInChildren<CyclopsSilentRunningAbilityButton>();
				if (silentRunning != null) {
					bool active = (bool)silentRunning.ReflectionGet("active");
					if (msg.silentRunning != active) {
						if (msg.silentRunning) {
							silentRunning.ReflectionCall("TurnOnSilentRunning");
						} else {
							silentRunning.ReflectionCall("TurnOffSilentRunning");
						}
					}
				}

				var motorMode = cyclops.GetComponentInChildren<CyclopsMotorMode>();
				if (motorMode != null) {
					if (msg.motorMode != motorMode.cyclopsMotorMode) {
						motorMode.BroadcastMessage("SetCyclopsMotorMode", msg.motorMode, SendMessageOptions.RequireReceiver);
					}
				}
			}
		}

		private void Process(ClientItemLabel msg) {
			var target = GuidHelper.Find(msg.targetGuid);
			if (target == null) {
				return;
			}

			using (new MessageBlocker()) {
				var beaconLabel = target.GetComponentInChildren<BeaconLabel>();
				if (beaconLabel != null) {
					beaconLabel.SetLabel(msg.label);
				}
			}
		}

		private void Process(ClientConstructorCraft msg) {
			using (new MessageBlocker()) {
				Logic.Commands.SpawnPrefab(msg.itemGuid, msg.children, msg.tech, msg.spawnPosition, msg.spawnRotation);
			}
		}

		private void Process(ClientCommandSpawn msg) {
			using (new MessageBlocker()) {
				Logic.Commands.SpawnPrefab(msg.objectGuid, msg.children, msg.tech, msg.spawnPosition, msg.spawnRotation);
			}
		}

		private void Process(ClientCommandChat msg) {
			RemotePlayer player;
			if (remotePlayers.TryGetValue(msg.id, out player)) {
				playerChat.WriteMessage(player.username + ": " + msg.text);
			}
		}

		private IEnumerator LaunchGameAsync(ServerJoinInfo info) {
			uGUI_MainMenu.main.OnButtonSurvival();

			yield return new WaitUntil(() => LargeWorldStreamer.main != null);
			yield return new WaitUntil(() => LargeWorldStreamer.main.IsReady() || LargeWorldStreamer.main.IsWorldSettled());
			yield return new WaitUntil(() => !PAXTerrainController.main.isWorking);

			uGUI.main?.intro?.Stop(true);

			GuidHelper.Set(LargeWorldStreamer.main.globalRoot, info.globalRootGuid);
			GuidHelper.Set(Inventory.main.gameObject, info.equipmentGuid);

			var escapePod = EscapePod.main;
			if (escapePod != null) {
				escapePod.StartAtPosition(info.spawnPosition);

				var gameObject = escapePod.gameObject;
				GuidHelper.Set(gameObject, info.escapePodGuid);
				GuidHelper.Set(gameObject.GetComponentInChildren<Fabricator>(true)?.gameObject, info.fabricatorGuid);
				GuidHelper.Set(gameObject.GetComponentInChildren<MedicalCabinet>(true)?.gameObject, info.medkitGuid);
				GuidHelper.Set(gameObject.GetComponentInChildren<Radio>(true)?.gameObject, info.radioGuid);
			}

			UpdateServerTime(info.timestamp);

			Log.Info("Game loaded.");

			RemotePlayer.Initialize();

			self.loaded = true;
			this.blocked = false;

			DevConsole.disableConsole = true;
			DevConsole.RegisterConsoleCommand(this, "mpdump", false, false);
			DevConsole.RegisterConsoleCommand(this, "mpsave", false, false);
			DevConsole.RegisterConsoleCommand(this, "mpsync", false, false);

			// Since I never got around to synchronizing door state, this is a hack to avoid
			// having to save progress for using ion cubes to open doors.
			foreach (var obj in UnityEngine.Object.FindObjectsOfType<PrecursorTeleporter>()) {
				obj.isOpen = true;
				obj.ToggleDoor(true);
			}

			//Uncomment for developing.
			//DevConsole.disableConsole = false;
			//GameModeUtils.ActivateCheat(GameModeOption.NoCost);
			//GameModeUtils.ActivateCheat(GameModeOption.NoOxygen);
			//GameModeUtils.ActivateCheat(GameModeOption.NoSurvival);
			//CraftData.AddToInventory(TechType.Builder, 1, false, true);

			Send(new ClientGameReload());

			yield break;
		}

		private void UpdateChat() {
			if ((bool)((DevConsole)Reflection.ReflectionGet<DevConsole>(null, "instance", false, true)).ReflectionGet("state")) {
				return;
			}

			if (Input.GetKey(KeyCode.T)) {
				playerChatInput.Show();
			}
		}

		private static T FindGameComponent<T>(string guid, TechType type, Vector3 position) where T : UnityEngine.MonoBehaviour {
			var gameObject = GuidHelper.Find(guid);
			if (gameObject != null) {
				return gameObject.GetComponent<T>();
			}

			foreach (var item in UnityEngine.GameObject.FindObjectsOfType<T>()) {
				if (type == TechType.None || CraftData.GetTechType(item.gameObject) == type) {
					if (Vector3.Distance(item.gameObject.transform.position, position) < objectSearchDistance) {
						GuidHelper.Set(item.gameObject, guid);
						return item.gameObject.GetComponent<T>();
					}
				}
			}

			return null;
		}

		private static BaseDeconstructable FindBaseDeconstructable(string guid, string baseGuid, Base.FaceType faceType, Base.Face face, Int3.Bounds bounds) {
			var baseObject = GuidHelper.Find(baseGuid);
			if (baseObject != null) {
				foreach (var item in baseObject.GetAllComponentsInChildren<BaseDeconstructable>()) {
					if (item.faceType != faceType) {
						continue;
					}

					if (item.bounds != bounds) {
						continue;
					}

					if (item.face.GetValueOrDefault() != face) {
						continue;
					}

					GuidHelper.Set(item.gameObject, guid);
					return item;
				}
			}

			if (string.IsNullOrEmpty(guid) == false) {
				Log.Warn("Couldn't find base deconstructable: " + guid);
			}

			return null;
		}

		private static VehicleDockingBay FindDockingBay(string guid, string subGuid, Vector3 position) {
			var result = GuidHelper.FindComponent<VehicleDockingBay>(guid);
			if (result != null) {
				return result;
			}

			var subroot = GuidHelper.FindComponent<SubRoot>(subGuid);
			if (subroot != null) {
				if (subroot.isBase == false) {
					return subroot.GetComponentInChildren<VehicleDockingBay>();
				}

				foreach (var item in subroot.GetAllComponentsInChildren<VehicleDockingBay>()) {
					if (Vector3.Distance(item.gameObject.transform.position, position) < objectSearchDistance) {
						GuidHelper.Set(item.gameObject, guid);
						return item;
					}
				}
			}

			if (string.IsNullOrEmpty(guid) == false) {
				Log.Warn("Couldn't find docking bay: " + guid);
			}

			return null;
		}

		private RemoteInventory FindRemoteInventory(string guid) {
			foreach (var entry in remotePlayers.Values) {
				if (entry.inventory.guid == guid) {
					return entry.inventory;
				}
			}

			return null;
		}

		private static void UpdateServerTime(double timestamp) {
			if (Math.Abs(DayNightCycle.main.timePassedAsDouble - timestamp) > 4.0) {
				DayNightCycle.main.timePassedAsDouble = timestamp;
			}
		}

		private static void LogMessage(string baseString, Message message) {
			var type = message.GetType();
			if (type != typeof(ClientPlayerMovement) 
				&& type != typeof(ClientVehicleMovement) 
				&& type != typeof(ClientPlayerVitals) 
				&& type != typeof(ClientObjectUpdate)
				&& type != typeof(ClientCreatureUpdate)
				&& type != typeof(LiveMixin)) {
				Log.Info(baseString + " " + message.ToString());
			}
		}

		private void OnConsoleCommand_mpsave() {
			Send(new ServerSaveRequest());
		}

		private void OnConsoleCommand_mpsync() {
			Send(new ServerSyncRequest());
		}

		private void OnConsoleCommand_mpdump() {
			SceneDumper.DumpScene();
		}

		private void GameReset() {
			foreach (var player in remotePlayers.Values) {
				player.Destroy();
			}
			remotePlayers.Clear();

			if (self.loaded) {
				uGUI.main.quickSlots.SetTarget(null);
				Player.main.SetCurrentSub(null);
				Player.main.ToNormalMode(false);
				Player.main.transform.parent = null;
				Inventory.main.ResetInventory();

				var lws = LargeWorldStreamer.main;
				lws.cellManager.IncreaseFreezeCount();
				lws.frozen = true;

				try {
					lws.UnloadGlobalRoot();
					lws.LoadGlobalRoot();
				} finally {
					lws.frozen = false;
					lws.cellManager.DecreaseFreezeCount();
				}
				
			}
		}
	}
}
