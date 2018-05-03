using System.Reflection;
using UnityEngine;
using ShinkaiModel.Core;

namespace ShinkaiClient.Unity {
	public static class Helpers {
		public static Equipment GetEquipment(GameObject owner) {
			if (owner == null) {
				return null;
			}

			var inventory = owner.GetComponent<Inventory>();
			if (inventory != null) {
				return inventory.equipment;
			}

			var charger = owner.GetComponent<Charger>();
			if (charger != null) {
				return (Equipment)charger.ReflectionGet("equipment");
			}

			var nuclearReactor = owner.GetComponent<BaseNuclearReactor>();
			if (nuclearReactor != null) {
				return (Equipment)typeof(BaseNuclearReactor).GetProperty("equipment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(nuclearReactor, null);
			}

			var decoyTube = owner.GetComponent<CyclopsDecoyLoadingTube>();
			if (decoyTube != null) {
				return decoyTube.decoySlots;
			}

			var exosuit = owner.GetComponent<Exosuit>();
			if (exosuit != null) {
				return exosuit.modules;
			}

			var seamoth = owner.GetComponent<SeaMoth>();
			if (seamoth != null) {
				return seamoth.modules;
			}

			var upgradeConsole = owner.GetComponent<UpgradeConsole>();
			if (upgradeConsole != null) {
				return upgradeConsole.modules;
			}

			var vehicle = owner.GetComponent<Vehicle>();
			if (vehicle != null) {
				return vehicle.modules;
			}

			var vehicleUpgradeConsoleInput = owner.GetComponent<VehicleUpgradeConsoleInput>();
			if (vehicleUpgradeConsoleInput != null) {
				return vehicleUpgradeConsoleInput.equipment;
			}

			return null;
		}

		public static ItemsContainer GetItemsContainer(GameObject owner) {
			if (owner == null) {
				return null;
			}

			var inventory = owner.GetComponent<Inventory>();
			if (inventory != null) {
				return inventory.container;
			}

			var seamothStorageContainer = owner.GetComponent<SeamothStorageContainer>();
			if (seamothStorageContainer != null) {
				return seamothStorageContainer.container;
			}

			var restrictContainer = owner.GetComponent<RestrictStorageContainer>();
			if (restrictContainer != null) {
				return restrictContainer.storageContainer?.container;
			}

			var storageContainer = owner.GetComponentInChildren<StorageContainer>(true);
			if (storageContainer != null) {
				return storageContainer.container;
			}

			var bioReactor = owner.GetComponent<BaseBioReactor>();
			if (bioReactor != null) {
				return (ItemsContainer)typeof(BaseBioReactor).GetProperty("container", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bioReactor, null);
			}

			var mapRoom = owner.GetComponent<MapRoomFunctionality>();
			if (mapRoom != null) {
				return mapRoom.storageContainer.container;
			}

			return null;
		}
	}
}
