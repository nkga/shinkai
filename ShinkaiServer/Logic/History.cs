using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ShinkaiModel.Networking;

namespace ShinkaiServer.Logic {
	[Serializable]
	public class SavedContainer {
		public Dictionary<string, ClientContainerAddItem> items = new Dictionary<string, ClientContainerAddItem>();
	}

	[Serializable]
	public class SavedEquipment {
		public Dictionary<string, ClientEquipmentAddItem> slots = new Dictionary<string, ClientEquipmentAddItem>();
	}

	[Serializable]
	public class SavedItem {
		public ClientItemDropped drop;
		public ClientObjectUpdate update;
		public ClientItemLabel label;
	}

	[Serializable]
	public class SavedPlayer {
		public int id;
		public string username;
		public string password;
		public string inventoryGuid;
		public string equipmentGuid;
		public ClientPlayerVitals vitals;
		public ClientPlayerMovement movement;
		public List<Message> unlocks = new List<Message>();
	}

	[Serializable]
	public class SavedVehicle {
		public int pilot;
		public Vector3 position;
		public Quaternion rotation;
		public ClientConstructorCraft craft;
		public ClientCommandSpawn spawn;
		public ClientVehicleMovement movement;
		public ClientVehicleDocking docking;
		public ClientCyclopsState cyclopsState;
		public ClientLiveMixinChange health;
		public ClientVehicleNameChange name;
		public Dictionary<int, ClientVehicleColorChange> colors = new Dictionary<int, ClientVehicleColorChange>();
	}

	[Serializable]
	public class History {
		public List<Message> building = new List<Message>();
		public Dictionary<string, SavedContainer> container = new Dictionary<string, SavedContainer>();
		public Dictionary<string, SavedEquipment> equipment = new Dictionary<string, SavedEquipment>();
		public Dictionary<string, SavedItem> items = new Dictionary<string, SavedItem>();
		public Dictionary<string, SavedPlayer> players = new Dictionary<string, SavedPlayer>();
		public Dictionary<string, SavedVehicle> vehicles = new Dictionary<string, SavedVehicle>();
		public Dictionary<string, ClientPowerChange> power = new Dictionary<string, ClientPowerChange>();
		public List<Message> unlocks = new List<Message>();
		public List<ClientResourceBreak> broken = new List<ClientResourceBreak>();

		public SavedPlayer GetPlayer(string username, string password) {
			SavedPlayer player;
			if (players.TryGetValue(username, out player) == false) {
				player = new SavedPlayer();
				player.id = players.Count + 1;
				player.username = username;
				player.password = password;
				player.inventoryGuid = State.GetGuid();
				player.equipmentGuid = player.inventoryGuid;
				players.Add(username, player);
			}

			return (player.password == password) ? player : null;
		}
	}
}
