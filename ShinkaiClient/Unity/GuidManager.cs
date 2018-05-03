using System;
using System.Collections.Generic;
using UnityEngine;
using ShinkaiModel.Core;	
using ShinkaiModel.Networking;
using ShinkaiClient.Mono;

namespace ShinkaiClient.Unity {
	public static class GuidHelper {
		private static readonly Type[] childGuidTypes = new Type[] {
			typeof(Openable),
			typeof(Fabricator),
			typeof(FireExtinguisherHolder),
			typeof(SeamothStorageContainer),
			typeof(StorageContainer),
			typeof(VehicleDockingBay),
			typeof(EnergyInterface),
			typeof(UpgradeConsole),
			typeof(PowerRelay)
		};

		static public GameObject Find(string guid, bool verbose = true) {
			if (string.IsNullOrEmpty(guid)) {
				return null;
			}

			UniqueIdentifier result;
			if (UniqueIdentifier.TryGetIdentifier(guid, out result) == false) {
				if (verbose) {
					Log.Warn("Couldn't find guid: " + guid);
				}
				return null;
			}

			return result.gameObject;
		}

		static public T FindComponent<T>(string guid, bool verbose = true) where T : MonoBehaviour {
			var gameObject = Find(guid, verbose);
			if (gameObject == null) {
				return null;
			}

			return gameObject.GetComponent<T>();
		}

		static public T FindComponentInChildren<T>(string guid, bool includeInactive = false) where T : MonoBehaviour {
			var gameObject = Find(guid);
			if (gameObject == null) {
				return null;
			}

			return gameObject.GetComponentInChildren<T>(includeInactive);
		}

		static public T FindComponentInParent<T>(string guid) where T : MonoBehaviour {
			var gameObject = Find(guid);
			if (gameObject == null) {
				return null;
			}

			return gameObject.GetComponentInParent<T>();
		}

		static public string Get(GameObject gameObject) {
			var uid = GetUniqueIdentifier(gameObject);
			if (uid == null) {
				return null;
			}

			return uid.Id;
		}

		static public void Set(GameObject gameObject, string guid) {
			if (string.IsNullOrEmpty(guid)) {
				return;
			}

			var uid = GetUniqueIdentifier(gameObject);
			if (uid != null) {
				uid.Id = guid;
			}
		}

		static public ChildGuid[] GetChildGuids(GameObject gameObject) {
			List<ChildGuid> children = new List<ChildGuid>();

			if (gameObject != null) {
				var fullName = gameObject.GetFullName() + "/"; ;

				foreach (var type in childGuidTypes) {
					var components = gameObject.GetComponentsInChildren(type, true);
					foreach (var component in components) {
						var child = new ChildGuid();
						child.guid = GuidHelper.Get(component.gameObject);

						string componentName = component.gameObject.GetFullName();
						child.path = componentName.Replace(fullName, "");

						children.Add(child);
					}
				}
			}

			return children.ToArray();
		}

		static public void SetChildGuids(GameObject gameObject, ChildGuid[] children) {
			if (gameObject == null || children == null) {
				return;
			}

			foreach (var child in children) {
				var transform = gameObject.transform.Find(child.path);
				if (transform != null) {
					GuidHelper.Set(transform.gameObject, child.guid);
				} else {
					Log.Warn("Couldn't find child on object: {0}, {1}", gameObject.name, child.path);
				}
			}
		}

		private static UniqueIdentifier GetUniqueIdentifier(GameObject gameObject) {
			if (gameObject == null) {
				return null;
			}

			var uid = (UniqueIdentifier)gameObject.GetComponent(typeof(UniqueIdentifier));
			if (uid == null) {
				uid = (PrefabIdentifier)gameObject.AddComponent(typeof(PrefabIdentifier));
			}

			return uid;
		}
	}
}
