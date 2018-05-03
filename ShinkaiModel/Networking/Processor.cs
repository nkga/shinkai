using System;
using System.Collections.Generic;
using ShinkaiModel.Core;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ShinkaiModel.Networking {
	public sealed class Processor {
		public delegate void Callback(NetPeer peer, NetDataReader reader);

		private char[] hashBuffer;
		private Dictionary<string, uint> hashCache;
		private Dictionary<uint, Callback> callbacks;
		private NetDataWriter writer;
		private readonly Serializer serializer;

		public Processor(Serializer serializer) {
			hashBuffer = new char[1024];
			hashCache = new Dictionary<string, uint>();
			callbacks = new Dictionary<uint, Callback>();
			writer = new NetDataWriter();
			this.serializer = serializer;
		}

		public void Add(Type type, Callback callback) {
			uint hash = GetHash(type);

			if (callbacks.ContainsKey(hash)) {
				Log.Warn("Callback already exists for: " + type.FullName);
			} else {
				callbacks.Add(hash, callback);
			}
		}

		public void Clear() {
			callbacks.Clear();
		}
		
		public bool Has(Type type) {
			return callbacks.ContainsKey(GetHash(type));
		}

		public bool Read(NetPeer peer, NetDataReader reader) {
			if (reader.AvailableBytes < 4) {
				return false;
			}

			var hash = reader.GetUInt();

			Callback callback;
			if (callbacks.TryGetValue(hash, out callback)) {
				callback(peer, reader);
			}

			return true;
		}

		public void ReadAll(NetPeer peer, NetDataReader reader) {
			while (reader.AvailableBytes >= 4) {
				var hash = reader.GetUInt();

				Callback callback;
				if (callbacks.TryGetValue(hash, out callback)) {
					callback(peer, reader);
				}
			}
		}

		public void Serialize(Message message) {
			writer.Reset();
			writer.Put((uint)GetHash(message.GetType()));
			serializer.Serialize(writer, message);
		}

		public void Send(NetPeer peer, DeliveryMethod method) {
			if (peer != null) {
				peer.Send(writer, method);
			}
		}

		public uint GetHash(Type type) {
			uint hash;
			string typeName = type.FullName;

			if (hashCache.TryGetValue(typeName, out hash)) {
				return hash;
			}

			hash = 2166136261u;
			typeName.CopyTo(0, hashBuffer, 0, typeName.Length);

			for (var i = 0; i < typeName.Length; i++) {
				hash = hash ^ hashBuffer[i];
				hash *= 16777619u;
			}

			hashCache.Add(typeName, hash);
			return hash;
		}
	}
}
