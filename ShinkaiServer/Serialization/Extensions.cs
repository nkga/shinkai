using UnityEngine;
using System.Runtime.Serialization;
using System.Collections;

namespace ShinkaiServer.Serialization {
	public class BaseFaceSerializationSurrogate : ISerializationSurrogate {
		public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context) {

			var tmp = (Base.Face)obj;
			info.AddValue("direction", tmp.direction);
			info.AddValue("cell", tmp.cell);
		}

		public System.Object SetObjectData(System.Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) {
			var tmp = (Base.Face)obj;
			tmp.direction = (Base.Direction)info.GetValue("direction", typeof(Base.Direction));
			tmp.cell = (Int3)info.GetValue("cell", typeof(Int3));
			obj = tmp;
			return obj;
		}
	}

	public class Int3SerializationSurrogate : ISerializationSurrogate {
		public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context) {

			Int3 tmp = (Int3)obj;
			info.AddValue("x", tmp.x);
			info.AddValue("y", tmp.y);
			info.AddValue("z", tmp.z);
		}

		public System.Object SetObjectData(System.Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) {
			Int3 tmp = (Int3)obj;
			tmp.x = (int)info.GetValue("x", typeof(int));
			tmp.y = (int)info.GetValue("y", typeof(int));
			tmp.z = (int)info.GetValue("z", typeof(int));
			obj = tmp;
			return obj;
		}
	}

	public class Vector3SerializationSurrogate : ISerializationSurrogate {
		public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context) {

			Vector3 tmp = (Vector3)obj;
			info.AddValue("x", tmp.x);
			info.AddValue("y", tmp.y);
			info.AddValue("z", tmp.z);
		}

		public System.Object SetObjectData(System.Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) {
			Vector3 tmp = (Vector3)obj;
			tmp.x = (float)info.GetValue("x", typeof(float));
			tmp.y = (float)info.GetValue("y", typeof(float));
			tmp.z = (float)info.GetValue("z", typeof(float));
			obj = tmp;
			return obj;
		}
	}

	public class QuaternionSerializationSurrogate : ISerializationSurrogate {
		public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context) {

			Quaternion tmp = (Quaternion)obj;
			info.AddValue("x", tmp.x);
			info.AddValue("y", tmp.y);
			info.AddValue("z", tmp.z);
			info.AddValue("w", tmp.w);
		}

		public System.Object SetObjectData(System.Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) {
			Quaternion tmp = (Quaternion)obj;
			tmp.x = (float)info.GetValue("x", typeof(float));
			tmp.y = (float)info.GetValue("y", typeof(float));
			tmp.z = (float)info.GetValue("z", typeof(float));
			tmp.w = (float)info.GetValue("w", typeof(float));
			obj = tmp;
			return obj;
		}
	}


	public class ColorSerializationSurrogate : ISerializationSurrogate {
		public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context) {

			Color tmp = (Color)obj;
			info.AddValue("r", tmp.r);
			info.AddValue("g", tmp.g);
			info.AddValue("b", tmp.b);
			info.AddValue("a", tmp.a);
		}

		public System.Object SetObjectData(System.Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) {
			Color tmp = (Color)obj;
			tmp.r = (float)info.GetValue("r", typeof(float));
			tmp.g = (float)info.GetValue("g", typeof(float));
			tmp.b = (float)info.GetValue("b", typeof(float));
			tmp.a = (float)info.GetValue("a", typeof(float));
			obj = tmp;
			return obj;
		}
	}
}
