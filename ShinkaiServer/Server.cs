using System;
using System.Collections.Generic;
using System.Reflection;
using LiteNetLib;
using LiteNetLib.Utils;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;
using ShinkaiServer.Logic;

namespace ShinkaiServer {
	public class Server : INetEventListener {
		public class Client {
			public readonly int id;
			public readonly NetPeer peer;
			public readonly SavedPlayer player;
			public SavedVehicle vehicle;
			public bool ready;

			public Client(int id, NetPeer peer, SavedPlayer player) {
				this.id = id;
				this.peer = peer;
				this.player = player;
			}

			public bool SetVehicle(SavedVehicle active) {
				if (vehicle != active) {
					if (vehicle != null && vehicle.pilot == id) {
						vehicle.pilot = 0;
					}

					vehicle = active;
				}

				if (active == null) {
					return false;
				}
				
				if (active.pilot == 0) {
					active.pilot = id;
				}

				return (active.pilot == id);
			}
		}

		public const string savefile = "server.savedata";

		private Serializer serializer;
		private Processor processor;
		private NetManager manager;
		private State state;

		private double lastUpdateTime;
		private Dictionary<NetPeer, Client> peers = new Dictionary<NetPeer, Client>();

		public Server() {
			serializer = new Serializer();
			MessageExtensions.Setup(serializer);
			processor = new Processor(serializer);

			manager = new NetManager(this, 128);
			manager.PingInterval = Config.pingInterval;
			manager.DisconnectTimeout = Config.disconnectTimeout;
			manager.MergeEnabled = true;
			// manager.SimulateLatency = true;
			// manager.SimulatePacketLoss = true;

			AddPeerHandler<ServerJoinRequest>(Process);
			AddClientHandler<ClientGameReload>(Process);
			AddClientHandler<ClientPlayerMovement>(Process);
			AddClientHandler<ClientPlayerVitals>(Process);
			AddClientHandler<ClientPlayerDeath>(Process);
			AddClientHandler<ClientVehicleMovement>(Process);
			AddClientHandler<ClientVehicleDocking>(Process);
			AddClientHandler<ClientVehicleKill>(Process);
			AddClientHandler<ClientVehicleColorChange>(Process);
			AddClientHandler<ClientVehicleNameChange>(Process);
			AddClientHandler<ClientCyclopsHorn>(ProcessAuthPass);
			AddClientHandler<ClientCyclopsState>(Process);
			AddClientHandler<ClientCreatureUpdate>(ProcessAuthPass);
			AddClientHandler<ClientLiveMixinChange>(Process);
			AddClientHandler<ClientBuildConstruct>(ProcessBuildPass);
			AddClientHandler<ClientBuildConstructChange>(ProcessBuildPass);
			AddClientHandler<ClientBuildDeconstructBase>(ProcessBuildPass);
			AddClientHandler<ClientItemDropped>(Process);
			AddClientHandler<ClientItemGrabbed>(Process);
			AddClientHandler<ClientItemLabel>(Process);
			AddClientHandler<ClientResourceBreak>(Process);
			AddClientHandler<ClientObjectUpdate>(Process);
			AddClientHandler<ClientFabricatorStart>(ProcessAuthPass);
			AddClientHandler<ClientFabricatorPickup>(ProcessAuthPass);
			AddClientHandler<ClientConstructorCraft>(Process);
			AddClientHandler<ClientOpenableStateChanged>(ProcessAuthPass);
			AddClientHandler<ClientEquipmentAddItem>(Process);
			AddClientHandler<ClientEquipmentRemoveItem>(Process);
			AddClientHandler<ClientContainerAddItem>(Process);
			AddClientHandler<ClientContainerRemoveItem>(Process);
			AddClientHandler<ClientScanProgress>(ProcessUnlock);
			AddClientHandler<ClientScanEncyclopedia>(ProcessUnlock);
			AddClientHandler<ClientStoryGoal>(ProcessUnlock);
			AddClientHandler<ClientScanKnownTech>(ProcessUnlock);
			AddClientHandler<ClientCommandSpawn>(Process);
			AddClientHandler<ClientCommandChat>(Process);
			AddClientHandler<ClientToggleLight>(ProcessAuthPass);
			AddClientHandler<ClientPowerChange>(Process);
			AddClientHandler<ServerSaveRequest>(Process);
			AddClientHandler<ServerSyncRequest>(Process);

			foreach (var type in typeof(Message).Assembly.GetTypes()) {
				if (typeof(Message).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass) {
					if (processor.Has(type) == false) {
						Log.Warn("Unhandled message type: " + type.FullName);
					}
				}
			}

			Load();
		}

		public void Load() {
			state = State.Load(savefile);

			foreach (var vehicle in state.history.vehicles.Values) {
				vehicle.pilot = 0;
			}
		}

