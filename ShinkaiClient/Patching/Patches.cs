using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;
using UWE;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;
using ShinkaiClient.Mono;
using ShinkaiClient.Logic;
using ShinkaiClient.Unity;

namespace ShinkaiClient.Patching {
	// Disables in game time pausing.
	public class FreezeTime_Begin_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(FreezeTime);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Begin", BindingFlags.Public | BindingFlags.Static);

		public static bool Prefix() {
			return false;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Disconnects from multiplayer on quit.
	public class IngameMenu_QuitGame_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(IngameMenu);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("QuitGame", BindingFlags.Public | BindingFlags.Instance);

		public static void Prefix() {
			if (Multiplayer.main != null) {
				Multiplayer.main.Disconnect();
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Prevents parts of the intro from playing.
	public class uGUI_SceneIntro_Play_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(uGUI_SceneIntro);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Play", BindingFlags.Public | BindingFlags.Instance);

		public static bool Prefix(uGUI_SceneIntro __instance) {
			return false;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Prevents hatch cinematics from occurring.
	public class EscapePodFirstUseCinematicsController_Initialize_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(EscapePodFirstUseCinematicsController);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Prefix(EscapePodFirstUseCinematicsController __instance) {
			EscapePod.main.bottomHatchUsed = true;
			EscapePod.main.topHatchUsed = true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Stores the last built module.
	public class Base_SpawnModule_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Base);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SpawnModule", BindingFlags.Public | BindingFlags.Instance);

		public static void Postfix(Base __instance, GameObject __result) {
			Logic.Building.SetNewModule(__result);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	// Notification for placing base pieces or interior pieces.
	public class Builder_TryPlace_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Builder);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("TryPlace");

		public static readonly OpCode PLACE_BASE_INJECTION_OPCODE = OpCodes.Callvirt;
		public static readonly object PLACE_BASE_INJECTION_OPERAND = typeof(BaseGhost).GetMethod("Place");

		public static readonly OpCode PLACE_FURNITURE_INJECTION_OPCODE = OpCodes.Call;
		public static readonly object PLACE_FURNITURE_INJECTION_OPERAND = typeof(SkyEnvironmentChanged).GetMethod("Send", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(GameObject), typeof(Component) }, null);

		public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions) {
			foreach (CodeInstruction instruction in instructions) {
				yield return instruction;

				if (instruction.opcode.Equals(PLACE_BASE_INJECTION_OPCODE) && instruction.operand.Equals(PLACE_BASE_INJECTION_OPERAND)) {
					// Building.BuildPlaceBase(componentInParent, component, CraftData.GetTechType(Builder.prefab));
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Ldloc_1);
					yield return new CodeInstruction(OpCodes.Ldsfld, TARGET_CLASS.GetField("prefab", BindingFlags.Static | BindingFlags.NonPublic));
					yield return new CodeInstruction(OpCodes.Call, typeof(CraftData).GetMethod("GetTechType", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(GameObject) }, null));
					yield return new CodeInstruction(OpCodes.Call, typeof(Building).GetMethod("PlaceBase", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(ConstructableBase), typeof(BaseGhost), typeof(TechType) }, null));
				}

				if (instruction.opcode.Equals(PLACE_FURNITURE_INJECTION_OPCODE) && instruction.operand.Equals(PLACE_FURNITURE_INJECTION_OPERAND)) {
					// Building.BuildPlaceFurniture(gameObject, Builder.placementTarget, CraftData.GetTechType(Builder.prefab));
					yield return new CodeInstruction(OpCodes.Ldloc_2);
					yield return new CodeInstruction(OpCodes.Ldsfld, TARGET_CLASS.GetField("placementTarget", BindingFlags.Static | BindingFlags.NonPublic));
					yield return new CodeInstruction(OpCodes.Ldsfld, TARGET_CLASS.GetField("prefab", BindingFlags.Static | BindingFlags.NonPublic));
					yield return new CodeInstruction(OpCodes.Call, typeof(CraftData).GetMethod("GetTechType", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(GameObject) }, null));
					yield return new CodeInstruction(OpCodes.Call, typeof(Building).GetMethod("PlaceFurniture", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(GameObject), typeof(GameObject), typeof(TechType) }, null));
				}
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchTranspiler(harmony, TARGET_METHOD);
		}
	}

	// Notification for construction and deconstruction events from the build tool.
	public class BuilderTool_Construct_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(BuilderTool);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Construct", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Postfix(bool __result, Constructable c, bool state) {
			if (__result) {
				Logic.Building.ChangeConstructionAmount(c, state);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	// Prevents construction events originating from the server from deviating from the message construction amount.
	public class Constructable_Construct_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Constructable);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Construct", BindingFlags.Public | BindingFlags.Instance);
		public static readonly MethodInfo UPDATE_MATERIAL = TARGET_CLASS.GetMethod("UpdateMaterial", BindingFlags.NonPublic | BindingFlags.Instance);

		public static bool Prefix(Constructable __instance, ref bool __result) {
			if (__instance.constructed == false && Logic.Building.buildConstructActive) {
				__instance.constructedAmount = Mathf.Clamp01(__instance.constructedAmount);

				UPDATE_MATERIAL.Invoke(__instance, null);

				if (__instance.constructedAmount >= 1f) {
					__instance.SetState(true, true);
				}

				__result = true;

				return false;
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Prevents deconstruction events originating from the server from deviating from the message construction amount.
	public class Constructable_Deconstruct_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Constructable);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Deconstruct", BindingFlags.Public | BindingFlags.Instance);
		public static readonly MethodInfo UPDATE_MATERIAL = TARGET_CLASS.GetMethod("UpdateMaterial", BindingFlags.NonPublic | BindingFlags.Instance);

		public static bool Prefix(Constructable __instance, ref bool __result) {
			if (__instance.constructed == false && Logic.Building.buildConstructActive) {
				__instance.constructedAmount = Mathf.Clamp01(__instance.constructedAmount);

				UPDATE_MATERIAL.Invoke(__instance, null);

				if (__instance.constructedAmount <= 0f) {
					UnityEngine.Object.Destroy(__instance.gameObject);
				}

				__result = true;

				return false;
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Overrides target base if applicable, skipping the raycast/camera check.
	public class BaseGhost_FindBase_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(BaseGhost);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("FindBase", BindingFlags.NonPublic | BindingFlags.Static);

		public static bool Prefix(ref Base __result) {
			if (Logic.Building.buildPlaceActive) {
				__result = Logic.Building.buildBase;
				return false;
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Notification of newly created base.
	public class BaseGhost_Finish_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(BaseGhost);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Finish", BindingFlags.Public | BindingFlags.Instance);

		public static readonly OpCode INJECTION_OPCODE = OpCodes.Stfld;
		public static readonly object INJECTION_OPERAND = TARGET_CLASS.GetField("targetBase", BindingFlags.NonPublic | BindingFlags.Instance);

		public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions) {
			foreach (CodeInstruction instruction in instructions) {
				yield return instruction;

				if (instruction.opcode.Equals(INJECTION_OPCODE)) {
					// targetBase = ....
					// Logic.Building.SetNewBase(gameObject)
					yield return new CodeInstruction(OpCodes.Ldloc_2);
					yield return new CodeInstruction(OpCodes.Call, typeof(Logic.Building).GetMethod("SetNewBase", BindingFlags.Public | BindingFlags.Static));
				}
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchTranspiler(harmony, TARGET_METHOD);
		}
	}

	// Notification of a base deconstructable beginning deconstruction.
	public class BaseDeconstructable_Deconstruct_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(BaseDeconstructable);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Deconstruct", BindingFlags.Public | BindingFlags.Instance);

		public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions) {
			foreach (CodeInstruction instruction in instructions) {
				yield return instruction;

				if (instruction.opcode.Equals(OpCodes.Stloc_3)) {
					// Logic.Building.DeconstructBase(this, componentInParent, gameObject, this.recipe)
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Ldloc_3);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, typeof(BaseDeconstructable).GetField("recipe", BindingFlags.NonPublic | BindingFlags.Instance));
					yield return new CodeInstruction(OpCodes.Call, typeof(Logic.Building).GetMethod("DeconstructBase", BindingFlags.Public | BindingFlags.Static));
				}
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchTranspiler(harmony, TARGET_METHOD);
		}
	}

	// Sets rotation of corridor pieces placed by multiplayer building.
	public class BaseAddCorridorGhost_CalculateCorridorType_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(BaseAddCorridorGhost);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("CalculateCorridorType", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Prefix(BaseAddCorridorGhost __instance) {
			if (Building.buildPlaceActive) {
				__instance.ReflectionSet("rotation", (int)Building.buildDirection);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Sets the anchor point for multiplayer placed objects.
	public class BaseAddFaceGhost_UpdatePlacement_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(BaseAddFaceGhost);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("UpdatePlacement", BindingFlags.Public | BindingFlags.Instance);

		public static bool Prefix(BaseAddFaceGhost __instance, ref bool __result, ref bool positionFound, ref bool geometryChanged) {
			// Log.Info("Update placement called.");

			if (Logic.Building.buildPlaceActive == false) {
				return true;
			}

			var targetBase = Logic.Building.buildBase;
			if (targetBase == null) {
				return true;
			}

			__instance.ReflectionSet("targetBase", targetBase);

			var anchorFace = Logic.Building.buildBaseFace;

			var face2 = new Base.Face(anchorFace.cell + Logic.Building.buildBaseAnchor, anchorFace.direction);
			var @int = targetBase.NormalizeCell(face2.cell);
			var cell = targetBase.GetCell(@int);
			var int2 = Base.CellSize[(int)cell];
			var bounds = new Int3.Bounds(face2.cell, face2.cell);
			var bounds2 = new Int3.Bounds(@int, @int + int2 - 1);
			var sourceRange = Int3.Bounds.Union(bounds, bounds2);
			geometryChanged = (bool)__instance.ReflectionCall("UpdateSize", false, false, new object[] { sourceRange.size });
			var face3 = new Base.Face(face2.cell - Logic.Building.buildBaseAnchor, face2.direction);
			var face4 = __instance.anchoredFace;

			if (face4 == null || __instance.anchoredFace.Value != face3) {
				__instance.anchoredFace = new Base.Face?(face3);

				var cell2 = face2.cell - @int;
				var face5 = new Base.Face(cell2, face2.direction);

				// targetBase.SetFaceMask(face5, false);
				// targetBase.SetFace(face5, __instance.faceType);

				var ghostBase = __instance.GhostBase;
				ghostBase.CopyFrom(targetBase, sourceRange, sourceRange.mins * -1);
				ghostBase.ClearMasks();

				ghostBase.SetFaceMask(face5, true);
				ghostBase.SetFace(face5, __instance.faceType);
				__instance.ReflectionCall("RebuildGhostGeometry");
				geometryChanged = true;
			}

			var componentInParent3 = __instance.GetComponentInParent<ConstructableBase>();
			componentInParent3.transform.position = targetBase.GridToWorld(@int);
			componentInParent3.transform.rotation = targetBase.transform.rotation;
			positionFound = true;

			__result = true;
			return false;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Sets rotation of interior modules placed by multiplayer building.
	public class BaseAddModuleGhost_SetupGhost_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(BaseAddModuleGhost);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SetupGhost", BindingFlags.Public | BindingFlags.Instance);

		public static void Postfix(BaseAddModuleGhost __instance) {
			if (Building.buildPlaceActive) {
				__instance.ReflectionSet("direction", (Base.Direction)Building.buildDirection);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	// Sets rotation of scanner rooms placed by multiplayer building.
	public class BaseAddMapRoomGhost_SetupGhost_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(BaseAddMapRoomGhost);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SetupGhost", BindingFlags.Public | BindingFlags.Instance);

		public static void Prefix(BaseAddMapRoomGhost __instance) {
			if (Building.buildPlaceActive) {
				__instance.ReflectionSet("cellType", (Base.CellType)Building.buildDirection);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Prevents input events from occuring during multiplayer building.
	public class GameInput_GetButtonDown_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(GameInput);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("GetButtonDown", BindingFlags.Public | BindingFlags.Static);

		public static bool Prefix(ref bool __result) {
			if (Building.buildPlaceActive) {
				__result = false;
				return false;
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Facbricator item crafting.
	public class Fabricator_OnCraftingBegin_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Fabricator);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnCraftingBegin", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Postfix(Fabricator __instance, TechType techType, float duration) {
			Logic.Crafting.FabricatorStart(__instance, techType, duration);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	// Sets the last crafted object for the mobile vehicle bay.
	public class ConstructorInput_OnCraftingBegin_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(ConstructorInput);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Craft", BindingFlags.NonPublic | BindingFlags.Instance);

		public static bool Prefix(ConstructorInput __instance, TechType techType, float duration) {
			Logic.Crafting.ConstructorCraft(__instance, techType);
			return false;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Fabricator pickup logic.
	public class CrafterLogic_TryPickup_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CrafterLogic);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("TryPickup", BindingFlags.Public | BindingFlags.Instance);

		public static readonly OpCode INJECTION_OPCODE = OpCodes.Stfld;
		public static readonly object INJECTION_OPERAND = TARGET_CLASS.GetField("numCrafted", BindingFlags.Public | BindingFlags.Instance);

		public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions) {
			bool injected = false;

			foreach (CodeInstruction instruction in instructions) {
				yield return instruction;

				if (instruction.opcode.Equals(INJECTION_OPCODE) && instruction.operand.Equals(INJECTION_OPERAND) && !injected) {
					injected = true;

					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, typeof(Component).GetMethod("get_gameObject", BindingFlags.Instance | BindingFlags.Public));
					yield return new CodeInstruction(OpCodes.Ldloc_2);
					yield return new CodeInstruction(OpCodes.Call, typeof(Logic.Crafting).GetMethod("FabricatorPickup", BindingFlags.Static | BindingFlags.Public));
				}
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchTranspiler(harmony, TARGET_METHOD);
		}
	}

	// Prevents splashes when spawning objects.
	public class VFXConstructing_ApplySplashImpulse_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(VFXConstructing);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("ApplySplashImpulse", BindingFlags.NonPublic | BindingFlags.Instance);

		public static bool Prefix(VFXConstructing __instance) {
			return false;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Adds an item to equpiment.
	public class Equipment_AddItem_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Equipment);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("AddItem", BindingFlags.Public | BindingFlags.Instance);

		public static void Postfix(Equipment __instance, bool __result, string slot, InventoryItem newItem) {
			if (__result) {
				Logic.Equipment.Equip(newItem.item, __instance.owner, slot);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	// Removes an item from equipment.
	public class Equipment_RemoveItem_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Equipment);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("RemoveItem", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(string), typeof(bool), typeof(bool) }, null);

		public static readonly OpCode INJECTION_OPCODE = OpCodes.Stloc_1;

		public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions) {
			foreach (CodeInstruction instruction in instructions) {
				yield return instruction;

				if (instruction.opcode.Equals(INJECTION_OPCODE)) {
					// Logic.EquipmentSlots.Unequip(inventoryItem.item, this.owner, slot)
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Callvirt, typeof(InventoryItem).GetMethod("get_item", BindingFlags.Instance | BindingFlags.Public));
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, TARGET_CLASS.GetMethod("get_owner", BindingFlags.Public | BindingFlags.Instance));
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Call, typeof(Logic.Equipment).GetMethod("Unequip", BindingFlags.Public | BindingFlags.Static));
				}
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchTranspiler(harmony, TARGET_METHOD);
		}
	}

	public class ItemsContainer_NotifyAddItem_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(ItemsContainer);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("NotifyAddItem", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Postfix(ItemsContainer __instance, InventoryItem item) {
			if (item != null) {
				Logic.Containers.AddItem(item.item, __instance.tr.parent.gameObject);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class ItemsContainer_NotifyRemoveItem_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(ItemsContainer);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("NotifyRemoveItem", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(InventoryItem) }, null);

		public static void Postfix(ItemsContainer __instance, InventoryItem item) {
			if (item != null) {
				Logic.Containers.RemoveItem(item.item, __instance.tr.parent.gameObject);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	// Notification of an item being dropped.
	public class Pickupable_Drop_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Pickupable);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Drop", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(Vector3), typeof(Vector3), typeof(bool) }, null);

		public static void Postfix(Pickupable __instance, Vector3 dropPosition) {
			Logic.Item.Dropped(__instance.gameObject, __instance.GetTechType(), dropPosition);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	// Notification of an item being picked up.
	public class Pickupable_Pickup_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Pickupable);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Pickup");

		public static bool Prefix(Pickupable __instance) {
			Logic.Item.Grabbed(__instance.gameObject, __instance.GetTechType(), __instance.transform.position);
			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Notification of a resource being broken into chunks.
	public class BreakableResource_BreakIntoResources_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(BreakableResource);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("BreakIntoResources", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Prefix(BreakableResource __instance) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientResourceBreak();
			res.time = DayNightCycle.main.timePassedAsFloat;
			res.tech = CraftData.GetTechType(__instance.gameObject);
			res.position = __instance.transform.position;

			Multiplayer.main.Send(res);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Opening animation for various objects.
	public class Openable_PlayOpenAnimation_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Openable);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("PlayOpenAnimation", BindingFlags.Public | BindingFlags.Instance);

		public static bool Prefix(Openable __instance, bool openState, float duration) {
			if (__instance.isOpen != openState) {
				Logic.Interior.OpenableStateChanged(__instance.gameObject, openState, duration);
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	// Prevents extra supplies from getting spawned during multiplayer.
	public class SpawnEscapePodSupplies_OnNewBorn_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(SpawnEscapePodSupplies);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnNewBorn", BindingFlags.NonPublic | BindingFlags.Instance);

		public static bool Prefix() {
			return false;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	/*
	public class ArmsController_Start_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(ArmsController);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);

		private static readonly MethodInfo reconfigure = TARGET_CLASS.GetMethod("Reconfigure", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Postfix(ArmsController __instance) {
			reconfigure.Invoke(__instance, new PlayerTool[] { null });
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class ArmsController_Update_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(ArmsController);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

		private static readonly FieldInfo leftAimField = TARGET_CLASS.GetField("leftAim", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly FieldInfo rightAimField = TARGET_CLASS.GetField("rightAim", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly FieldInfo reconfigureWorldTarget = TARGET_CLASS.GetField("reconfigureWorldTarget", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo reconfigure = TARGET_CLASS.GetMethod("Reconfigure", BindingFlags.NonPublic | BindingFlags.Instance);

		private static readonly Type armAiming = TARGET_CLASS.GetNestedType("ArmAiming", BindingFlags.NonPublic);
		private static readonly MethodInfo armAimingUpdate = armAiming.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
		private static readonly MethodInfo updateHandIKWeights = TARGET_CLASS.GetMethod("UpdateHandIKWeights", BindingFlags.NonPublic | BindingFlags.Instance);

		public static bool Prefix(ArmsController __instance) {
			if (__instance.smoothSpeedAboveWater == 0) {
				if ((bool)reconfigureWorldTarget.GetValue(__instance)) {
					reconfigure.Invoke(__instance, new PlayerTool[] { null });
					reconfigureWorldTarget.SetValue(__instance, false);
				}

				object leftAim = leftAimField.GetValue(__instance);
				object rightAim = rightAimField.GetValue(__instance);
				object[] args = new object[] { __instance.ikToggleTime };
				armAimingUpdate.Invoke(leftAim, args);
				armAimingUpdate.Invoke(rightAim, args);

				updateHandIKWeights.Invoke(__instance, new object[] { });
				return false;
			}
			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}
	*/

	// Notification of tech being unlocked.
	public class KnownTech_NotifyAdd_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(KnownTech);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("NotifyAdd", BindingFlags.NonPublic | BindingFlags.Static);

		public static void Postfix(TechType techType) {
			Logic.Scanning.AddKnownTech(techType);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	// Notification of a PDA entry being added.
	public class PDAEncyclopedia_Add_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(PDAEncyclopedia);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(string), typeof(PDAEncyclopedia.Entry), typeof(bool) }, null);

		public static void Prefix(string key) {
			if (string.IsNullOrEmpty(key) == false && !PDAEncyclopedia.ContainsEntry(key)) {
				Logic.Scanning.AddEncyclopedia(key);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class PDAScanner_NotifyRemove_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(PDAScanner);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("NotifyRemove", BindingFlags.NonPublic | BindingFlags.Static);

		public static void Prefix(PDAScanner.Entry entry) {
			if (entry != null) {
				Logic.Scanning.AddProgress(entry.techType, entry.unlocked);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class PDAScanner_NotifyProgress_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(PDAScanner);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("NotifyProgress", BindingFlags.NonPublic | BindingFlags.Static);

		public static void Prefix(PDAScanner.Entry entry) {
			if (entry != null) {
				Logic.Scanning.AddProgress(entry.techType, entry.unlocked);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class Vehicle_GetRecentlyUndocked_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Vehicle);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("GetRecentlyUndocked", BindingFlags.Public | BindingFlags.Instance);

		public static bool Prefix(Vehicle __instance, bool __result) {
			var sync = __instance.GetComponent<SyncedVehicle>();
			if (sync != null && sync.activePlayer != null) {
				__result = true;
				return false;
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class VehicleDockingBay_LateUpdate_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(VehicleDockingBay);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

		public static bool Prefix(VehicleDockingBay __instance) {
			var vehicle = __instance.GetDockedVehicle();
			if (vehicle != null) {
				var sync = vehicle.GetComponent<SyncedVehicle>();
				if (sync != null && sync.activePlayer != null) {
					if (Vector3.Distance(vehicle.transform.position, __instance.transform.position) > 5.0f) {
						__instance.SetVehicleUndocked();
						__instance.ReflectionSet("_dockedVehicle", (Vehicle)null);
						__instance.ReflectionSet("interpolatingVehicle", (Vehicle)null);
						__instance.onDockedChanged?.Invoke();
					}
					return false;
				}
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class VehicleDockingBay_DockVehicle_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(VehicleDockingBay);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("DockVehicle", BindingFlags.Public | BindingFlags.Instance);

		public static void Prefix(VehicleDockingBay __instance, Vehicle vehicle) {
			Logic.VehicleLogic.SendDocked(__instance, vehicle, true);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class VehicleDockingBay_SetVehicleDocked_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(VehicleDockingBay);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SetVehicleDocked", BindingFlags.Public | BindingFlags.Instance);

		public static void Prefix(VehicleDockingBay __instance, Vehicle vehicle) {
			Logic.VehicleLogic.SendDocked(__instance, vehicle, true);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class VehicleDockingBay_SetVehicleUndocked_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(VehicleDockingBay);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SetVehicleUndocked", BindingFlags.Public | BindingFlags.Instance);

		public static void Prefix(VehicleDockingBay __instance) {
			Logic.VehicleLogic.SendDocked(__instance, __instance.GetDockedVehicle(), false);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class VehicleDockingBay_OnUndockingComplete_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(VehicleDockingBay);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnUndockingComplete", BindingFlags.Public | BindingFlags.Instance);

		public static void Prefix(VehicleDockingBay __instance) {
			Logic.VehicleLogic.SendDocked(__instance, __instance.GetDockedVehicle(), false);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class Utils_CreatePrefab_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Utils);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("CreatePrefab", BindingFlags.Public | BindingFlags.Static);

		public static void Postfix(GameObject __result, GameObject prefab) {
			Logic.Commands.SendSpawn(__result, CraftData.GetTechType(prefab));
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class SubConsoleCommand_OnConsoleCommand_sub_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(SubConsoleCommand);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnConsoleCommand_sub", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Prefix(SubConsoleCommand __instance, GameObject __state) {
			__state = __instance.GetLastCreatedSub();
		}

		public static void Postfix(SubConsoleCommand __instance, GameObject __state) {
			var gameObject = __instance.GetLastCreatedSub();
			if (gameObject != null && __state != gameObject) {
				Logic.Commands.SendSpawn(gameObject, TechType.Cyclops);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchMultiple(harmony, TARGET_METHOD, true, true, false);
		}
	}

	public class ToggleLights_SetLightsActive_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(ToggleLights);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SetLightsActive", BindingFlags.Public | BindingFlags.Instance);

		private static readonly HashSet<Type> syncedParents = new HashSet<Type>()
		{
			typeof(SeaMoth),
			typeof(Seaglide),
			typeof(FlashLight),
            typeof(LEDLight)
		};

		public static bool Prefix(ToggleLights __instance, out bool __state) {
			__state = __instance.lightsActive;
			return true;
		}

		public static void Postfix(ToggleLights __instance, bool __state) {
			if (__state != __instance.lightsActive) {
				GameObject gameObject = null;
				foreach (Type t in syncedParents) {
					if (__instance.GetComponent(t)) {
						gameObject = __instance.gameObject;
						break;
					} else if (__instance.GetComponentInParent(t)) {
						gameObject = __instance.transform.parent.gameObject;
						break;
					}
				}

				if (gameObject != null) {
					if (Multiplayer.main.blocked == false) {
						var res = new ClientToggleLight();
						res.objectGuid = GuidHelper.Get(gameObject);
						res.state = __instance.lightsActive;
						Multiplayer.main.Send(res);
					}
				}
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchMultiple(harmony, TARGET_METHOD, true, true, false);
		}
	}

	public class SubRoot_Awake_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(SubRoot);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Awake", BindingFlags.Public | BindingFlags.Instance);

		public static void Postfix(SubRoot __instance) {
			__instance.gameObject.EnsureComponent(typeof(PowerRelayMonitor));
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class Vehicle_Awake_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Vehicle);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Awake", BindingFlags.Public | BindingFlags.Instance);

		public static void Postfix(Vehicle __instance) {
			__instance.gameObject.EnsureComponent(typeof(EnergyInterfaceMonitor));
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	class CyclopsHelmHUDManager_Start_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CyclopsHelmHUDManager);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Postfix(CyclopsHelmHUDManager __instance) {
			__instance.ReflectionSet("hudActive", true);
			if (__instance.motorMode.engineOn) {
				__instance.engineToggleAnimator.SetTrigger("EngineOn");
			} else {
				__instance.engineToggleAnimator.SetTrigger("EngineOff");
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class CyclopsHelmHUDManager_StopPiloting_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CyclopsHelmHUDManager);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("StopPiloting", BindingFlags.Public | BindingFlags.Instance);

		public static void Postfix(CyclopsHelmHUDManager __instance) {
			__instance.ReflectionSet("hudActive", true);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	class CyclopsHelmHUDManager_Update_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CyclopsHelmHUDManager);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Postfix(CyclopsHelmHUDManager __instance) {
			//To show the Cyclops HUD every time "hudActive" have to be true. "hornObject" is a good indicator to check if the player piloting the cyclops.
			if (!__instance.hornObject.activeSelf && (bool)__instance.ReflectionGet("hudActive")) {
				__instance.canvasGroup.interactable = false;
			} else if (!(bool)__instance.ReflectionGet("hudActive")) {
				__instance.ReflectionSet("hudActive", true);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class CyclopsHornButton_OnPress_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CyclopsHornButton);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnPress", BindingFlags.Public | BindingFlags.Instance);

		public static void Postfix(CyclopsHornButton __instance) {
			Logic.Commands.SendCyclopsHorn(__instance.subRoot);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class CyclopsEngineChangeState_OnClick_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CyclopsEngineChangeState);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnClick", BindingFlags.Public | BindingFlags.Instance);

		public static void Postfix(CyclopsEngineChangeState __instance) {
			Logic.Commands.SendCyclopsState(__instance.subRoot);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class CyclopsLightingPanel_ToggleFloodlights_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CyclopsLightingPanel);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("ToggleFloodlights", BindingFlags.Public | BindingFlags.Instance);

		public static bool Prefix(CyclopsLightingPanel __instance, out bool __state) {
			__state = __instance.floodlightsOn;
			return true;
		}

		public static void Postfix(CyclopsLightingPanel __instance, bool __state) {
			if (__state != __instance.floodlightsOn) {
				Logic.Commands.SendCyclopsState(__instance.cyclopsRoot);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchMultiple(harmony, TARGET_METHOD, true, true, false);
		}
	}

	public class CyclopsLightingPanel_ToggleInternalLighting_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CyclopsLightingPanel);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("ToggleInternalLighting", BindingFlags.Public | BindingFlags.Instance);

		public static bool Prefix(CyclopsLightingPanel __instance, out bool __state) {
			__state = __instance.lightingOn;
			return true;
		}

		public static void Postfix(CyclopsLightingPanel __instance, bool __state) {
			if (__state != __instance.lightingOn) {
				Logic.Commands.SendCyclopsState(__instance.cyclopsRoot);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchMultiple(harmony, TARGET_METHOD, true, true, false);
		}
	}

	public class CyclopsMotorMode_SaveEngineStateAndPowerDown_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CyclopsMotorMode);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SaveEngineStateAndPowerDown", BindingFlags.Public | BindingFlags.Instance);

		public static bool Prefix(CyclopsMotorMode __instance) {
			__instance.ReflectionSet("engineOnOldState", __instance.engineOn);
			return false;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class CyclopsMotorModeButton_OnClick_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CyclopsMotorModeButton);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnClick", BindingFlags.Public | BindingFlags.Instance);

		public static bool Prefix(CyclopsMotorModeButton __instance, out bool __state) {
			SubRoot cyclops = (SubRoot)__instance.ReflectionGet("subRoot");
			if (cyclops != null && cyclops == Player.main.currentSub) {
				var hud = cyclops.gameObject.GetComponentInChildren<CyclopsHelmHUDManager>();
				if (hud != null) {
					if ((bool)hud.ReflectionGet("hudActive")) {
						__state = hud.hornObject.activeSelf;
						return hud.hornObject.activeSelf;
					}
				}
			}
			__state = false;
			return false;
		}

		public static void Postfix(CyclopsMotorModeButton __instance, bool __state) {
			if (__state) {
				var cyclops = (SubRoot)__instance.ReflectionGet("subRoot");
				Logic.Commands.SendCyclopsState(cyclops);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchMultiple(harmony, TARGET_METHOD, true, true, false);
		}
	}

	public class CyclopsShieldButton_OnClick_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CyclopsShieldButton);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnClick", BindingFlags.Public | BindingFlags.Instance);

		public static void Postfix(CyclopsShieldButton __instance) {
			Logic.Commands.SendCyclopsState(__instance.subRoot);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class CyclopsSilentRunningAbilityButton_TurnOnSilentRunning_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CyclopsSilentRunningAbilityButton);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("TurnOnSilentRunning", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Postfix(CyclopsSilentRunningAbilityButton __instance) {
			Logic.Commands.SendCyclopsState(__instance.subRoot);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class CyclopsSilentRunningAbilityButton_TurnOffSilentRunning_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CyclopsSilentRunningAbilityButton);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("TurnOffSilentRunning", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Postfix(CyclopsSilentRunningAbilityButton __instance) {
			Logic.Commands.SendCyclopsState(__instance.subRoot);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class Vehicle_OnKill_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Vehicle);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnKill", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Prefix(Vehicle __instance) {
			Logic.Commands.SendVehicleKill(__instance.gameObject);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class SubRoot_DestroyCyclopsSubRoot_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(SubRoot);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("DestroyCyclopsSubRoot", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Prefix(SubRoot __instance) {
			Logic.Commands.SendVehicleKill(__instance.gameObject);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	/*
	public class LiveMixin_TakeDamage_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(LiveMixin);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("TakeDamage", BindingFlags.Public | BindingFlags.Instance);

		public static void Prefix(LiveMixin __instance, float originalDamage, Vector3 position, DamageType type) {
			Logic.Commands.SendLiveMixinChange(__instance, -originalDamage, position, type);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}
	*/

	/*
	public class LiveMixin_AddHealth_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(LiveMixin);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("AddHealth", BindingFlags.Public | BindingFlags.Instance);

		public static void Prefix(LiveMixin __instance, float healthBack) {
			Logic.Commands.SendLiveMixinChange(__instance, healthBack);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}
	*/

	public class Welder_Weld_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Welder);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Weld", BindingFlags.NonPublic | BindingFlags.Instance);
		public static readonly FieldInfo activeWeld = TARGET_CLASS.GetField("activeWeldTarget", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Prefix(Welder __instance, float __state) {
			var mixin = (LiveMixin)activeWeld.GetValue(__instance);
			if (mixin != null) {
				__state = mixin.health;
			}
		}

		public static void Postfix(Welder __instance, float __state) {
			var mixin = (LiveMixin)activeWeld.GetValue(__instance);
			if (mixin != null) {
				if (mixin.health > __state) {
					Logic.Commands.SendLiveMixinChange(mixin, 10f);
				}
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchMultiple(harmony, TARGET_METHOD, true, true, false);
		}
	}

	public class StoryGoal_Execute_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Story.StoryGoal);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);

		public static void Prefix(string key, Story.GoalType goalType) {
			Logic.Commands.SendStoryGoal(key, goalType);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class ExosuitGrapplingArm_OnHit_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(ExosuitGrapplingArm);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnHit", BindingFlags.Public | BindingFlags.Instance);

		public static bool Prefix(ExosuitGrapplingArm __instance) {
			var mvc = __instance.GetComponentInParent<SyncedExosuit>();
			if (mvc != null && mvc.activePlayer != null) {
				// Log.Info("Launching remote grapple.");

				var hook = (GrapplingHook)__instance.ReflectionGet("hook");
				hook.transform.parent = null;
				hook.transform.position = __instance.front.transform.position;
				hook.SetFlying(true);

				Exosuit exosuit = mvc.exosuit;
				GameObject x = null;
				Vector3 a = default(Vector3);

				using (new CameraChange(exosuit.transform.position, exosuit.transform.rotation)) {
					UWE.Utils.TraceFPSTargetPosition(exosuit.gameObject, 100f, ref x, ref a, false);
					if (x == null || x == hook.gameObject) {
						a = exosuit.transform.position + exosuit.transform.forward * 25f;
					}
				}

				Vector3 a2 = Vector3.Normalize(a - hook.transform.position);
				hook.rb.velocity = a2 * 25f;
				Utils.PlayFMODAsset(__instance.shootSound, __instance.front, 15f);
				__instance.ReflectionSet("grapplingStartPos", exosuit.transform.position);

				return false;
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class SubNameInput_OnColorChange_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(SubNameInput);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnColorChange", BindingFlags.Public | BindingFlags.Instance);

		public static void Postfix(SubNameInput __instance, ColorChangeEventData eventData) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var subname = (SubName)__instance.ReflectionGet("target");
			if (subname != null) {
				GameObject parentVehicle = null;

				var vehicle = subname.GetComponent<Vehicle>();
				if (vehicle) {
					parentVehicle = vehicle.gameObject;
				} else {
					var subroot = subname.GetComponentInParent<SubRoot>();
					if (subroot != null) {
						parentVehicle = subroot.gameObject;
					}
				}

				if (parentVehicle == null) {
					return;
				}

				var res = new ClientVehicleColorChange();
				res.vehicleGuid = GuidHelper.Get(parentVehicle);
				res.index = __instance.SelectedColorIndex;
				res.hsb = eventData.hsb;
				res.color = eventData.color;
				Multiplayer.main.Send(res);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class SubNameInput_OnNameChange_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(SubNameInput);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnNameChange", BindingFlags.Public | BindingFlags.Instance);

		public static void Postfix(SubNameInput __instance) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var subname = (SubName)__instance.ReflectionGet("target");
			if (subname != null) {
				GameObject parentVehicle = null;

				var vehicle = subname.GetComponent<Vehicle>();
				if (vehicle) {
					parentVehicle = vehicle.gameObject;
				} else {
					var subroot = subname.GetComponentInParent<SubRoot>();
					if (subroot != null) {
						parentVehicle = subroot.gameObject;
					}
				}

				if (parentVehicle == null) {
					return;
				}

				var res = new ClientVehicleNameChange();
				res.vehicleGuid = GuidHelper.Get(parentVehicle);
				res.name = subname.GetName();
				Multiplayer.main.Send(res);
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class Vehicle_OnHandClick_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(Vehicle);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnHandClick", BindingFlags.Public | BindingFlags.Instance);

		public static bool Prefix(Vehicle __instance) {
			var sync = __instance.GetComponent<SyncedVehicle>();
			if (sync != null && sync.activePlayer != null) {
				return false;
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class PilotingChair_OnHandClick_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(PilotingChair);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("OnHandClick", BindingFlags.Public | BindingFlags.Instance);

		public static bool Prefix(PilotingChair __instance) {
			if (__instance.subRoot != null) {
				var sync = __instance.subRoot.GetComponentInParent<SyncedVehicle>();
				if (sync != null && sync.activePlayer != null) {
					return false;
				}
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	/*
	public class CellManager_RegisterEntity_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(CellManager);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("RegisterEntity", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(LargeWorldEntity) }, null);

		public static void Prefix(LargeWorldEntity lwe) {
			var creature = (Creature)lwe.GetComponent(typeof(Creature));
			if (creature != null) {
				var sync = (SyncedCreature)creature.gameObject.EnsureComponent(typeof(SyncedCreature));
				sync.ownership = true;
			}
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}
	*/

	public class SoundQueue_PlayQueued_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(SoundQueue);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("PlayQueued", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(string) }, null);

		public static bool Prefix(SoundQueue __instance) {
			if (Multiplayer.main.blocked) {
				return false;
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class SoundQueue_Play_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(SoundQueue);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Play", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string) }, null);

		public static bool Prefix() {
			if (Multiplayer.main.blocked) {
				return false;
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class VoiceNotification_Play_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(SoundQueue);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Play", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string) }, null);

		public static bool Prefix() {
			if (Multiplayer.main.blocked) {
				return false;
			}

			return true;
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class TelemetryReporting_SendPlayerDeathEvent_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(TelemetryReporting);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SendPlayerDeathEvent", BindingFlags.Public | BindingFlags.Static);

		public static void Postfix(UnityEngine.Object context, Vector3 position) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var res = new ClientPlayerDeath();
			res.id = Multiplayer.main.self.id;
			Multiplayer.main.Send(res);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class BeaconLabel_SetLabel_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(BeaconLabel);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SetLabel", BindingFlags.Public | BindingFlags.Instance);

		public static void Postfix(BeaconLabel __instance, string label) {
			if (Multiplayer.main.blocked) {
				return;
			}

			var parent = __instance.GetComponentInParent<Pickupable>();
			if (parent == null) {
				return;
			}

			var res = new ClientItemLabel();
			res.targetGuid = GuidHelper.Get(parent.gameObject);
			res.label = label;
			Multiplayer.main.Send(res);
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPostfix(harmony, TARGET_METHOD);
		}
	}

	public class EntitySlot_SpawnVirtualEntities_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(EntitySlot);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SpawnVirtualEntities", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Prefix(EntitySlot __instance) {
			var block = LargeWorldStreamer.main.GetBlock(__instance.transform.position);
			UnityEngine.Random.InitState(Multiplayer.main.self.seed + block.GetMoreRandomHash());
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}

	public class EntitySlotsPlaceholder_Spawn_Patch : ShinkaiPatch {
		public static readonly Type TARGET_CLASS = typeof(EntitySlotsPlaceholder);
		public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Spawn", BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Prefix(EntitySlotsPlaceholder __instance) {

			var block = LargeWorldStreamer.main.GetBlock(__instance.transform.position);
			UnityEngine.Random.InitState(Multiplayer.main.self.seed + block.GetHashCode());
		}

		public override void Patch(HarmonyInstance harmony) {
			PatchPrefix(harmony, TARGET_METHOD);
		}
	}
}
