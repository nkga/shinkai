using System;
using System.Collections.Generic;
using UnityEngine;
using ShinkaiClient.Unity;

namespace ShinkaiClient.Mono {
	public class RemoteInventory {
		public readonly string guid;
		public readonly Dictionary<string, GameObject> items;

		public RemoteInventory(string guid) {
			this.guid = guid;
			items = new Dictionary<string, GameObject>();
		}

		public void Add(string itemGuid, byte[] data) {
			var gameObject = ObjectSerializer.GetGameObject(data);
			if (gameObject == null) {
				return;
			}

			var pickupable = gameObject.GetComponent<Pickupable>();
			if (pickupable != null) {
				pickupable.isPickupable = false;
				pickupable.SetVisible(false);
			}

			foreach (var rigidbody in gameObject.GetComponentsInChildren<Rigidbody>()) {
				UnityEngine.GameObject.Destroy(rigidbody);
			}

			foreach (var collider in gameObject.GetComponentsInChildren<Collider>()) {
				UnityEngine.GameObject.Destroy(collider);
			}

			items[itemGuid] = gameObject;
		}

		public GameObject Get(string itemGuid) {
			GameObject obj;
			if (items.TryGetValue(itemGuid, out obj)) {
				return obj;
			}

			return null;
		}

		public void Remove(string itemGuid) {
			GameObject obj;
			if (items.TryGetValue(itemGuid, out obj)) {
				items.Remove(itemGuid);
				UnityEngine.GameObject.Destroy(obj);
			}
		}

		public void Reset() {
			foreach (var obj in items.Values) {
				UnityEngine.GameObject.Destroy(obj);
			}
			items.Clear();
		}
	}
}
