using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Harmony;
using ShinkaiModel.Core;
using ShinkaiClient.Mono;
using ShinkaiClient.Patching;

namespace ShinkaiClient {
	public static class Main {
		private const string rootName = "shinkai";
		private static readonly HarmonyInstance harmony = HarmonyInstance.Create("com.shinkai");
		private static ShinkaiPatch[] patches = null;
		private static bool initialized;
		private static bool patched;

		public static void Execute() {
			if (initialized) {
				return;
			}
			initialized = true;

			Log.SetLevel(Log.LogLevel.ConsoleInfo | Log.LogLevel.ConsoleDebug | Log.LogLevel.InGameMessages);

			DevConsole.disableConsole = false;
			Application.runInBackground = true;

			HarmonyInstance.DEBUG = false;

			patches = Assembly.GetExecutingAssembly()
				.GetTypes()
				.Where(p => typeof(ShinkaiPatch).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract)
				.Select(Activator.CreateInstance)
				.Cast<ShinkaiPatch>()
				.ToArray();

			var rootObject = GameObject.Find(rootName);
			if (rootObject) {
				GameObject.Destroy(rootObject);
				rootObject = null;
			}

			rootObject = new GameObject();
			rootObject.name = rootName;
			rootObject.AddComponent<ShinkaiMod>();

			Log.Info("Mod loaded.");
		}

		public static void Patch() {
			if (patched) {
				return;
			}

			foreach (var patch in patches) {
				patch.Patch(harmony);
			}

			patched = true;
		}

		public static void Restore() {
			if (patched == false) {
				return;
			}

			foreach (var patch in patches) {
				patch.Restore();
			}

			patched = false;
		}
	}
}
