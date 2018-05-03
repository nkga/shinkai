using UnityEngine;
using UnityEngine.UI;
using ShinkaiClient.Unity;

namespace ShinkaiClient.Mono {
	class MenuPanel : MonoBehaviour {
		public GameObject savedGamesRef;
		public GameObject multiButtonRef;

		private bool shouldFocus;
		private bool showJoinServer;
		private string usernameInput;
		private string passwordInput;
		private string hostnameInput;
		private Rect window = new Rect(Screen.width / 2 - 200, 200, 400, 125);

		GameObject multiplayerButton;
		Transform savedGameAreaContent;

		public void Awake() {
			multiplayerButton = savedGamesRef.transform.Find("Scroll View/Viewport/SavedGameAreaContent/NewGame").gameObject;
			savedGameAreaContent = multiButtonRef.transform.Find("Scroll View/Viewport/SavedGameAreaContent");
			CreateButton("Join Server", OnMenuButtonJoinClick);

			usernameInput = "Player";
			passwordInput = "";
			hostnameInput = "127.0.0.1";
		}

		private void CreateButton(string text, UnityEngine.Events.UnityAction clickEvent) {
			GameObject multiplayerButtonInst = Instantiate(multiplayerButton);
			multiplayerButtonInst.transform.Find("NewGameButton/Text").GetComponent<Text>().text = text;
			Button multiplayerButtonButton = multiplayerButtonInst.transform.Find("NewGameButton").GetComponent<Button>();
			multiplayerButtonButton.onClick = new Button.ButtonClickedEvent();
			multiplayerButtonButton.onClick.AddListener(clickEvent);
			multiplayerButtonInst.transform.SetParent(savedGameAreaContent, false);
		}

		private bool IsNameValid() {
			var len = usernameInput.Length;
			return len >= 4 && len <= 30; 
		}

		private void OnMenuButtonJoinClick() {
			HideWindows();
			showJoinServer = true;
			shouldFocus = true;
		}

		private void HideWindows() {
			showJoinServer = false;
			shouldFocus = true;
		}

		private void OnGUI() {
			if (showJoinServer) {
				window = GUILayout.Window(GUIUtility.GetControlID(FocusType.Keyboard), window, JoinServerWindow, "Join Server");
			}
		}

		private void OnJoinButtonClick() {
			HideWindows();
			Multiplayer.main.Connect(usernameInput, passwordInput, hostnameInput);
		}

		private void OnCancelButtonClick() {
			HideWindows();
			Multiplayer.main.Disconnect();
		}

		private void JoinServerWindow(int windowId) {
			Event e = Event.current;
			if (e.isKey) {
				switch (e.keyCode) {
					case KeyCode.Return:
					OnJoinButtonClick();
					break;
					case KeyCode.Escape:
					OnCancelButtonClick();
					break;
				}
			}

			GUISkinUtils.RenderWithSkin(GetGUISkin("menus.server.join", 80), () => {
				using (new GUILayout.VerticalScope("Box")) {
					using (new GUILayout.HorizontalScope()) {
						GUILayout.Label("Username:");
						GUI.SetNextControlName("usernameInput");
						usernameInput = GUILayout.TextField(usernameInput, 30);
					}

					using (new GUILayout.HorizontalScope()) {
						GUILayout.Label("Password:");
						GUI.SetNextControlName("passwordInput");
						passwordInput = GUILayout.TextField(passwordInput, 64);
					}

					using (new GUILayout.HorizontalScope()) {
						GUILayout.Label("Host:");
						GUI.SetNextControlName("hostnameInput");
						hostnameInput = GUILayout.TextField(hostnameInput, 120);
					}

					GUILayout.BeginHorizontal();

					if (GUILayout.Button("Join")) {
						OnJoinButtonClick();
					}

					if (GUILayout.Button("Cancel")) {
						OnCancelButtonClick();
					}

					GUILayout.EndHorizontal();
				}

				if (shouldFocus) {
					GUI.FocusControl("playerNameField");
					shouldFocus = false;
				}
			});
		}

		private GUISkin GetGUISkin(string skinName, int labelWidth) {
			return GUISkinUtils.RegisterDerivedOnce(skinName, s => {
				s.textField.fontSize = 14;
				s.textField.richText = false;
				s.textField.alignment = TextAnchor.MiddleLeft;
				s.textField.wordWrap = true;
				s.textField.stretchHeight = true;
				s.textField.padding = new RectOffset(10, 10, 5, 5);

				s.label.fontSize = 14;
				s.label.alignment = TextAnchor.MiddleRight;
				s.label.stretchHeight = true;
				s.label.fixedWidth = labelWidth;

				s.button.fontSize = 14;
				s.button.stretchHeight = true;
			});
		}
	}
}
