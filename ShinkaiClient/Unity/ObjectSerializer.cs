using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ShinkaiClient.Unity {
	public static class ObjectSerializer {
		public static byte[] GetBytes(GameObject gameObject, bool removeParent = false) {
			byte[] result = null;

			if (gameObject != null) {
				var parent = gameObject.transform.parent;

				if (removeParent) {
					gameObject.transform.parent = null;
				}

				try {
					using (var stream = new MemoryStream()) {
						using (var proxy = ProtobufSerializerPool.GetProxy()) {
							proxy.Value.SerializeObjectTree(stream, gameObject);
							result = stream.ToArray();
						}
					}
				} finally {
					if (removeParent) {
						gameObject.transform.parent = parent;
					}
				}
			}

			return result;
		}

		public static GameObject GetGameObject(byte[] bytes) {
			GameObject result = null;

			if (bytes != null) {
				try {
					using (var stream = new MemoryStream(bytes)) {
						using (var proxy = ProtobufSerializerPool.GetProxy()) {
							result = proxy.Value.DeserializeObjectTree(stream, 0);
						}
					}
				} catch (Exception exception) {
					UnityEngine.Debug.Log(exception);
				}
			}

			return result;
		}
	}
}
