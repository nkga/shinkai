using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShinkaiClient.Mono {
	class MenuMods : MonoBehaviour {
		private void OnEnable() {
			SceneManager.sceneLoaded += OnSceneLoad;
		}

		private void OnDisable() {
			SceneManager.sceneLoaded -= OnSceneLoad;
		}

		private void OnSceneLoad(Scene scene, LoadSceneMode loadMode) {
			if (scene.name == "XMenu") {
				EnableMods();

				if (Multiplayer.main != null) {
					Multiplayer.main.Disconnect();
				}

				Main.Restore();
			}
		}

		private void EnableMods() {
			GameObject startButton = GameObject.Find("Menu canvas/Panel/MainMenu/PrimaryOptions/MenuButtons/ButtonPlay");
			GameObject showLoadedMultiplayer = Instantiate(startButton);
			Text buttonText = showLoadedMultiplayer.transform.Find("Circle/Bar/Text").gameObject.GetComponent<Text>();
			buttonText.text = "Multiplayer";
			showLoadedMultiplayer.transform.SetParent(GameObject.Find("Menu canvas/Panel/MainMenu/PrimaryOptions/MenuButtons").transform, false);
			showLoadedMultiplayer.transform.SetSiblingIndex(3);
			Button showLoadedMultiplayerButton = showLoadedMultiplayer.GetComponent<Button>();
			showLoadedMultiplayerButton.onClick.RemoveAllListeners();
			showLoadedMultiplayerButton.onClick.AddListener(ShowMultiplayerMenu);

			MainMenuRightSide rightSide = MainMenuRightSide.main;
			GameObject savedGamesRef = FindObject(rightSide.gameObject, "SavedGames");
			GameObject multiButtonRef = Instantiate(savedGamesRef);
			multiButtonRef.name = "Multiplayer";
			multiButtonRef.transform.Find("Header").GetComponent<Text>().text = "Multiplayer";
			Destroy(multiButtonRef.transform.Find("Scroll View/Viewport/SavedGameAreaContent/NewGame").gameObject);

			MenuPanel panel = multiButtonRef.AddComponent<MenuPanel>();
			panel.savedGamesRef = savedGamesRef;
			panel.multiButtonRef = multiButtonRef;

			Destroy(multiButtonRef.GetComponent<MainMenuLoadPanel>());
			multiButtonRef.transform.SetParent(rightSide.transform, false);
			rightSide.groups.Add(multiButtonRef);
		}

		private void ShowMultiplayerMenu() {
			MainMenuRightSide rightSide = MainMenuRightSide.main;
			rightSide.OpenGroup("Multiplayer");
		}

		private GameObject FindObject(GameObject parent, string name) {
			Component[] trs = parent.GetComponentsInChildren(typeof(Transform), true);
			foreach (Component t in trs) {
				if (t.name == name) {
					return t.gameObject;
				}
			}
			return null;
		}
	}
}
