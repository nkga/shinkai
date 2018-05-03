#pragma warning disable // Disable all warnings for copied file

using System.Collections.Generic;
using ShinkaiModel.Core;
using ShinkaiClient.Unity;
using UnityEngine;
using UWE;

namespace ShinkaiClient.Overrides
{
    // Token: 0x020006BA RID: 1722
    public static class MultiplayerBuilder
    {
		// Token: 0x1700034E RID: 846
		// (get) Token: 0x060030F5 RID: 12533 RVA: 0x00021B24 File Offset: 0x0001FD24
		public static Bounds aaBounds {
			get {
				return MultiplayerBuilder._aaBounds;
			}
		}

		// Token: 0x1700034F RID: 847
		// (get) Token: 0x060030F6 RID: 12534 RVA: 0x00021B2B File Offset: 0x0001FD2B
		public static bool isPlacing {
			get {
				return MultiplayerBuilder.prefab != null;
			}
		}

		// Token: 0x17000350 RID: 848
		// (get) Token: 0x060030F7 RID: 12535 RVA: 0x00021B38 File Offset: 0x0001FD38
		// (set) Token: 0x060030F8 RID: 12536 RVA: 0x00021B3F File Offset: 0x0001FD3F
		public static bool canPlace { get; private set; }

		// Token: 0x060030F9 RID: 12537 RVA: 0x0012D2EC File Offset: 0x0012B4EC
		public static void Initialize() {
			if (MultiplayerBuilder.initialized) {
				return;
			}
			MultiplayerBuilder.initialized = true;
			MultiplayerBuilder.placeLayerMask = ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Trigger"));
			MultiplayerBuilder.ghostStructureMaterial = new Material(Resources.Load<Material>("Materials/ghostmodel"));
		}

		// Token: 0x060030FA RID: 12538 RVA: 0x00021B47 File Offset: 0x0001FD47
		public static bool Begin(GameObject modulePrefab) {
			MultiplayerBuilder.Initialize();
			if (modulePrefab == null) {
				Debug.LogWarning("Builder : Begin() : Module prefab is null!");
				return false;
			}
			if (modulePrefab != MultiplayerBuilder.prefab) {
				MultiplayerBuilder.End();
			}
			MultiplayerBuilder.prefab = modulePrefab;
			MultiplayerBuilder.Update();
			return true;
		}

		// Token: 0x060030FB RID: 12539 RVA: 0x0012D348 File Offset: 0x0012B548
		public static void End() {
			MultiplayerBuilder.Initialize();
			if (MultiplayerBuilder.ghostModel != null) {
				ConstructableBase componentInParent = MultiplayerBuilder.ghostModel.GetComponentInParent<ConstructableBase>();
				if (componentInParent != null) {
					UnityEngine.Object.Destroy(componentInParent.gameObject);
				}
				UnityEngine.Object.Destroy(MultiplayerBuilder.ghostModel);
			}
			MultiplayerBuilder.prefab = null;
			MultiplayerBuilder.ghostModel = null;
			MultiplayerBuilder.canPlace = false;
			MultiplayerBuilder.placementTarget = null;
		}

		// Token: 0x060030FC RID: 12540 RVA: 0x0012D3C4 File Offset: 0x0012B5C4
		public static void Update() {
			MultiplayerBuilder.Initialize();
			MultiplayerBuilder.canPlace = false;
			if (MultiplayerBuilder.prefab == null) {
				return;
			}
			if (MultiplayerBuilder.CreateGhost()) {
			}
			MultiplayerBuilder.canPlace = MultiplayerBuilder.UpdateAllowed();
			Transform transform = MultiplayerBuilder.ghostModel.transform;
			transform.position = MultiplayerBuilder.placePosition + MultiplayerBuilder.placeRotation * MultiplayerBuilder.ghostModelPosition;
			transform.rotation = MultiplayerBuilder.placeRotation * MultiplayerBuilder.ghostModelRotation;
			transform.localScale = MultiplayerBuilder.ghostModelScale;
			Color color = (!MultiplayerBuilder.canPlace) ? MultiplayerBuilder.placeColorDeny : MultiplayerBuilder.placeColorAllow;
			IBuilderGhostModel[] components = MultiplayerBuilder.ghostModel.GetComponents<IBuilderGhostModel>();
			for (int i = 0; i < components.Length; i++) {
				components[i].UpdateGhostModelColor(MultiplayerBuilder.canPlace, ref color);
			}
			MaterialExtensions.SetColor(MultiplayerBuilder.renderers, ShaderPropertyID._Tint, color);
		}

