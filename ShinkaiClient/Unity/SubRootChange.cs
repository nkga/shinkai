using System;
using System.Reflection;
using UnityEngine;

namespace ShinkaiClient.Unity {
	public class SubRootChange : IDisposable {
		static private FieldInfo TARGET_FIELD = typeof(Player).GetField("_currentSub", BindingFlags.NonPublic | BindingFlags.Instance);

		public readonly SubRoot backupSubRoot;

		public SubRootChange(SubRoot subRoot) {
			if (Player.main != null) {
				backupSubRoot = Player.main.GetCurrentSub();
				TARGET_FIELD.SetValue(Player.main, subRoot);
			}
		}

		public void Dispose() {
			if (Player.main != null) {
				TARGET_FIELD.SetValue(Player.main, backupSubRoot);
			}
		}
	}
}
