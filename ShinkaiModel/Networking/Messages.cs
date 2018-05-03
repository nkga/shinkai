using System;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ShinkaiModel.Networking {
	[Serializable]
	public struct ChildGuid {
		public string guid;
		public string path;

		public override string ToString() {
			return "ChildGuid(guid=" + guid + ", path=" + path + ")";
		}
	}

	[Serializable]
	public abstract class Message {
		public override string ToString() {
			var type = this.GetType();

			var sb = new System.Text.StringBuilder();
			sb.AppendFormat(type.Name);

			foreach (var prop in this.GetType().GetFields()) {
				sb.Append(" " + prop.Name + "=" + prop.GetValue(this));
			}

			return sb.ToString();
		}
	}

	[Serializable]
	public class ServerJoinRequest : Message {
		public string username;
		public string password;
	}

	[Serializable]
	public class ServerJoinInfo : Message {
		public double timestamp;
		public int id;
		public int seed;
		public Vector3 spawnPosition;
		public string globalRootGuid;
		public string escapePodGuid;
		public string fabricatorGuid;
		public string medkitGuid;
		public string radioGuid;
		public string inventoryGuid;
		public string equipmentGuid;
	}

	[Serializable]
	public class ServerJoinReject : Message {
		public string reason;
	}

	[Serializable]
	public class ServerSaveRequest : Message {
	}

	[Serializable]
	public class ServerSyncRequest : Message {
	}
	
	public class ServerSyncFinish : Message {
	}

	[Serializable]
	public class ServerTimeUpdate : Message {
		public double timestamp;
	}

	[Serializable]
	public class ClientGameReload : Message {
	}

	[Serializable]
	public class ClientPlayerJoin : Message {
		public int id;
		public string username;
		public string inventoryGuid;
	}

	[Serializable]
	public class ClientPlayerLeave : Message {
		public int id;
	}

	[Serializable]
	public class ClientPlayerVitals : Message {
		public int id;
		public float health;
		public float oxygen;
		public float food;
		public float water;
	}

	[Serializable]
	public class ClientPlayerDeath : Message {
		public int id;
	}

		[Serializable]
	public class ClientPlayerMovement : Message {
		public double timestamp;
		public int id;
		public string subGuid;
		public Vector3 position;
		public Vector3 subPosition;
		public Vector3 velocity;
		public Quaternion bodyRotation;
		public Quaternion lookRotation;
		public Player.Mode mode;
		public Player.MotorMode motorMode;
		public TechType handTool;
		public string handGuid;
		public bool underwater;
		public bool falling;
		public bool usingPda;
		public bool usingTool;
	}

	[Serializable]
	public class ClientCreatureUpdate : Message {
		public double timestamp;
		public string creatureGuid;
		public TechType tech;
		public Vector3 position;
		public Quaternion rotation;
		public int actionIndex;
		public Vector3 leashPosition;
	}

	[Serializable]
	public class ClientVehicleMovement : Message {
		public double timestamp;
		public int id;
		public string vehicleGuid;
		public TechType vehicleTech;
		public Vector3 position;
		public Vector3 velocity;
		public Vector3 angularVelocity;
		public Quaternion rotation;
		public float steeringWheelYaw;
		public float steeringWheelPitch;
		public float thrust;
		public bool throttle;
		public bool grounded;
		public bool useLeft;
		public bool useRight;
	}

	[Serializable]
	public class ClientVehicleColorChange : Message {
		public string vehicleGuid;
		public int index;
		public Vector3 hsb;
		public Color color;
	}

	[Serializable]
	public class ClientVehicleNameChange : Message {
		public string vehicleGuid;
		public string name;
	}

	[Serializable]
	public class ClientVehicleDocking : Message {
		public int id;
		public string vehicleGuid;
		public string bayGuid;
		public string subGuid;
		public Vector3 bayPosition;
		public bool docked;
	}

	[Serializable]
	public class ClientVehicleKill : Message {
		public string vehicleGuid;
	}

	[Serializable]
	public class ClientLiveMixinChange : Message {
		public string targetGuid;
		public float health;
		public float amount;
		public DamageType type;
		public Vector3 position;
		public bool force;
	}

	[Serializable]
	public class ClientBuildConstruct : Message {
		public string objectGuid;
		public string targetGuid;
		public string subGuid;
		public string ghostGuid;
		public TechType tech;
		public bool furniture;
		public Vector3 objectPosition;
		public Quaternion objectRotation;
		public Vector3 cameraPosition;
		public Quaternion cameraRotation;
		public float additiveRotation;
		public int direction;
		public bool hasFace;
		public Base.Face face;
		public Int3 anchor;
	}

	[Serializable]
	public class ClientBuildConstructChange : Message {
		public string objectGuid;
		public string baseGuid;
		public string moduleGuid;
		public TechType tech;
		public Vector3 objectPosition;
		public float amount;
		public bool state;
		public bool constructing;
	}

	[Serializable]
	public class ClientBuildDeconstructBase : Message {
		public string deconstructableGuid;
		public string constructableGuid;
		public string baseGuid;
		public TechType tech;
		public Vector3 position;
		public Int3 boundsMin;
		public Int3 boundsMax;
		public Base.FaceType faceType;
		public Base.Direction faceDirection;
		public Int3 faceCell;
	}

	[Serializable]
	public class ClientContainerAddItem : Message {
		public string ownerGuid;
		public string itemGuid;
		public TechType tech;
		public Vector3 position;
		public byte[] data;
	}

	[Serializable]
	public class ClientContainerRemoveItem : Message {
		public string ownerGuid;
		public string itemGuid;
		public TechType tech;
		public Vector3 position;
	}

	[Serializable]
	public class ClientEquipmentAddItem : Message {
		public string ownerGuid;
		public string itemGuid;
		public string slot;
		public Vector3 position;
		public byte[] data;
	}

	[Serializable]
	public class ClientEquipmentRemoveItem : Message {
		public string ownerGuid;
		public string itemGuid;
		public string slot;
		public Vector3 position;
	}

	[Serializable]
	public class ClientItemDropped : Message {
		public string itemGuid;
		public string waterParkGuid;
		public TechType tech;
		public Vector3 position;
		public byte[] data;
	}

	[Serializable]
	public class ClientItemGrabbed : Message {
		public string itemGuid;
		public TechType tech;
		public Vector3 position;
	}

	[Serializable]
	public class ClientResourceBreak : Message {
		public float time;
		public TechType tech;
		public Vector3 position;
	}

	[Serializable]
	public class ClientFabricatorStart : Message {
		public string fabricatorGuid;
		public TechType tech;
		public float duration;
	}

	[Serializable]
	public class ClientFabricatorPickup : Message {
		public string fabricatorGuid;
		public TechType tech;
	}

	[Serializable]
	public class ClientConstructorCraft : Message {
		public double timestamp;
		public string constructorGuid;
		public string itemGuid;
		public TechType tech;
		public float duration;
		public Vector3 spawnPosition;
		public Quaternion spawnRotation;
		public ChildGuid[] children;
	}

	[Serializable]
	public class ClientOpenableStateChanged : Message {
		public string objectGuid;
		public bool state;
		public float duration;
	}

	[Serializable]
	public class ClientItemLabel : Message {
		public string targetGuid;
		public string label;
	}

	[Serializable]
	public class ClientObjectUpdate : Message {
		public double timestamp;
		public string targetGuid;
		public Vector3 position;
		public Quaternion rotation;
	}

	[Serializable]
	public class ClientScanEncyclopedia : Message {
		public string key;
	}

	[Serializable]
	public class ClientScanKnownTech : Message {
		public TechType tech;
	}

	[Serializable]
	public class ClientScanProgress : Message {
		public TechType tech;
		public int progress;
	}

	[Serializable]
	public class ClientStoryGoal : Message {
		public double timestamp;
		public string key;
		public Story.GoalType goal;
	}

	[Serializable]
	public class ClientCommandSpawn : Message {
		public string objectGuid;
		public TechType tech;
		public Vector3 spawnPosition;
		public Quaternion spawnRotation;
		public ChildGuid[] children;
	}

	[Serializable]
	public class ClientCommandChat : Message {
		public int id;
		public string text;
	}

	[Serializable]
	public class ClientToggleLight : Message {
		public string objectGuid;
		public bool state;
	}

	[Serializable]
	public class ClientPowerChange : Message {
		public string targetGuid;
		public float total;
		public bool force;
	}

	[Serializable]
	public class ClientCyclopsHorn : Message {
		public string vehicleGuid;
	}

	[Serializable]
	public class ClientCyclopsState : Message {
		public string vehicleGuid;
		public CyclopsMotorMode.CyclopsMotorModes motorMode;
		public bool engineOn;
		public bool engineStarting;
		public bool shield;
		public bool floodLights;
		public bool internalLights;
		public bool silentRunning;
	}

	public static class MessageExtensions {
		public static Base.Face GetBaseFace(this NetDataReader reader) {
			return new Base.Face {
				direction = (Base.Direction)reader.GetByte(),
				cell = reader.GetInt3()
			};
		}

		public static void Put(this NetDataWriter writer, Base.Face face) {
			writer.Put((byte)face.direction);
			writer.Put(face.cell);
		}

		public static Color GetColor(this NetDataReader reader) {
			return new Color {
				r = reader.GetFloat(),
				g = reader.GetFloat(),
				b = reader.GetFloat(),
				a = reader.GetFloat()
			};
		}

		public static void Put(this NetDataWriter writer, Color obj) {
			writer.Put(obj.r);
			writer.Put(obj.g);
			writer.Put(obj.b);
			writer.Put(obj.a);
		}

		public static Int3 GetInt3(this NetDataReader reader) {
			return new Int3 {
				x = reader.GetInt(),
				y = reader.GetInt(),
				z = reader.GetInt()
			};
		}

		public static void Put(this NetDataWriter writer, Int3 vec) {
			writer.Put(vec.x);
			writer.Put(vec.y);
			writer.Put(vec.z);
		}

		public static Quaternion GetQuaternion(this NetDataReader reader) {
			return new Quaternion {
				w = reader.GetFloat(),
				x = reader.GetFloat(),
				y = reader.GetFloat(),
				z = reader.GetFloat()
			};
		}

		public static void Put(this NetDataWriter writer, Quaternion quat) {
			writer.Put(quat.w);
			writer.Put(quat.x);
			writer.Put(quat.y);
			writer.Put(quat.z);
		}

		public static Vector3 GetVector3(this NetDataReader reader) {
			return new Vector3 {
				x = reader.GetFloat(),
				y = reader.GetFloat(),
				z = reader.GetFloat()
			};
		}

		public static void Put(this NetDataWriter writer, Vector3 vec) {
			writer.Put(vec.x);
			writer.Put(vec.y);
			writer.Put(vec.z);
		}

		public static ChildGuid GetChildGuid(this NetDataReader reader) {
			return new ChildGuid {
				guid = reader.GetString(),
				path = reader.GetString()
			};
		}

		public static void Put(this NetDataWriter writer, ChildGuid child) {
			writer.Put(child.guid);
			writer.Put(child.path);
		}

		public static void Setup(Serializer serializer) {
			serializer.RegisterNestedType(Put, GetQuaternion);
			serializer.RegisterNestedType(Put, GetVector3);
			serializer.RegisterNestedType(Put, GetInt3);
			serializer.RegisterNestedType(Put, GetColor);
			serializer.RegisterNestedType(Put, GetChildGuid);
			serializer.RegisterNestedType(Put, GetBaseFace);
		}
	}
}
