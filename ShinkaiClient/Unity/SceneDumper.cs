using System;
using System.IO;
using UnityEngine;

namespace ShinkaiClient.Unity {
	public static class SceneDumper {
		public static void DumpScene() {
			foreach (GameObject item in GameObject.FindObjectsOfType(typeof(GameObject))) {
				if (item.transform.parent == null) {
					DumpGameObject(item, "");
				}
			}
		}

		private static void DumpGameObject(GameObject gameObject, string indent) {
			var guid = "";
			var uid = gameObject.GetComponent<UniqueIdentifier>();
			if (uid != null) {
				guid = uid.Id;
			}

			Console.WriteLine("{0}+{1}, position={2}, guid={3}", indent, gameObject.name, gameObject.transform.position, guid);

			foreach (Component component in gameObject.GetComponents<Component>()) {
				DumpComponent(component, indent + "  ");
			}

			foreach (Transform child in gameObject.transform) {
				DumpGameObject(child.gameObject, indent + "  ");
			}
		}

		private static void DumpComponent(Component component, string indent) {
			var type = component.GetType().Name;
			Console.WriteLine("{0}{1}, type={2}", indent, (component == null ? "(null)" : type), type);
		}
	}
}
