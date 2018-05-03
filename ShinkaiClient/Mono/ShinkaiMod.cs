using UnityEngine;

namespace ShinkaiClient.Mono {
	public class ShinkaiMod : MonoBehaviour {
		public void Awake() {
			DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<Multiplayer>();
			gameObject.AddComponent<SceneCleanerPreserve>();
			gameObject.AddComponent<MenuMods>();
		}
	}
}
