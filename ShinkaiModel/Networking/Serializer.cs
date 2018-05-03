using System;
using System.Reflection;
using System.Collections.Generic;
using LiteNetLib.Utils;
using ShinkaiModel.Core;

namespace ShinkaiModel.Networking {
	public sealed class Serializer {
		private sealed class NestedType {
			public readonly NestedTypeWriter WriteDelegate;
			public readonly NestedTypeReader ReadDelegate;

			public NestedType(NestedTypeWriter writeDelegate, NestedTypeReader readDelegate) {
				WriteDelegate = writeDelegate;
				ReadDelegate = readDelegate;
			}
		}

		private delegate void NestedTypeWriter(NetDataWriter writer, object customObj);
		private delegate object NestedTypeReader(NetDataReader reader);

		private sealed class StructInfo {
			public readonly Action<NetDataWriter>[] WriteDelegate;
			public readonly Action<NetDataReader>[] ReadDelegate;
			public object Reference;
			private readonly int _membersCount;

			public StructInfo(int membersCount) {
				_membersCount = membersCount;
				WriteDelegate = new Action<NetDataWriter>[membersCount];
				ReadDelegate = new Action<NetDataReader>[membersCount];
			}

			public void Write(NetDataWriter writer, object obj) {
				Reference = obj;
				for (int i = 0; i < _membersCount; i++) {
					WriteDelegate[i](writer);
				}
			}

			public void Read(NetDataReader reader) {
				for (int i = 0; i < _membersCount; i++) {
					ReadDelegate[i](reader);
				}
			}
		}

		private static readonly HashSet<Type> BasicTypes = new HashSet<Type>
		{
			typeof(int),
			typeof(uint),
			typeof(byte),
			typeof(sbyte),
			typeof(short),
			typeof(ushort),
			typeof(long),
			typeof(ulong),
			typeof(string),
			typeof(float),
			typeof(double),
			typeof(bool)
		};

		private readonly NetDataWriter _writer;
		private readonly int _maxStringLength;
		private readonly Dictionary<string, StructInfo> _registeredTypes;
		private readonly Dictionary<Type, NestedType> _registeredNestedTypes;

		public Serializer() {
			_maxStringLength = 0;
			_registeredTypes = new Dictionary<string, StructInfo>();
			_registeredNestedTypes = new Dictionary<Type, NestedType>();
			_writer = new NetDataWriter();
		}

		private bool RegisterNestedTypeInternal<T>(Func<T> constructor) where T : INetSerializable {
			var t = typeof(T);
			if (_registeredNestedTypes.ContainsKey(t)) {
				return false;
			}

			var rwDelegates = new NestedType(
				(writer, obj) => {
					((T)obj).Serialize(writer);
				},
				reader => {
					var instance = constructor();
					instance.Deserialize(reader);
					return instance;
				});
			_registeredNestedTypes.Add(t, rwDelegates);
			return true;
		}

		/// <summary>
		/// Register nested property type
		/// </summary>
		/// <typeparam name="T">INetSerializable structure</typeparam>
		/// <returns>True - if register successful, false - if type already registered</returns>
		public bool RegisterNestedType<T>() where T : struct, INetSerializable {
			return RegisterNestedTypeInternal(() => new T());
		}

		/// <summary>
		/// Register nested property type
		/// </summary>
		/// <typeparam name="T">INetSerializable class</typeparam>
		/// <returns>True - if register successful, false - if type already registered</returns>
		public bool RegisterNestedType<T>(Func<T> constructor) where T : class, INetSerializable {
			return RegisterNestedTypeInternal(constructor);
		}

		/// <summary>
		/// Register nested property type
		/// </summary>
		/// <param name="writeDelegate"></param>
		/// <param name="readDelegate"></param>
		/// <returns>True - if register successful, false - if type already registered</returns>
		public bool RegisterNestedType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate) {
			var t = typeof(T);
			if (BasicTypes.Contains(t) || _registeredNestedTypes.ContainsKey(t)) {
				return false;
			}

			var rwDelegates = new NestedType(
				(writer, obj) => writeDelegate(writer, (T)obj),
				reader => readDelegate(reader));

			_registeredNestedTypes.Add(t, rwDelegates);
			return true;
		}

