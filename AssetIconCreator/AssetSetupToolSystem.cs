using Colossal.Serialization.Entities;

using Game;
using Game.Areas;
using Game.Audio;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Debug;
using Game.Input;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.Simulation;
using Game.Tools;
using Game.UI.Editor;

using System;
using System.Linq;

using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using UnityEngine;

using AgeMask = Game.Tools.AgeMask;
using Space = Game.Areas.Space;
using Transform = Game.Objects.Transform;

namespace AssetIconCreator
{
	internal partial class AssetSetupToolSystem : ToolBaseSystem
	{
		private PhotoModeRenderSystem _photoModeRenderSystem;
		private CameraUpdateSystem _cameraUpdateSystem;
		private DefaultToolSystem _defaultToolSystem;
		private ToolSystem _toolSystem;
		private RenderingSystem _renderingSystem;
		private ToolRaycastSystem _toolRaycastSystem;
		private SimulationSystem _simulationSystem;
		private ObjectToolSystem _objectToolSystem;
		private PrefabSystem _prefabSystem;
		private TerrainSystem _terrainSystem;
		private CityConfigurationSystem _cityConfigurationSystem;
		private AudioManager _audioManager;
		private WaterSystem _waterSystem;
		private ToolOutputBarrier _toolOutputBarrier;
		private ClimateSystem _climateSystem;
		private DebugSystem _debugSystem;
		private PlanetarySystem _planetarySystem;
		private EditorToolUISystem _editorToolUISystem;
		private AssetCreatorUISystem _assetCreatorUISystem;
		private ProxyAction toolHotKey;
		private PrefabBase selectedPrefab;
		private GameMode gameMode;
		private float currentFov;
		private float simSpeed;

		public override string toolID { get; } = nameof(AssetSetupToolSystem);

		public ScreenshotUtility ScreenshotUtility { get; private set; }

		protected override void OnCreate()
		{
			base.OnCreate();

			_photoModeRenderSystem = World.GetOrCreateSystemManaged<PhotoModeRenderSystem>();
			_cameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
			_defaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
			_toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
			_renderingSystem = World.GetOrCreateSystemManaged<RenderingSystem>();
			_toolRaycastSystem = World.GetOrCreateSystemManaged<ToolRaycastSystem>();
			_simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
			_objectToolSystem = World.GetOrCreateSystemManaged<ObjectToolSystem>();
			_prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
			_terrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
			_cityConfigurationSystem = World.GetOrCreateSystemManaged<CityConfigurationSystem>();
			_audioManager = World.GetOrCreateSystemManaged<AudioManager>();
			_waterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
			_toolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
			_climateSystem = World.GetOrCreateSystemManaged<ClimateSystem>();
			_debugSystem = World.GetOrCreateSystemManaged<DebugSystem>();
			_planetarySystem = World.GetOrCreateSystemManaged<PlanetarySystem>();
			_editorToolUISystem = World.GetOrCreateSystemManaged<EditorToolUISystem>();
			_assetCreatorUISystem = World.GetOrCreateSystemManaged<AssetCreatorUISystem>();

			ScreenshotUtility = new ScreenshotUtility(_simulationSystem, _renderingSystem, _toolRaycastSystem, _cameraUpdateSystem, _prefabSystem, _defaultToolSystem, _toolSystem);

			toolHotKey = Mod.Settings.GetAction(nameof(Setting.ToolKeyBinding));

			toolHotKey.shouldBeEnabled = true;
			toolHotKey.onInteraction += ToolHotKey_onInteraction;
		}

		protected override void OnGamePreload(Purpose purpose, GameMode mode)
		{
			base.OnGamePreload(purpose, mode);

			if (mode == GameMode.Editor)
			{
				if (_editorToolUISystem?.tools?.Any(t => t?.id == toolID) ?? true)
				{
					return;
				}

				var tools = _editorToolUISystem.tools;
				Array.Resize(ref tools, tools.Length + 1);
				tools[tools.Length - 1] = new AssetIconCreatorEditorTool(World, this, _toolSystem);
				_editorToolUISystem.tools = tools;
			}
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			selectedPrefab = null;
			applyAction.shouldBeEnabled = true;
		}

		protected override void OnStopRunning()
		{
			base.OnStopRunning();

			selectedPrefab = null;
			applyAction.shouldBeEnabled = false;
		}