		// Token: 0x060030FD RID: 12541 RVA: 0x0012D4C0 File Offset: 0x0012B6C0
		private static bool CreateGhost() {
			if (MultiplayerBuilder.ghostModel != null) {
				return false;
			}
			Constructable component = MultiplayerBuilder.prefab.GetComponent<Constructable>();
			MultiplayerBuilder.constructableTechType = component.techType;
			MultiplayerBuilder.placeMinDistance = component.placeMinDistance;
			MultiplayerBuilder.placeMaxDistance = component.placeMaxDistance;
			MultiplayerBuilder.placeDefaultDistance = component.placeDefaultDistance;
			MultiplayerBuilder.allowedSurfaceTypes = component.allowedSurfaceTypes;
			MultiplayerBuilder.forceUpright = component.forceUpright;
			MultiplayerBuilder.allowedInSub = component.allowedInSub;
			MultiplayerBuilder.allowedInBase = component.allowedInBase;
			MultiplayerBuilder.allowedOutside = component.allowedOutside;
			MultiplayerBuilder.allowedOnConstructables = component.allowedOnConstructables;
			MultiplayerBuilder.rotationEnabled = component.rotationEnabled;

			ConstructableBase component2 = MultiplayerBuilder.prefab.GetComponent<ConstructableBase>();
			if (component2 != null) {
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(MultiplayerBuilder.prefab);
				component2 = gameObject.GetComponent<ConstructableBase>();
				MultiplayerBuilder.ghostModel = component2.model;
				BaseGhost component3 = MultiplayerBuilder.ghostModel.GetComponent<BaseGhost>();
				component3.SetupGhost();
				MultiplayerBuilder.ghostModelPosition = Vector3.zero;
				MultiplayerBuilder.ghostModelRotation = Quaternion.identity;
				MultiplayerBuilder.ghostModelScale = Vector3.one;
				MultiplayerBuilder.renderers = MaterialExtensions.AssignMaterial(MultiplayerBuilder.ghostModel, MultiplayerBuilder.ghostStructureMaterial);
				MaterialExtensions.SetLocalScale(MultiplayerBuilder.renderers);
				MultiplayerBuilder.InitBounds(MultiplayerBuilder.ghostModel);
			} else {
				MultiplayerBuilder.ghostModel = UnityEngine.Object.Instantiate<GameObject>(component.model);
				MultiplayerBuilder.ghostModel.SetActive(true);
				Transform component4 = component.GetComponent<Transform>();
				Transform component5 = component.model.GetComponent<Transform>();
				Quaternion quaternion = Quaternion.Inverse(component4.rotation);
				MultiplayerBuilder.ghostModelPosition = quaternion * (component5.position - component4.position);
				MultiplayerBuilder.ghostModelRotation = quaternion * component5.rotation;
				MultiplayerBuilder.ghostModelScale = component5.lossyScale;
				Collider[] componentsInChildren = MultiplayerBuilder.ghostModel.GetComponentsInChildren<Collider>();
				for (int i = 0; i < componentsInChildren.Length; i++) {
					UnityEngine.Object.Destroy(componentsInChildren[i]);
				}
				MultiplayerBuilder.renderers = MaterialExtensions.AssignMaterial(MultiplayerBuilder.ghostModel, MultiplayerBuilder.ghostStructureMaterial);
				MaterialExtensions.SetLocalScale(MultiplayerBuilder.renderers);
				MultiplayerBuilder.SetupRenderers(MultiplayerBuilder.ghostModel, Player.main.IsInSub());
				MultiplayerBuilder.CreatePowerPreview(MultiplayerBuilder.constructableTechType, MultiplayerBuilder.ghostModel);
				MultiplayerBuilder.InitBounds(MultiplayerBuilder.prefab);
			}
			return true;
		}