		private static Delegate CreateDelegate(Type type, MethodInfo info) {
			return Delegate.CreateDelegate(type, info);

		}
		private StructInfo RegisterInternal(Type t) {
			string typeName = t.FullName;
			StructInfo info;
			if (_registeredTypes.TryGetValue(typeName, out info)) {
				return info;
			}

			var props = t.GetFields(BindingFlags.Instance | BindingFlags.Public);

			int propsCount = props.Length;

			info = new StructInfo(propsCount);
			for (int i = 0; i < propsCount; i++) {
				var property = props[i];
				var propertyType = property.FieldType;

				bool isEnum = propertyType.IsEnum;

				if (isEnum) {
					var underlyingType = Enum.GetUnderlyingType(propertyType);
					if (underlyingType == typeof(byte)) {
						info.ReadDelegate[i] = reader => {
							property.SetValue(info.Reference, Enum.ToObject(propertyType, reader.GetByte()));
						};
						info.WriteDelegate[i] = writer => {
							writer.Put((byte)property.GetValue(info.Reference));
						};
					} else if (underlyingType == typeof(int)) {
						info.ReadDelegate[i] = reader => {
							property.SetValue(info.Reference, Enum.ToObject(propertyType, reader.GetInt()));
						};
						info.WriteDelegate[i] = writer => {
							writer.Put((int)property.GetValue(info.Reference));
						};
					} else {
						throw new InvalidTypeException("Not supported enum underlying type: " + underlyingType.Name);
					}
				} else if (propertyType == typeof(string)) {
					if (_maxStringLength <= 0) {
						info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetString());
						info.WriteDelegate[i] = writer => writer.Put((string)property.GetValue(info.Reference));
					} else {
						info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetString(_maxStringLength));
						info.WriteDelegate[i] = writer => writer.Put((string)property.GetValue(info.Reference), _maxStringLength);
					}
				} else if (propertyType == typeof(bool)) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetBool());
					info.WriteDelegate[i] = writer => writer.Put((bool)property.GetValue(info.Reference));
				} else if (propertyType == typeof(byte)) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetByte());
					info.WriteDelegate[i] = writer => writer.Put((byte)property.GetValue(info.Reference));
				} else if (propertyType == typeof(sbyte)) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetSByte());
					info.WriteDelegate[i] = writer => writer.Put((sbyte)property.GetValue(info.Reference));
				} else if (propertyType == typeof(short)) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetShort());
					info.WriteDelegate[i] = writer => writer.Put((short)property.GetValue(info.Reference));
				} else if (propertyType == typeof(ushort)) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetUShort());
					info.WriteDelegate[i] = writer => writer.Put((ushort)property.GetValue(info.Reference));
				} else if (propertyType == typeof(int)) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetInt());
					info.WriteDelegate[i] = writer => writer.Put((int)property.GetValue(info.Reference));
				} else if (propertyType == typeof(uint)) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetUInt());
					info.WriteDelegate[i] = writer => writer.Put((uint)property.GetValue(info.Reference));
				} else if (propertyType == typeof(long)) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetLong());
					info.WriteDelegate[i] = writer => writer.Put((long)property.GetValue(info.Reference));
				} else if (propertyType == typeof(ulong)) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetULong());
					info.WriteDelegate[i] = writer => writer.Put((ulong)property.GetValue(info.Reference));
				} else if (propertyType == typeof(float)) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetFloat());
					info.WriteDelegate[i] = writer => writer.Put((float)property.GetValue(info.Reference));
				} else if (propertyType == typeof(double)) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetDouble());
					info.WriteDelegate[i] = writer => writer.Put((double)property.GetValue(info.Reference));
				} else if (propertyType == typeof(string[])) {
					if (_maxStringLength <= 0) {
						info.ReadDelegate[i] =
							reader => property.SetValue(info.Reference, reader.GetStringArray());
						info.WriteDelegate[i] =
							writer => writer.PutArray((string[])property.GetValue(info.Reference));
					} else {
						info.ReadDelegate[i] =
							reader => property.SetValue(info.Reference, reader.GetStringArray(_maxStringLength));
						info.WriteDelegate[i] =
							writer => writer.PutArray((string[])property.GetValue(info.Reference), _maxStringLength);
					}
				} else if (propertyType == typeof(byte[])) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetBytesWithLength());
					info.WriteDelegate[i] = writer => writer.PutBytesWithLength((byte[])property.GetValue(info.Reference));
				} else if (propertyType == typeof(short[])) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetShortArray());
					info.WriteDelegate[i] = writer => writer.PutArray((short[])property.GetValue(info.Reference));
				} else if (propertyType == typeof(ushort[])) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetUShortArray());
					info.WriteDelegate[i] = writer => writer.PutArray((ushort[])property.GetValue(info.Reference));
				} else if (propertyType == typeof(int[])) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetIntArray());
					info.WriteDelegate[i] = writer => writer.PutArray((int[])property.GetValue(info.Reference));
				} else if (propertyType == typeof(uint[])) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetUIntArray());
					info.WriteDelegate[i] = writer => writer.PutArray((uint[])property.GetValue(info.Reference));
				} else if (propertyType == typeof(long[])) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetLongArray());
					info.WriteDelegate[i] = writer => writer.PutArray((long[])property.GetValue(info.Reference));
				} else if (propertyType == typeof(ulong[])) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetULongArray());
					info.WriteDelegate[i] = writer => writer.PutArray((ulong[])property.GetValue(info.Reference));
				} else if (propertyType == typeof(float[])) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetFloatArray());
					info.WriteDelegate[i] = writer => writer.PutArray((float[])property.GetValue(info.Reference));
				} else if (propertyType == typeof(double[])) {
					info.ReadDelegate[i] = reader => property.SetValue(info.Reference, reader.GetDoubleArray());
					info.WriteDelegate[i] = writer => writer.PutArray((double[])property.GetValue(info.Reference));
				} else {
					NestedType registeredNestedType;
					bool array = false;

					if (propertyType.IsArray) {
						array = true;
						propertyType = propertyType.GetElementType();
					}

					if (_registeredNestedTypes.TryGetValue(propertyType, out registeredNestedType)) {
						if (array) {
							// Array type serialize/deserialize
							info.ReadDelegate[i] = reader => {
								ushort arrLength = reader.GetUShort();
								Array arr = Array.CreateInstance(propertyType, arrLength);
								for (int k = 0; k < arrLength; k++) {
									arr.SetValue(registeredNestedType.ReadDelegate(reader), k);
								}

								property.SetValue(info.Reference, arr);
							};

							info.WriteDelegate[i] = writer => {
								Array arr = (Array)property.GetValue(info.Reference);
								writer.Put((ushort)arr.Length);
								for (int k = 0; k < arr.Length; k++) {
									registeredNestedType.WriteDelegate(writer, arr.GetValue(k));
								}
							};
						} else {
							// Simple
							info.ReadDelegate[i] = reader => {
								property.SetValue(info.Reference, registeredNestedType.ReadDelegate(reader));
							};

							info.WriteDelegate[i] = writer => {
								registeredNestedType.WriteDelegate(writer, property.GetValue(info.Reference));
							};
						}
					} else {
						// var extInfo = RegisterInternal(propertyType);
						// info.ReadDelegate[i] = reader => { extInfo.Read(reader); };
						// info.WriteDelegate[i] = writer => { extInfo.Write(writer, property.GetValue(info.Reference)); };
						throw new InvalidTypeException("Unknown property type: " + propertyType.FullName);
					}
				}
			}
			_registeredTypes.Add(typeName, info);

			return info;
		}

		public void Register<T>() {
			RegisterInternal(typeof(T));
		}

		public bool Deserialize(NetDataReader reader, object target) {
			var info = RegisterInternal(target.GetType());
			info.Reference = target;
			try {
				info.Read(reader);
			} catch {
				return false;
			}
			return true;
		}

		public void Serialize(NetDataWriter writer, object obj) {
			RegisterInternal(obj.GetType()).Write(writer, obj);
		}
	}
}