		public void Save() {
			State.Save(savefile, state);
		}

		public void Start() {
			if (manager.Start(Config.connectPort) == false) {
				Log.Error("Couldn't start server.");
				return;
			}

			Log.Info("Listening on port: " + manager.LocalPort);
		}

		public void Update() {
			manager.PollEvents();

			double timestamp = state.gameTime.GetCurrentTime();
			if (timestamp - lastUpdateTime > 30f) {
				lastUpdateTime = timestamp;

				state.timeUpdate.timestamp = timestamp;
				SendToAll(null, state.timeUpdate);
			}
		}

		public void SendToAll(NetPeer self, Message message, DeliveryMethod method = DeliveryMethod.ReliableOrdered) {
			if (message == null) {
				return;
			}

			processor.Serialize(message);

			foreach (var item in peers) {
				var client = item.Value;
				if (client.peer != self && client.ready) {
					processor.Send(client.peer, method);
				}
			}
		}

		public void SendToPeer(NetPeer peer, Message message, DeliveryMethod method = DeliveryMethod.ReliableOrdered) {
			if (message != null) {
				processor.Serialize(message);
				processor.Send(peer, method);
			}
		}

		public void OnNetworkError(NetEndPoint endpoint, int socketErrorCode) {
			Log.Warn("Socket error: " + socketErrorCode);
		}

		public void OnNetworkReceive(NetPeer peer, NetDataReader reader, DeliveryMethod channel) {
			processor.ReadAll(peer, reader);
		}

		public void OnNetworkReceiveUnconnected(NetEndPoint endpoint, NetDataReader reader, UnconnectedMessageType type) {
		}

		public void OnNetworkLatencyUpdate(NetPeer peer, int latency) {
		}

		public void OnConnectionRequest(LiteNetLib.ConnectionRequest request) {
			request.Accept();
		}

		public virtual void OnPeerConnected(NetPeer peer) {
			Log.Debug("Peer connected: " + peer.EndPoint);
		}

		public virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
			Client client;
			if (peers.TryGetValue(peer, out client)) {
				Log.Debug(client.player.username + " disconnected: " + peer.EndPoint);

				var res = new ClientPlayerLeave();
				res.id = client.id;
				SendToAll(client.peer, res);

				client.SetVehicle(null);
				peers.Remove(peer);
				Save();
			} else {
				Log.Debug("Disconnect from peer: {0}, {1}", peer.EndPoint, disconnectInfo.Reason);
			}
		}

		private void AddPeerHandler<T>(Action<NetPeer, T> onReceive) where T : Message, new() {
			processor.Add(typeof(T), (peer, reader) => {
				var inst = new T();
				if (serializer.Deserialize(reader, inst)) {
					onReceive(peer, inst);
				}
			});
		}

		private void AddClientHandler<T>(Action<Client, T> onReceive) where T : Message, new() {
			processor.Add(typeof(T), (peer, reader) => {
				var inst = new T();
				if (serializer.Deserialize(reader, inst)) {
					Client client;
					if (peers.TryGetValue(peer, out client)) {
						onReceive(client, inst);
					}
				}
			});
		}

		private void ProcessAuthPass(Client client, Message msg) {
			SendToAll(client.peer, msg);
		}

		private void ProcessBuildPass(Client client, Message msg) {
			state.history.building.Add(msg);
			SendToAll(client.peer, msg);
		}

		private void ProcessUnlock(Client client, Message msg) {
			client.player.unlocks.Add(msg);
			state.history.unlocks.Add(msg);
		}

		private void Process(NetPeer peer, ServerJoinRequest msg) {
			if (msg.username == null || msg.username.Length < 4 || msg.username.Length > 30) {
				SendToPeer(peer, new ServerJoinReject { reason = "Invalid name" });
				return;
			}

			if (peers.ContainsKey(peer)) {
				SendToPeer(peer, new ServerJoinReject { reason = "Already connected" });
				return;
			}

			var player = state.history.GetPlayer(msg.username, msg.password);
			if (player == null) {
				SendToPeer(peer, new ServerJoinReject { reason = "Bad login" });
				return;
			}

			foreach (var other in peers.Values) {
				if (player.id == other.player.id) {
					SendToPeer(peer, new ServerJoinReject { reason = "Name in use" });
					return;
				}
			}

			Client client;
			if (peers.TryGetValue(peer, out client) == false) {
				client = new Client(player.id, peer, player);
				peers.Add(peer, client);
			}

			client.ready = false;

			var res = state.serverInfo;
			res.timestamp = state.gameTime.GetCurrentTime();
			res.id = client.id;

			if (player.equipmentGuid == null) {
				player.equipmentGuid = State.GetGuid();
			}

			res.inventoryGuid = player.inventoryGuid;
			res.equipmentGuid = player.equipmentGuid;

			SendToPeer(peer, res);

			Log.Info("Player {0} joining: {1}", client.id, client.player.username);
		}