		// Token: 0x060030FE RID: 12542 RVA: 0x0012D6FC File Offset: 0x0012B8FC
		private static bool UpdateAllowed() {
			MultiplayerBuilder.SetDefaultPlaceTransform(ref MultiplayerBuilder.placePosition, ref MultiplayerBuilder.placeRotation);
			bool flag = false;
			ConstructableBase componentInParent = MultiplayerBuilder.ghostModel.GetComponentInParent<ConstructableBase>();
			bool flag2;
			if (componentInParent != null) {
				Transform transform = componentInParent.transform;
				transform.position = MultiplayerBuilder.placePosition;
				transform.rotation = MultiplayerBuilder.placeRotation;
				flag2 = componentInParent.UpdateGhostModel(MultiplayerBuilder.GetAimTransform(), MultiplayerBuilder.ghostModel, default(RaycastHit), out flag);
				MultiplayerBuilder.placePosition = transform.position;
				MultiplayerBuilder.placeRotation = transform.rotation;
				if (flag) {
					MultiplayerBuilder.renderers = MaterialExtensions.AssignMaterial(MultiplayerBuilder.ghostModel, MultiplayerBuilder.ghostStructureMaterial);
					MultiplayerBuilder.InitBounds(MultiplayerBuilder.ghostModel);
				}
			} else {
				flag2 = MultiplayerBuilder.CheckAsSubModule();
			}
			if (flag2) {
				List<GameObject> list = new List<GameObject>();
				MultiplayerBuilder.GetObstacles(MultiplayerBuilder.placePosition, MultiplayerBuilder.placeRotation, MultiplayerBuilder.bounds, list);
				flag2 = (list.Count == 0);
				list.Clear();
			}
			return flag2;
		}

		// Token: 0x060030FF RID: 12543 RVA: 0x0012D7E8 File Offset: 0x0012B9E8
		public static bool TryPlace(out GameObject outObject, out Constructable outConstructable) {
			outObject = null;
			outConstructable = null;

			MultiplayerBuilder.Initialize();

			if (MultiplayerBuilder.prefab == null) {
				return false;
			}

			Utils.PlayEnvSound(MultiplayerBuilder.placeSound, MultiplayerBuilder.ghostModel.transform.position, 10f);
			ConstructableBase componentInParent = MultiplayerBuilder.ghostModel.GetComponentInParent<ConstructableBase>();
			if (componentInParent != null) {
				MultiplayerBuilder.Update();
				BaseGhost component = MultiplayerBuilder.ghostModel.GetComponent<BaseGhost>();

				component.Place();
				if (component.TargetBase != null) {
					componentInParent.transform.SetParent(component.TargetBase.transform, true);
				}
				componentInParent.SetState(false, true);

				componentInParent.transform.position = overridePosition;
				componentInParent.transform.rotation = overrideRotation;

				outObject = componentInParent.gameObject;
				outConstructable = componentInParent;
			} else {
				MultiplayerBuilder.placementTarget = GuidHelper.Find(targetGuid);

				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(MultiplayerBuilder.prefab);
				bool flag = false;
				bool flag2 = false;

				SubRoot currentSub = Player.main.GetCurrentSub();
				if (currentSub != null) {
					flag = currentSub.isBase;
					flag2 = currentSub.isCyclops;
					gameObject.transform.parent = currentSub.GetModulesRoot();
				} else if (MultiplayerBuilder.placementTarget != null && MultiplayerBuilder.allowedOutside) {
					SubRoot componentInParent2 = MultiplayerBuilder.placementTarget.GetComponentInParent<SubRoot>();
					if (componentInParent2 != null) {
						gameObject.transform.parent = componentInParent2.GetModulesRoot();
					}
				}
				Transform transform = gameObject.transform;
				transform.localPosition = overridePosition;
				transform.localRotation = overrideRotation;

				Constructable componentInParent3 = gameObject.GetComponentInParent<Constructable>();
				componentInParent3.SetState(false, true);
				Utils.SetLayerRecursively(gameObject, LayerMask.NameToLayer((!flag) ? "Interior" : "Default"), true, -1);
				if (MultiplayerBuilder.ghostModel != null) {
					UnityEngine.Object.Destroy(MultiplayerBuilder.ghostModel);
				}
				componentInParent3.SetIsInside(flag || flag2);
				SkyEnvironmentChanged.Send(gameObject, currentSub);

				transform.localPosition = overridePosition;
				transform.localRotation = overrideRotation;

				outObject = gameObject;
				outConstructable = componentInParent3;
			}
			MultiplayerBuilder.ghostModel = null;
			MultiplayerBuilder.prefab = null;
			MultiplayerBuilder.canPlace = false;
			return true;
		}

