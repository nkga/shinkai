using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;
using ShinkaiServer.Serialization;
using UnityEngine;

namespace ShinkaiServer.Logic {
	[Serializable]
	public class State {
		public GameTime gameTime;
		public ServerJoinInfo serverInfo;
		public ServerTimeUpdate timeUpdate;
		public History history = new History();

		public State() {
			gameTime = new GameTime();
			history = new History();

			var random = new System.Random();

			serverInfo = new ServerJoinInfo();
			serverInfo.seed = random.Next();
			serverInfo.spawnPosition = new UnityEngine.Vector3(6.6f, 1.9f, 89.6f);
			serverInfo.globalRootGuid = GetGuid();
			serverInfo.escapePodGuid = GetGuid();
			serverInfo.fabricatorGuid = GetGuid();
			serverInfo.medkitGuid = GetGuid();
			serverInfo.radioGuid = GetGuid();

			timeUpdate = new ServerTimeUpdate();
			timeUpdate.timestamp = gameTime.GetCurrentTime();
		}

		static public State Load(string filename) {
			var result = new State();

			if (File.Exists(filename) == false) {
				return result;
			}

			Log.Info("Loading server data.");

			try {
				using (var stream = File.Open(filename, FileMode.Open)) {
					var formatter = GetFormatter();
					result = (State)formatter.Deserialize(stream);
				}
			} catch (Exception exception) {
				Log.Warn("Couldn't load server save data: " + exception);
			}

			return result;
		}

		static public void Save(string filename, State state) {
			Log.Info("Saving server data.");

			try {
				using (var stream = File.Open(filename, FileMode.Create)) {
					var formatter = GetFormatter();
					formatter.Serialize(stream, state);
				}
			} catch (Exception exception) {
				Log.Warn("Couldn't save server save data: " + exception);
			}
		}

		static private BinaryFormatter GetFormatter() {
			var formatter = new BinaryFormatter();

			var surrogate = new SurrogateSelector();
			var ctx = new StreamingContext(StreamingContextStates.All);

			surrogate.AddSurrogate(typeof(Int3), ctx, new Int3SerializationSurrogate());
			surrogate.AddSurrogate(typeof(Vector3), ctx, new Vector3SerializationSurrogate());
			surrogate.AddSurrogate(typeof(Quaternion), ctx, new QuaternionSerializationSurrogate());
			surrogate.AddSurrogate(typeof(Base.Face), ctx, new BaseFaceSerializationSurrogate());
			surrogate.AddSurrogate(typeof(Color), ctx, new ColorSerializationSurrogate());

			formatter.SurrogateSelector = surrogate;

			return formatter;
		}

		static public string GetGuid() {
			return Guid.NewGuid().ToString();
		}
	}
}