		private void Process(Client client, ClientGameReload msg) {
			Log.Info("Player {0} loaded: {1}", client.id, client.player.username);

			ReloadClient(client);
		}

		private void Process(Client client, ClientPlayerMovement msg) {
			msg.id = client.id;
			client.player.movement = msg;
			client.SetVehicle(null);

			SendToAll(client.peer, msg, DeliveryMethod.Sequenced);
		}

		private void Process(Client client, ClientPlayerVitals msg) {
			msg.id = client.id;
			client.player.vitals = msg;
		}

		private void Process(Client client, ClientPlayerDeath msg) {
			if (msg.id == client.id) {
				msg.id = client.id;
				SendToAll(client.peer, msg);
			}
		}

		private void Process(Client client, ClientItemDropped msg) {
			var item = state.history.items.GetOrAddNew(msg.itemGuid);
			item.drop = msg;

			SendToAll(client.peer, msg);
		}

		private void Process(Client client, ClientItemGrabbed msg) {
			state.history.items.Remove(msg.itemGuid);
			SendToAll(client.peer, msg);
		}

		private void Process(Client client, ClientItemLabel msg) {
			SavedItem item;
			if (state.history.items.TryGetValue(msg.targetGuid, out item)) {
				item.label = msg;
				SendToAll(client.peer, msg);
			}
		}

		private void Process(Client client, ClientResourceBreak msg) {
			state.history.broken.Add(msg);
			// SendToAll(client.peer, msg);
		}

		private void Process(Client client, ClientObjectUpdate msg) {
			SavedVehicle vehicle;
			if (state.history.vehicles.TryGetValue(msg.targetGuid, out vehicle)) {
				if (vehicle.pilot == 0) {
					vehicle.position = msg.position;
					vehicle.rotation = msg.rotation;
				}
			}

			SavedItem item;
			if (state.history.items.TryGetValue(msg.targetGuid, out item)) {
				item.update = msg;
			}

			SendToAll(client.peer, msg);
		}

		private void Process(Client client, ClientVehicleMovement msg) {
			msg.id = client.id;

			SavedVehicle vehicle;
			if (state.history.vehicles.TryGetValue(msg.vehicleGuid, out vehicle)) {
				if (client.SetVehicle(vehicle)) {
					vehicle.position = msg.position;
					vehicle.rotation = msg.rotation;
					vehicle.movement = msg;

					SendToAll(client.peer, msg, DeliveryMethod.Sequenced);
				}
			}
		}

		private void Process(Client client, ClientVehicleDocking msg) {
			msg.id = client.id;

			SavedVehicle vehicle;
			if (state.history.vehicles.TryGetValue(msg.vehicleGuid, out vehicle)) {
				if (msg.docked) {
					vehicle.docking = msg;
				} else {
					vehicle.docking = null;
				}

				SendToAll(client.peer, msg);
			}
		}

		private void Process(Client client, ClientVehicleKill msg) {
			SavedVehicle vehicle;
			if (state.history.vehicles.TryGetValue(msg.vehicleGuid, out vehicle)) {
				state.history.vehicles.Remove(msg.vehicleGuid);
			}

			SendToAll(client.peer, msg);
		}

		private void Process(Client client, ClientVehicleColorChange msg) {
			SavedVehicle vehicle;
			if (state.history.vehicles.TryGetValue(msg.vehicleGuid, out vehicle)) {
				vehicle.colors[msg.index] = msg;
				SendToAll(client.peer, msg);
			}
		}

		private void Process(Client client, ClientVehicleNameChange msg) {
			SavedVehicle vehicle;
			if (state.history.vehicles.TryGetValue(msg.vehicleGuid, out vehicle)) {
				vehicle.name = msg;
				SendToAll(client.peer, msg);
			}
		}

		private void Process(Client client, ClientConstructorCraft msg) {
			var vehicle = state.history.vehicles.GetOrAddNew(msg.itemGuid);
			vehicle.position = msg.spawnPosition;
			vehicle.rotation = msg.spawnRotation;
			vehicle.craft = msg;

			SendToAll(client.peer, msg);
		}


		private void Process(Client client, ClientCommandSpawn msg) {
			if (msg.tech == TechType.Cyclops || msg.tech == TechType.Exosuit || msg.tech == TechType.Seamoth) {
				var vehicle = state.history.vehicles.GetOrAddNew(msg.objectGuid);
				vehicle.position = msg.spawnPosition;
				vehicle.rotation = msg.spawnRotation;
				vehicle.spawn = msg;
			} else {
				state.history.building.Add(msg);
			}

			SendToAll(client.peer, msg);
		}