		// Token: 0x06003101 RID: 12545 RVA: 0x0012DA08 File Offset: 0x0012BC08
		private static void InitBounds(GameObject gameObject) {
			Transform transform = gameObject.transform;
			MultiplayerBuilder.CacheBounds(transform, gameObject, MultiplayerBuilder.bounds, false);
			MultiplayerBuilder._aaBounds.center = Vector3.zero;
			MultiplayerBuilder._aaBounds.extents = Vector3.zero;
			int count = MultiplayerBuilder.bounds.Count;
			if (count > 0) {
				Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
				Vector3 a = new Vector3(float.MinValue, float.MinValue, float.MinValue);
				for (int i = 0; i < count; i++) {
					OrientedBounds orientedBounds = MultiplayerBuilder.bounds[i];
					Matrix4x4 boundsToLocalMatrix = OrientedBounds.TransformMatrix(orientedBounds.position, orientedBounds.rotation);
					OrientedBounds.MinMaxBounds(boundsToLocalMatrix, Vector3.zero, orientedBounds.extents, ref vector, ref a);
				}
				MultiplayerBuilder._aaBounds.extents = (a - vector) * 0.5f;
				MultiplayerBuilder._aaBounds.center = vector + MultiplayerBuilder.aaBounds.extents;
			}
		}

		// Token: 0x06003102 RID: 12546 RVA: 0x0012DB10 File Offset: 0x0012BD10
		public static void OnDrawGizmos() {
			Matrix4x4 matrix = Gizmos.matrix;
			Color color = Gizmos.color;
			Gizmos.matrix = OrientedBounds.TransformMatrix(MultiplayerBuilder.placePosition, MultiplayerBuilder.placeRotation);
			Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
			Gizmos.DrawCube(MultiplayerBuilder.aaBounds.center, MultiplayerBuilder.aaBounds.extents * 2f);
			Gizmos.matrix = matrix;
			Gizmos.color = color;
			MultiplayerBuilder.OnDrawGizmos();
		}

		// Token: 0x06003103 RID: 12547 RVA: 0x0012DB98 File Offset: 0x0012BD98
		public static void CacheBounds(Transform transform, GameObject target, List<OrientedBounds> results, bool append = false) {
			if (!append) {
				results.Clear();
			}
			if (target == null) {
				return;
			}
			foreach (ConstructableBounds constructableBounds in target.GetComponentsInChildren<ConstructableBounds>()) {
				OrientedBounds localBounds = constructableBounds.bounds;
				OrientedBounds orientedBounds = OrientedBounds.ToWorldBounds(constructableBounds.transform, localBounds);
				if (transform != null) {
					orientedBounds = OrientedBounds.ToLocalBounds(transform, orientedBounds);
				}
				results.Add(orientedBounds);
			}
		}

		// Token: 0x06003104 RID: 12548 RVA: 0x0012DC10 File Offset: 0x0012BE10
		public static bool CheckSpace(Vector3 position, Quaternion rotation, Vector3 extents, int layerMask, Collider allowedCollider) {
			if (extents.x <= 0f || extents.y <= 0f || extents.z <= 0f) {
				return true;
			}
			int num = Physics.OverlapBoxNonAlloc(position, extents, MultiplayerBuilder.sColliders, rotation, layerMask, QueryTriggerInteraction.Ignore);
			return num == 0 || (num == 1 && MultiplayerBuilder.sColliders[0] == allowedCollider);
		}

		// Token: 0x06003105 RID: 12549 RVA: 0x0012DC88 File Offset: 0x0012BE88
		public static bool CheckSpace(Vector3 position, Quaternion rotation, List<OrientedBounds> localBounds, int layerMask, Collider allowedCollider) {
			if (rotation.IsDistinguishedIdentity()) {
				rotation = Quaternion.identity;
			}
			for (int i = 0; i < localBounds.Count; i++) {
				OrientedBounds orientedBounds = localBounds[i];
				if (orientedBounds.rotation.IsDistinguishedIdentity()) {
					orientedBounds.rotation = Quaternion.identity;
				}
				orientedBounds.position = position + rotation * orientedBounds.position;
				orientedBounds.rotation = rotation * orientedBounds.rotation;
				if (!MultiplayerBuilder.CheckSpace(orientedBounds.position, orientedBounds.rotation, orientedBounds.extents, layerMask, allowedCollider)) {
					return false;
				}
			}
			return true;
		}

		// Token: 0x06003106 RID: 12550 RVA: 0x0012DD38 File Offset: 0x0012BF38
		public static void GetOverlappedColliders(Vector3 position, Quaternion rotation, Vector3 extents, List<Collider> results) {
			results.Clear();
			int num = UWE.Utils.OverlapBoxIntoSharedBuffer(position, extents, rotation, -1, QueryTriggerInteraction.Collide);
			for (int i = 0; i < num; i++) {
				Collider collider = UWE.Utils.sharedColliderBuffer[i];
				GameObject gameObject = collider.gameObject;
				if (!collider.isTrigger || gameObject.layer == LayerID.Useable) {
					results.Add(collider);
				}
			}
		}