		public override void InitializeRaycast()
		{
			base.InitializeRaycast();

			m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.MovingObjects | TypeMask.Areas;
			m_ToolRaycastSystem.areaTypeMask |= AreaTypeMask.Lots | AreaTypeMask.Spaces | AreaTypeMask.Surfaces;
			m_ToolRaycastSystem.raycastFlags = RaycastFlags.BuildingLots | RaycastFlags.Decals | RaycastFlags.PartialSurface;
			m_ToolRaycastSystem.collisionMask = CollisionMask.Overground | CollisionMask.OnGround | CollisionMask.Underground;
		}

		private void ToolHotKey_onInteraction(ProxyAction arg1, UnityEngine.InputSystem.InputActionPhase arg2)
		{
			if (arg2 == UnityEngine.InputSystem.InputActionPhase.Performed)
			{
				_toolSystem.activeTool = this;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			applyMode = ApplyMode.Clear;

			EntityManager.RemoveComponent<Highlighted>(SystemAPI.QueryBuilder().WithAll<Highlighted>().Build());
			EntityManager.AddComponent<BatchesUpdated>(SystemAPI.QueryBuilder().WithAll<Highlighted>().Build());

			if (ScreenshotUtility.SettingUp)
			{
				return base.OnUpdate(inputDeps);
			}

			if (GetRaycastResult(out var entity, out RaycastHit hit) && CheckIfReady(entity))
			{
				if (applyAction.WasPressedThisFrame())
				{
					gameMode = m_ToolSystem.actionMode;
					currentFov = _cameraUpdateSystem.orbitCameraController.lens.FieldOfView;
					simSpeed = _simulationSystem.selectedSpeed;

					if (Mod.Settings.ClearMap)
					{
						EntityManager.AddComponent<Deleted>(SystemAPI.QueryBuilder().WithAll<Game.Objects.Object>().WithNone<Deleted, Temp>().Build());
					}

					Setup(entity);

					selectedPrefab = _prefabSystem.GetPrefab<PrefabBase>(EntityManager.GetComponentData<PrefabRef>(entity));

					return base.OnUpdate(inputDeps);
				}

				EntityManager.AddComponent<Highlighted>(entity);
				EntityManager.AddComponent<BatchesUpdated>(entity);
			}

			if (selectedPrefab != null)
			{
				applyMode = ApplyMode.Apply;

				GameManager.instance.StartCoroutine(ScreenshotUtility.CaptureScreenshot(selectedPrefab, gameMode, currentFov, simSpeed));
			}

			return base.OnUpdate(inputDeps);
		}

		private void Setup(Entity entity)
		{
			_simulationSystem.selectedSpeed = 0f;

			typeof(ToolSystem).GetProperty("actionMode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).SetValue(m_ToolSystem, GameMode.Game);

			OnGamePreload(Purpose.NewGame, GameMode.Game);

			SetupWeatherAndTime();

			SetupCamera(entity);

			EntityManager.AddComponent<Deleted>(entity);

			CreateNewEntity(entity);
		}

		private void CreateNewEntity(Entity entity)
		{
			var transform = EntityManager.GetComponentData<Transform>(entity);
			var prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;

			CreateDefinitions(prefab, transform.m_Position, new quaternion(0, 1f, 0, 0.2f), RandomSeed.Next());
		}

		private bool CheckIfReady(Entity entity)
		{
			if (_cameraUpdateSystem.orbitCameraController is null || entity == Entity.Null)
			{
				return false;
			}

			return EntityManager.HasComponent<PrefabRef>(entity)
				&& EntityManager.HasComponent<CullingInfo>(entity)
				&& EntityManager.HasComponent<Transform>(entity);
		}

		private void SetupWeatherAndTime()
		{
			var shadows = _photoModeRenderSystem.photoModeProperties["ShadowsMidtonesHighlights.shadows"];
			shadows.setValue(0.15f);
			shadows.setEnabled(true);

			var midtones = _photoModeRenderSystem.photoModeProperties["ShadowsMidtonesHighlights.midtones"];
			midtones.setValue(0.15f);
			midtones.setEnabled(true);

			var highlights = _photoModeRenderSystem.photoModeProperties["ShadowsMidtonesHighlights.highlights"];
			highlights.setValue(-0.2f);
			highlights.setEnabled(true);

			_climateSystem.currentDate.overrideValue = 0.5f;
			_climateSystem.currentDate.overrideState = true;
			_climateSystem.precipitation.overrideValue = 0f;
			_climateSystem.precipitation.overrideState = true;
			_climateSystem.cloudiness.overrideValue = 0f;
			_climateSystem.cloudiness.overrideState = true;
			_planetarySystem.longitude = 0;
			_planetarySystem.latitude = 15;
			_planetarySystem.day = 85;
			_planetarySystem.time = 11.5f;
			_planetarySystem.overrideTime = true;

			typeof(DebugSystem).GetField("m_FastForwardClimateTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(_debugSystem, true);
		}

		private void SetupCamera(Entity entity)
		{
			var cullingInfo = EntityManager.GetComponentData<CullingInfo>(entity);
			var camera = _cameraUpdateSystem.orbitCameraController;
			var fov = Mathf.Clamp(Camera.FocalLengthToFieldOfView(Mathf.Max(240, 0.0001f), camera.lens.SensorSize.y), 1f, 179f);

			_cameraUpdateSystem.activeCameraController = _cameraUpdateSystem.orbitCameraController;

			camera.lens.FieldOfView = fov;
			camera.followedEntity = entity;

			typeof(OrbitCameraController)
				.GetField("m_FollowTimer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.SetValue(camera, float.MaxValue);

			camera.UpdateCamera();
			camera.zoom = (EntityManager.HasComponent<Building>(entity) ? 120 : 40)
				+ math.max((cullingInfo.m_Bounds.max.y - cullingInfo.m_Bounds.min.y) * 10f, (cullingInfo.m_Bounds.max.x - cullingInfo.m_Bounds.min.x) * 8f);
			camera.rotation = new Vector3(20, -60, 0);
			camera.pivot = (cullingInfo.m_Bounds.min + cullingInfo.m_Bounds.max) * 0.5f;
			camera.position = (cullingInfo.m_Bounds.min + cullingInfo.m_Bounds.max) * 0.5f;
			camera.UpdateCamera();
		}

		private void CreateDefinitions(Entity objectPrefab, float3 position, quaternion rotation, RandomSeed randomSeed)
		{
			CreateDefinitions definitions = default;
			definitions.m_RandomizationEnabled = false;
			definitions.m_FixedRandomSeed = 0;
			definitions.m_EditorMode = false;
			definitions.m_LefthandTraffic = _cityConfigurationSystem.leftHandTraffic;
			definitions.m_ObjectPrefab = objectPrefab;
			definitions.m_Theme = _cityConfigurationSystem.defaultTheme;
			definitions.m_RandomSeed = randomSeed;
			definitions.m_AgeMask = AgeMask.Mature;
			definitions.m_ControlPoint = new() { m_Position = position, m_Rotation = rotation };
			definitions.m_AttachmentPrefab = default;
			definitions.m_OwnerData = SystemAPI.GetComponentLookup<Owner>(true);
			definitions.m_TransformData = SystemAPI.GetComponentLookup<Transform>(true);
			definitions.m_AttachedData = SystemAPI.GetComponentLookup<Attached>(true);
			definitions.m_LocalTransformCacheData = SystemAPI.GetComponentLookup<LocalTransformCache>(true);
			definitions.m_ElevationData = SystemAPI.GetComponentLookup<Game.Objects.Elevation>(true);
			definitions.m_BuildingData = SystemAPI.GetComponentLookup<Building>(true);
			definitions.m_LotData = SystemAPI.GetComponentLookup<Game.Buildings.Lot>(true);
			definitions.m_EdgeData = SystemAPI.GetComponentLookup<Edge>(true);
			definitions.m_NodeData = SystemAPI.GetComponentLookup<Game.Net.Node>(true);
			definitions.m_CurveData = SystemAPI.GetComponentLookup<Curve>(true);
			definitions.m_NetElevationData = SystemAPI.GetComponentLookup<Game.Net.Elevation>(true);
			definitions.m_OrphanData = SystemAPI.GetComponentLookup<Orphan>(true);
			definitions.m_UpgradedData = SystemAPI.GetComponentLookup<Upgraded>(true);
			definitions.m_CompositionData = SystemAPI.GetComponentLookup<Composition>(true);
			definitions.m_AreaClearData = SystemAPI.GetComponentLookup<Clear>(true);
			definitions.m_AreaSpaceData = SystemAPI.GetComponentLookup<Space>(true);
			definitions.m_AreaLotData = SystemAPI.GetComponentLookup<Game.Areas.Lot>(true);
			definitions.m_EditorContainerData = SystemAPI.GetComponentLookup<Game.Tools.EditorContainer>(true);
			definitions.m_PrefabRefData = SystemAPI.GetComponentLookup<PrefabRef>(true);
			definitions.m_PrefabNetObjectData = SystemAPI.GetComponentLookup<NetObjectData>(true);
			definitions.m_PrefabBuildingData = SystemAPI.GetComponentLookup<BuildingData>(true);
			definitions.m_PrefabAssetStampData = SystemAPI.GetComponentLookup<AssetStampData>(true);
			definitions.m_PrefabBuildingExtensionData = SystemAPI.GetComponentLookup<BuildingExtensionData>(true);
			definitions.m_PrefabSpawnableObjectData = SystemAPI.GetComponentLookup<SpawnableObjectData>(true);
			definitions.m_PrefabObjectGeometryData = SystemAPI.GetComponentLookup<ObjectGeometryData>(true);
			definitions.m_PrefabPlaceableObjectData = SystemAPI.GetComponentLookup<PlaceableObjectData>(true);
			definitions.m_PrefabAreaGeometryData = SystemAPI.GetComponentLookup<AreaGeometryData>(true);
			definitions.m_PrefabBuildingTerraformData = SystemAPI.GetComponentLookup<BuildingTerraformData>(true);
			definitions.m_PrefabCreatureSpawnData = SystemAPI.GetComponentLookup<CreatureSpawnData>(true);
			definitions.m_PlaceholderBuildingData = SystemAPI.GetComponentLookup<PlaceholderBuildingData>(true);
			definitions.m_PrefabNetGeometryData = SystemAPI.GetComponentLookup<NetGeometryData>(true);
			definitions.m_PrefabCompositionData = SystemAPI.GetComponentLookup<NetCompositionData>(true);
			definitions.m_SubObjects = SystemAPI.GetBufferLookup<Game.Objects.SubObject>(true);
			definitions.m_CachedNodes = SystemAPI.GetBufferLookup<LocalNodeCache>(true);
			definitions.m_InstalledUpgrades = SystemAPI.GetBufferLookup<InstalledUpgrade>(true);
			definitions.m_SubNets = SystemAPI.GetBufferLookup<Game.Net.SubNet>(true);
			definitions.m_ConnectedEdges = SystemAPI.GetBufferLookup<ConnectedEdge>(true);
			definitions.m_SubAreas = SystemAPI.GetBufferLookup<Game.Areas.SubArea>(true);
			definitions.m_AreaNodes = SystemAPI.GetBufferLookup<Game.Areas.Node>(true);
			definitions.m_AreaTriangles = SystemAPI.GetBufferLookup<Triangle>(true);
			definitions.m_PrefabSubObjects = SystemAPI.GetBufferLookup<Game.Prefabs.SubObject>(true);
			definitions.m_PrefabSubNets = SystemAPI.GetBufferLookup<Game.Prefabs.SubNet>(true);
			definitions.m_PrefabSubLanes = SystemAPI.GetBufferLookup<Game.Prefabs.SubLane>(true);
			definitions.m_PrefabSubAreas = SystemAPI.GetBufferLookup<Game.Prefabs.SubArea>(true);
			definitions.m_PrefabSubAreaNodes = SystemAPI.GetBufferLookup<SubAreaNode>(true);
			definitions.m_PrefabPlaceholderElements = SystemAPI.GetBufferLookup<PlaceholderObjectElement>(true);
			definitions.m_PrefabRequirementElements = SystemAPI.GetBufferLookup<ObjectRequirementElement>(true);
			definitions.m_PrefabServiceUpgradeBuilding = SystemAPI.GetBufferLookup<ServiceUpgradeBuilding>(true);
			//definitions.m_WaterSurfaceData = _waterSystem.GetSurfaceData(out var _);
			definitions.m_TerrainHeightData = _terrainSystem.GetHeightData();
			definitions.m_CommandBuffer = _toolOutputBarrier.CreateCommandBuffer();
			definitions.Execute();
		}

		public override PrefabBase GetPrefab()
		{
			return null;
		}

		public override bool TrySetPrefab(PrefabBase prefab)
		{
			return false;
		}
	}
}