		private void Process(Client client, ClientCommandChat msg) {
			msg.id = client.id;
			SendToAll(client.peer, msg);
		}


		private void Process(Client client, ClientCyclopsState msg) {
			SavedVehicle vehicle;
			if (state.history.vehicles.TryGetValue(msg.vehicleGuid, out vehicle)) {
				vehicle.cyclopsState = msg;
				SendToAll(client.peer, msg);
			}
		}

		private void Process(Client client, ClientLiveMixinChange msg) {
			SavedVehicle vehicle;
			if (state.history.vehicles.TryGetValue(msg.targetGuid, out vehicle)) {
				vehicle.health = msg;
			}

			msg.force = false;
			SendToAll(client.peer, msg);
			msg.force = true;
		}

		private void Process(Client client, ClientContainerAddItem msg) {
			var container = state.history.container.GetOrAddNew(msg.ownerGuid);
			container.items[msg.itemGuid] = msg;

			SendToAll(client.peer, msg);
		}

		private void Process(Client client, ClientContainerRemoveItem msg) {
			var container = state.history.container.GetOrAddNew(msg.ownerGuid);
			container.items.Remove(msg.itemGuid);

			SendToAll(client.peer, msg);
		}

		private void Process(Client client, ClientEquipmentAddItem msg) {
			var equipment = state.history.equipment.GetOrAddNew(msg.ownerGuid);
			equipment.slots[msg.slot] = msg;

			SendToAll(client.peer, msg);
		}

		private void Process(Client client, ClientEquipmentRemoveItem msg) {
			var equipment = state.history.equipment.GetOrAddNew(msg.ownerGuid);
			equipment.slots.Remove(msg.slot);

			SendToAll(client.peer, msg);
		}

		private void Process(Client client, ClientPowerChange msg) {
			state.history.power[msg.targetGuid] = msg;
			msg.force = false;
			SendToAll(client.peer, msg);
			msg.force = true;
		}

		private void Process(Client client, ServerSaveRequest msg) {
			Save();
		}

		private void Process(Client client, ServerSyncRequest msg) {
			Log.Info("Reload requested from player: " + client.player.username);

			foreach (var entry in peers.Values) {
				if (entry.ready) {
					SendToPeer(entry.peer, msg);
					ReloadClient(entry);
				}
			}
		}

		private void ReloadClient(Client client) {
			var peer = client.peer;

			foreach (var other in peers.Values) {
				if (client.id == other.id) {
					continue;
				}

				SendToPeer(other.peer, new ClientPlayerJoin {
					id = client.id,
					username = client.player.username,
					inventoryGuid = client.player.equipmentGuid
				});

				SendToPeer(peer, new ClientPlayerJoin {
					id = other.id,
					username = other.player.username,
					inventoryGuid = other.player.equipmentGuid
				});

				SendToPeer(peer, other.player.movement);
			}

			client.ready = true;

			state.timeUpdate.timestamp = state.gameTime.GetCurrentTime();
			SendToPeer(peer, state.timeUpdate);

			foreach (var entry in client.player.unlocks) {
				SendToPeer(peer, entry);
			}

			var vehicles = state.history.vehicles.Values;

			foreach (var vehicle in vehicles) {
				if (vehicle.craft != null) {
					vehicle.craft.spawnPosition = vehicle.position;
					vehicle.craft.spawnRotation = vehicle.rotation;
					SendToPeer(peer, vehicle.craft);
				} else if (vehicle.spawn != null) {
					vehicle.spawn.spawnPosition = vehicle.position;
					vehicle.spawn.spawnRotation = vehicle.rotation;
					SendToPeer(peer, vehicle.spawn);
				}

				SendToPeer(peer, vehicle.name);

				foreach (var color in vehicle.colors.Values) {
					SendToPeer(peer, color);
				}

				SendToPeer(peer, vehicle.cyclopsState);
				SendToPeer(peer, vehicle.health);
			}

			foreach (var entry in state.history.building) {
				SendToPeer(peer, entry);
			}

			foreach (var vehicle in vehicles) {
				SendToPeer(peer, vehicle.docking);
			}

			foreach (var item in state.history.items.Values) {
				SendToPeer(peer, item.drop);
				SendToPeer(peer, item.update);
			}

			foreach (var container in state.history.container.Values) {
				foreach (var item in container.items.Values) {
					SendToPeer(peer, item);
				}
			}

			foreach (var equipment in state.history.equipment.Values) {
				foreach (var item in equipment.slots.Values) {
					SendToPeer(peer, item);
				}
			}

			foreach (var item in state.history.power) {
				SendToPeer(peer, item.Value);
			}

			SendToPeer(peer, client.player.vitals);
			SendToPeer(peer, client.player.movement);
			SendToPeer(peer, new ServerSyncFinish());
		}
	}
}
