using System;
using System.Reflection;

namespace ShinkaiClient.Unity {
	public class CheatChange : IDisposable {
		static private FieldInfo TARGET_FIELD = typeof(GameModeUtils).GetField("currentCheats", BindingFlags.NonPublic | BindingFlags.Static);

		public GameModeOption backupCheats;

		public CheatChange(GameModeOption cheats) {
			backupCheats = GetCheats();
			SetCheats(backupCheats | cheats);
		}

		public void Dispose() {
			SetCheats(backupCheats);
		}

		private GameModeOption GetCheats() {
			return (GameModeOption)TARGET_FIELD.GetValue(null);
		}

		private void SetCheats(GameModeOption newCheats) {
			TARGET_FIELD.SetValue(null, newCheats);
		}
	}
}