		// Token: 0x06003107 RID: 12551 RVA: 0x0012DDA0 File Offset: 0x0012BFA0
		public static void GetRootObjects(List<Collider> colliders, List<GameObject> results) {
			results.Clear();
			for (int i = 0; i < colliders.Count; i++) {
				Collider collider = colliders[i];
				GameObject gameObject = collider.gameObject;
				GameObject gameObject2 = UWE.Utils.GetEntityRoot(gameObject);
				if (gameObject2 == null) {
					SceneObjectIdentifier componentInParent = gameObject.GetComponentInParent<SceneObjectIdentifier>();
					if (componentInParent != null) {
						gameObject2 = componentInParent.gameObject;
					}
				}
				gameObject = ((!(gameObject2 != null)) ? gameObject : gameObject2);
				if (!results.Contains(gameObject)) {
					results.Add(gameObject);
				}
			}
		}

		// Token: 0x06003108 RID: 12552 RVA: 0x00021B87 File Offset: 0x0001FD87
		public static void GetOverlappedObjects(Vector3 position, Quaternion rotation, Vector3 extents, List<GameObject> results) {
			MultiplayerBuilder.GetOverlappedColliders(position, rotation, extents, MultiplayerBuilder.sCollidersList);
			MultiplayerBuilder.GetRootObjects(MultiplayerBuilder.sCollidersList, results);
			MultiplayerBuilder.sCollidersList.Clear();
		}

		// Token: 0x06003109 RID: 12553 RVA: 0x0012DE30 File Offset: 0x0012C030
		public static void GetObstacles(Vector3 position, Quaternion rotation, List<OrientedBounds> localBounds, List<GameObject> results) {
			results.Clear();
			if (rotation.IsDistinguishedIdentity()) {
				rotation = Quaternion.identity;
			}
			List<GameObject> list = new List<GameObject>();
			for (int i = 0; i < localBounds.Count; i++) {
				OrientedBounds orientedBounds = localBounds[i];
				if (orientedBounds.rotation.IsDistinguishedIdentity()) {
					orientedBounds.rotation = Quaternion.identity;
				}
				orientedBounds.position = position + rotation * orientedBounds.position;
				orientedBounds.rotation = rotation * orientedBounds.rotation;
				MultiplayerBuilder.GetOverlappedColliders(orientedBounds.position, orientedBounds.rotation, orientedBounds.extents, MultiplayerBuilder.sCollidersList);
				MultiplayerBuilder.GetRootObjects(MultiplayerBuilder.sCollidersList, list);
				for (int j = list.Count - 1; j >= 0; j--) {
					GameObject go = list[j];
					if (!MultiplayerBuilder.IsObstacle(go)) {
						list.RemoveAt(j);
					}
				}
				for (int k = 0; k < MultiplayerBuilder.sCollidersList.Count; k++) {
					Collider collider = MultiplayerBuilder.sCollidersList[k];
					if (MultiplayerBuilder.IsObstacle(collider)) {
						GameObject gameObject = collider.gameObject;
						if (!list.Contains(gameObject)) {
							list.Add(gameObject);
						}
					}
				}
				MultiplayerBuilder.sCollidersList.Clear();
				for (int l = 0; l < list.Count; l++) {
					GameObject item = list[l];
					if (!results.Contains(item)) {
						results.Add(item);
					}
				}
			}
		}

		// Token: 0x0600310A RID: 12554 RVA: 0x0012DFC0 File Offset: 0x0012C1C0
		public static bool CanDestroyObject(GameObject go) {
			Player componentInParent = go.GetComponentInParent<Player>();
			if (componentInParent != null) {
				return false;
			}
			LargeWorldEntity component = go.GetComponent<LargeWorldEntity>();
			if (component != null && component.cellLevel >= LargeWorldEntity.CellLevel.Global) {
				return false;
			}
			SubRoot componentInParent2 = go.GetComponentInParent<SubRoot>();
			if (componentInParent2 != null) {
				return false;
			}
			Constructable componentInParent3 = go.GetComponentInParent<Constructable>();
			if (componentInParent3 != null) {
				return false;
			}
			IObstacle component2 = go.GetComponent<IObstacle>();
			if (component2 != null) {
				return false;
			}
			Pickupable component3 = go.GetComponent<Pickupable>();
			if (component3 != null && component3.attached) {
				return false;
			}
			PlaceTool component4 = go.GetComponent<PlaceTool>();
			return !(component4 != null);
		}

