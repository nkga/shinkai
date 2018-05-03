using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShinkaiModel.Core;

namespace ShinkaiClient.Mono {
	public class PlayerChatInput : MonoBehaviour {
		private const int CHAR_LIMIT = 80;
		private const int INPUT_WIDTH = 300;
		private const int INPUT_HEIGHT = 25;
		private const int INPUT_MARGIN = 15;
		private const string GUI_CHAT_NAME = "ShinkaiChatInput";

		private PlayerChat manager;
		private float chatOpened = 0f;
		private bool chatEnabled = false;
		private string chatMessage = "";

		public void OnGUI() {
			if (chatEnabled == false) {
				return;
			}

			SetGUIStyle();
			GUI.SetNextControlName(GUI_CHAT_NAME);
			chatMessage = GUI.TextField(new Rect(INPUT_MARGIN, Screen.height - INPUT_HEIGHT - INPUT_MARGIN, INPUT_WIDTH, INPUT_HEIGHT), chatMessage, CHAR_LIMIT);
			GUI.FocusControl(GUI_CHAT_NAME);
			
			if (Event.current.isKey && Event.current.keyCode == KeyCode.Return) {
				SendMessage();
				Hide();
			}

			if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape) {
				Hide();
			}
		}

		private void SetGUIStyle() {
			GUI.skin.textField.fontSize = 12;
			GUI.skin.textField.richText = false;
			GUI.skin.textField.alignment = TextAnchor.MiddleLeft;
		}

		private void SendMessage() {
			if (manager != null && string.IsNullOrEmpty(chatMessage) == false) {
				Logic.Commands.SendChat(chatMessage);
				manager.WriteMessage(Multiplayer.main.self.username + ": " + chatMessage);
			}
		}

		public void Show() {
			if (chatEnabled == false) {
				chatOpened = Time.time;
				chatEnabled = true;
			}
		}

		public void Hide() {
			if (chatEnabled != false) {
				chatEnabled = false;
				chatMessage = "";
			}
		}

		public void SetManager(PlayerChat manager) {
			this.manager = manager;
		}
	}

	public class PlayerChat : MonoBehaviour {
		private const int LINE_LINE = 80;
		private const int MESSAGE_LIMIT = 7;
		private const float VISIBLE_TIMER = 7f;

		private float display;
		public GameObject chatEntry;
		private GUIText chatText;
		private List<string> messages;

		protected void Awake() {
			messages = new List<string>();
			chatEntry = new GameObject();
			chatEntry.transform.parent = this.transform;

			chatEntry.name = "ShinkaiChatEntry";
			chatText = chatEntry.AddComponent<GUIText>();
			chatEntry.AddComponent<GUITextShadow>();
			chatText.name = "ShinkaiChatText";
			chatText.text = "";
			chatText.alignment = TextAlignment.Left;
			chatText.fontSize = 18;
			chatText.transform.position = new Vector3(0.05f, .5f, 1f);
			chatText.enabled = false;
			display = 0f;

			DontDestroyOnLoad(chatEntry);
			DontDestroyOnLoad(this);
		}

		protected void Update() {
			if (Time.time - display <= VISIBLE_TIMER) {
				chatText.enabled = true;
			} else {
				chatText.enabled = false;
			}
		}

		public void WriteMessage(string message) {
			if (message == null) {
				return;
			}

			chatText.enabled = true;
			display = Time.time;

			AddChatMessage(SanitizeMessage(message));
			BuildChatText();
		}

		private void AddChatMessage(string sanitizedChatMessage) {
			if (messages.Count == MESSAGE_LIMIT) {
				messages.RemoveAt(0);
			}

			messages.Add(sanitizedChatMessage);
		}

		private void BuildChatText() {
			if (chatText == null) {
				Log.Warn("Chat text is null.");
				return;
			}

			chatText.text = "";

			foreach (var message in messages) {
				if (string.IsNullOrEmpty(message) == false) {
					if (chatText.text.Length > 0) {
						chatText.text += "\n";
					}

					chatText.text += message;
				}
			}
		}

		private string SanitizeMessage(string message) {
			message = message.Trim();

			if (message.Length < LINE_LINE) {
				return message;
			}

			return message.Substring(0, LINE_LINE);
		}
	}
}