		// Token: 0x0600310B RID: 12555 RVA: 0x0012E07C File Offset: 0x0012C27C
		public static bool IsObstacle(Collider collider) {
			if (collider != null) {
				GameObject gameObject = collider.gameObject;
				if (gameObject.layer == LayerID.TerrainCollider) {
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600310C RID: 12556 RVA: 0x0012E0B0 File Offset: 0x0012C2B0
		public static bool IsObstacle(GameObject go) {
			return go.GetComponent<IObstacle>() != null;
		}

		// Token: 0x0600310D RID: 12557 RVA: 0x00003343 File Offset: 0x00001543
		public static Transform GetAimTransform() {
			return MainCamera.camera.transform;
		}

		// Token: 0x0600310E RID: 12558 RVA: 0x00021BAB File Offset: 0x0001FDAB
		public static GameObject GetGhostModel() {
			return MultiplayerBuilder.ghostModel;
		}

		// Token: 0x0600310F RID: 12559 RVA: 0x0012E0D0 File Offset: 0x0012C2D0
		private static bool CheckAsSubModule() {
			if (!Constructable.CheckFlags(MultiplayerBuilder.allowedInBase, MultiplayerBuilder.allowedInSub, MultiplayerBuilder.allowedOutside)) {
				return false;
			}
			Transform aimTransform = MultiplayerBuilder.GetAimTransform();
			MultiplayerBuilder.placementTarget = null;
			RaycastHit hit;
			if (!Physics.Raycast(aimTransform.position, aimTransform.forward, out hit, MultiplayerBuilder.placeMaxDistance, MultiplayerBuilder.placeLayerMask.value, QueryTriggerInteraction.Ignore)) {
				return false;
			}
			MultiplayerBuilder.placementTarget = hit.collider.gameObject;
			MultiplayerBuilder.SetPlaceOnSurface(hit, ref MultiplayerBuilder.placePosition, ref MultiplayerBuilder.placeRotation);
			if (!MultiplayerBuilder.CheckTag(hit.collider)) {
				return false;
			}
			if (!MultiplayerBuilder.CheckSurfaceType(MultiplayerBuilder.GetSurfaceType(hit.normal))) {
				return false;
			}
			if (!MultiplayerBuilder.CheckDistance(hit.point, MultiplayerBuilder.placeMinDistance)) {
				return false;
			}
			if (!MultiplayerBuilder.allowedOnConstructables && MultiplayerBuilder.HasComponent<Constructable>(hit.collider.gameObject)) {
				return false;
			}
			if (!Player.main.IsInSub()) {
				GameObject entityRoot = UWE.Utils.GetEntityRoot(MultiplayerBuilder.placementTarget);
				if (!entityRoot) {
					entityRoot = MultiplayerBuilder.placementTarget;
				}
				if (!MultiplayerBuilder.ValidateOutdoor(entityRoot)) {
					return false;
				}
			}
			return MultiplayerBuilder.CheckSpace(MultiplayerBuilder.placePosition, MultiplayerBuilder.placeRotation, MultiplayerBuilder.bounds, MultiplayerBuilder.placeLayerMask.value, hit.collider);
		}

		// Token: 0x06003110 RID: 12560 RVA: 0x00021BB2 File Offset: 0x0001FDB2
		private static SurfaceType GetSurfaceType(Vector3 hitNormal) {
			if ((double)hitNormal.y < -0.33) {
				return SurfaceType.Ceiling;
			}
			if ((double)hitNormal.y < 0.33) {
				return SurfaceType.Wall;
			}
			return SurfaceType.Ground;
		}

		// Token: 0x06003111 RID: 12561 RVA: 0x0012E21C File Offset: 0x0012C41C
		private static bool CheckTag(Collider c) {
			if (c == null) {
				return false;
			}
			GameObject gameObject = c.gameObject;
			return !(gameObject == null) && !gameObject.CompareTag(MultiplayerBuilder.ignoreTag);
		}

		// Token: 0x06003112 RID: 12562 RVA: 0x00021BE5 File Offset: 0x0001FDE5
		private static bool CheckSurfaceType(SurfaceType surfaceType) {
			return MultiplayerBuilder.allowedSurfaceTypes.Contains(surfaceType);
		}

		// Token: 0x06003113 RID: 12563 RVA: 0x0012E260 File Offset: 0x0012C460
		private static bool CheckDistance(Vector3 worldPosition, float minDistance) {
			Transform aimTransform = MultiplayerBuilder.GetAimTransform();
			float magnitude = (worldPosition - aimTransform.position).magnitude;
			return magnitude >= minDistance;
		}

		// Token: 0x06003114 RID: 12564 RVA: 0x00021BF2 File Offset: 0x0001FDF2
		private static bool HasComponent<T>(GameObject go) where T : Component {
			return go.GetComponentInParent<T>() != null;
		}

		// Token: 0x06003115 RID: 12565 RVA: 0x0012E290 File Offset: 0x0012C490
		private static void SetDefaultPlaceTransform(ref Vector3 position, ref Quaternion rotation) {
			Transform aimTransform = MultiplayerBuilder.GetAimTransform();
			position = aimTransform.position + aimTransform.forward * MultiplayerBuilder.placeDefaultDistance;
			Vector3 forward;
			Vector3 up;
			if (MultiplayerBuilder.forceUpright) {
				forward = -aimTransform.forward;
				forward.y = 0f;
				forward.Normalize();
				up = Vector3.up;
			} else {
				forward = -aimTransform.forward;
				up = aimTransform.up;
			}
			rotation = Quaternion.LookRotation(forward, up);
			if (MultiplayerBuilder.rotationEnabled) {
				rotation = Quaternion.AngleAxis(MultiplayerBuilder.additiveRotation, up) * rotation;
			}
		}

		// Token: 0x06003116 RID: 12566 RVA: 0x0012E340 File Offset: 0x0012C540
		private static void SetPlaceOnSurface(RaycastHit hit, ref Vector3 position, ref Quaternion rotation) {
			Transform aimTransform = MultiplayerBuilder.GetAimTransform();
			Vector3 vector = Vector3.forward;
			Vector3 vector2 = Vector3.up;
			if (MultiplayerBuilder.forceUpright) {
				vector = -aimTransform.forward;
				vector.y = 0f;
				vector.Normalize();
				vector2 = Vector3.up;
			} else {
				SurfaceType surfaceType = MultiplayerBuilder.GetSurfaceType(hit.normal);
				if (surfaceType != SurfaceType.Wall) {
					if (surfaceType != SurfaceType.Ceiling) {
						if (surfaceType == SurfaceType.Ground) {
							vector2 = hit.normal;
							vector = -aimTransform.forward;
							vector.y -= Vector3.Dot(vector, vector2);
							vector.Normalize();
						}
					} else {
						vector = hit.normal;
						vector2 = -aimTransform.forward;
						vector2.y -= Vector3.Dot(vector2, vector);
						vector2.Normalize();
					}
				} else {
					vector = hit.normal;
					vector2 = Vector3.up;
				}
			}
			position = hit.point;
			rotation = Quaternion.LookRotation(vector, vector2);
			if (MultiplayerBuilder.rotationEnabled) {
				rotation = Quaternion.AngleAxis(MultiplayerBuilder.additiveRotation, vector2) * rotation;
			}
		}

		// Token: 0x06003117 RID: 12567 RVA: 0x0012E478 File Offset: 0x0012C678
		private static void SetupRenderers(GameObject gameObject, bool interior) {
			int newLayer;
			if (interior) {
				newLayer = LayerMask.NameToLayer("Viewmodel");
			} else {
				newLayer = LayerMask.NameToLayer("Default");
			}
			Utils.SetLayerRecursively(gameObject, newLayer, true, -1);
		}

		// Token: 0x06003118 RID: 12568 RVA: 0x0012E4B0 File Offset: 0x0012C6B0
		public static bool ValidateOutdoor(GameObject hitObject) {
			Rigidbody component = hitObject.GetComponent<Rigidbody>();
			if (component && !component.isKinematic) {
				return false;
			}
			SubRoot component2 = hitObject.GetComponent<SubRoot>();
			Base component3 = hitObject.GetComponent<Base>();
			if (component2 != null && component3 == null) {
				return false;
			}
			Pickupable component4 = hitObject.GetComponent<Pickupable>();
			if (component4 != null) {
				return false;
			}
			LiveMixin component5 = hitObject.GetComponent<LiveMixin>();
			return !(component5 != null) || !component5.destroyOnDeath;
		}

		// Token: 0x06003119 RID: 12569 RVA: 0x0012E540 File Offset: 0x0012C740
		private static void CreatePowerPreview(TechType constructableTechType, GameObject ghostModel) {
			GameObject gameObject = null;
			string poweredPrefabName = CraftData.GetPoweredPrefabName(constructableTechType);
			if (poweredPrefabName != string.Empty) {
				gameObject = PrefabDatabase.GetPrefabForFilename(poweredPrefabName);
			}
			if (gameObject != null) {
				PowerRelay component = gameObject.GetComponent<PowerRelay>();
				Utils.Assert(component != null, "see log", null);
				if (component.powerFX != null && component.powerFX.attachPoint != null) {
					PowerFX powerFX = ghostModel.AddComponent<PowerFX>();
					powerFX.attachPoint = new GameObject {
						transform =
						{
						parent = ghostModel.transform,
						localPosition = component.powerFX.attachPoint.localPosition
					}
					}.transform;
				}
				PowerRelay powerRelay = ghostModel.AddComponent<PowerRelay>();
				powerRelay.maxOutboundDistance = component.maxOutboundDistance;
				powerRelay.dontConnectToRelays = component.dontConnectToRelays;
				if (component.internalPowerSource != null) {
					PowerSource powerSource = ghostModel.AddComponent<PowerSource>();
					powerSource.maxPower = 0f;
					powerRelay.internalPowerSource = powerSource;
				}
			}
		}

		// Custom attributes.
		public static string baseGhostGuid;
		public static string targetGuid;

		public static Vector3 overridePosition;
		public static Quaternion overrideRotation;

		private static Vector3 placePosition;
		private static Quaternion placeRotation;

		// Token: 0x04003170 RID: 12656
		public static readonly float additiveRotationSpeed = 90f;

		// Token: 0x04003171 RID: 12657
		public static readonly GameInput.Button buttonRotateCW = GameInput.Button.CyclePrev;

		// Token: 0x04003172 RID: 12658
		public static readonly GameInput.Button buttonRotateCCW = GameInput.Button.CycleNext;

		// Token: 0x04003173 RID: 12659
		private static readonly Vector3[] checkDirections = new Vector3[]
		{
		Vector3.up,
		Vector3.down,
		Vector3.left,
		Vector3.right
		};

		// Token: 0x04003174 RID: 12660
		private static readonly Color placeColorAllow = new Color(0f, 1f, 0f, 1f);

		// Token: 0x04003175 RID: 12661
		private static readonly Color placeColorDeny = new Color(1f, 0f, 0f, 1f);

		// Token: 0x04003176 RID: 12662
		private static readonly string ignoreTag = "DenyBuilding";

		// Token: 0x04003177 RID: 12663
		private static bool initialized = false;

		// Token: 0x04003179 RID: 12665
		private static Collider[] sColliders = new Collider[2];

		// Token: 0x0400317A RID: 12666
		private static List<Collider> sCollidersList = new List<Collider>();

		// Token: 0x0400317C RID: 12668
		public static float additiveRotation = 0f;

		// Token: 0x0400317D RID: 12669
		private static GameObject prefab;

		// Token: 0x0400317E RID: 12670
		private static float placeMaxDistance;

		// Token: 0x0400317F RID: 12671
		private static float placeMinDistance;

		// Token: 0x04003180 RID: 12672
		private static float placeDefaultDistance;

		// Token: 0x04003181 RID: 12673
		private static TechType constructableTechType;

		// Token: 0x04003182 RID: 12674
		private static List<SurfaceType> allowedSurfaceTypes;

		// Token: 0x04003183 RID: 12675
		private static bool forceUpright;

		// Token: 0x04003184 RID: 12676
		private static bool allowedInSub;

		// Token: 0x04003185 RID: 12677
		private static bool allowedInBase;

		// Token: 0x04003186 RID: 12678
		private static bool allowedOutside;

		// Token: 0x04003187 RID: 12679
		private static bool allowedOnConstructables;

		// Token: 0x04003188 RID: 12680
		private static bool rotationEnabled;

		// Token: 0x04003189 RID: 12681
		private static Renderer[] renderers;

		// Token: 0x0400318A RID: 12682
		private static GameObject ghostModel;

		// Token: 0x0400318B RID: 12683
		private static Vector3 ghostModelPosition;

		// Token: 0x0400318C RID: 12684
		private static Quaternion ghostModelRotation;

		// Token: 0x0400318D RID: 12685
		private static Vector3 ghostModelScale;

		// Token: 0x0400318E RID: 12686
		private static List<OrientedBounds> bounds = new List<OrientedBounds>();

		// Token: 0x0400318F RID: 12687
		private static Bounds _aaBounds = default(Bounds);

		// Token: 0x04003192 RID: 12690
		private static Material ghostStructureMaterial;

		// Token: 0x04003193 RID: 12691
		private static LayerMask placeLayerMask;

		// Token: 0x04003194 RID: 12692
		private static GameObject placementTarget;

		// Token: 0x04003195 RID: 12693
		private static string placeSound = "event:/tools/builder/place";
	}
}
#pragma warning restore // Re-enable all warnings for copied file
