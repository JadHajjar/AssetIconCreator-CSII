using Colossal.Core;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Environment;

using Game;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using Game.Tools;
using Game.UI.InGame;

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using UnityEngine;

namespace AssetIconCreator
{
	internal class ScreenshotUtility
	{
		private readonly SimulationSystem _simulationSystem;
		private readonly RenderingSystem _renderingSystem;
		private readonly ToolRaycastSystem _toolRaycastSystem;
		private readonly CameraUpdateSystem _cameraUpdateSystem;
		private readonly PrefabSystem _prefabSystem;
		private readonly DefaultToolSystem _defaultToolSystem;
		private readonly ToolSystem _toolSystem;
		private readonly PrefabUISystem _prefabUISystem;

		public bool SettingUp { get; private set; }
		public bool InProcess { get; private set; }
		public string ProgressText { get; private set; }
		public string ResultThumbnail { get; private set; }

		internal ScreenshotUtility(SimulationSystem simulationSystem, RenderingSystem renderingSystem, ToolRaycastSystem toolRaycastSystem, CameraUpdateSystem cameraUpdateSystem, PrefabSystem prefabSystem, DefaultToolSystem defaultToolSystem, ToolSystem toolSystem)
		{
			_simulationSystem = simulationSystem;
			_renderingSystem = renderingSystem;
			_toolRaycastSystem = toolRaycastSystem;
			_cameraUpdateSystem = cameraUpdateSystem;
			_prefabSystem = prefabSystem;
			_defaultToolSystem = defaultToolSystem;
			_toolSystem = toolSystem;
			_prefabUISystem = toolSystem.World.GetOrCreateSystemManaged<PrefabUISystem>();
		}

		internal IEnumerator CaptureScreenshot(PrefabBase prefab, GameMode mode, float currentFov, float simSpeed)
		{
			SettingUp = true;
			InProcess = true;
			ResultThumbnail = null;
			ProgressText = "Preparing Scene...";

			yield return new WaitForSeconds(0.15f);

			typeof(ToolSystem).GetProperty("actionMode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).SetValue(_toolSystem, mode);

			_simulationSystem.selectedSpeed = 1f;

			var currentTextures = LoadTextureInGame();
			var graphicSettings = SharedSettings.instance.graphics.qualitySettings;

			SetGraphicSettings();

			_renderingSystem.disableLodModels = true;

			yield return new WaitForSeconds(1.5f);

			var ui = GameManager.instance.userInterface;
			if (ui != null)
			{
				ui.view.enabled = false;
			}

			SetOverlayHidden(true);

			yield return new WaitForEndOfFrame();

			var texture2D = ScreenCapture.CaptureScreenshotAsTexture(SharedSettings.instance.graphics.resolution.height < Mod.Settings.OutputSize * 2 ? 2 : 1);

			SettingUp = false;
			ProgressText = "Processing image...";

			if (ui != null)
			{
				ui.view.enabled = true;
			}

			_cameraUpdateSystem.orbitCameraController.lens.FieldOfView = currentFov;
			_cameraUpdateSystem.activeCameraController = _cameraUpdateSystem.gamePlayController;

			SetOverlayHidden(false);

			_simulationSystem.selectedSpeed = simSpeed;

			UnloadTextureInGame(currentTextures);

			_renderingSystem.disableLodModels = false;

			SharedSettings.instance.graphics.qualitySettings = graphicSettings;
			SharedSettings.instance.graphics.Apply();

			_toolSystem.activeTool = _defaultToolSystem;

			yield return new WaitForEndOfFrame();

			Task.Run(() => ProcessScreenshot(prefab, texture2D));
		}

		private void ProcessScreenshot(PrefabBase prefab, Texture2D texture2D)
		{
			Mod.Log.Info($"Processing Icon");

			using var ms = new MemoryStream(texture2D.EncodeToPNG());
			using var bitmap = new Bitmap(ms);

			var sw = Stopwatch.StartNew();
			var folder = Mod.ContentFolder;
			var fileName = $"{System.Guid.NewGuid()}.png";

			try
			{
				using var output = IconMakerUtil.LoadImage(bitmap);

				sw.Stop();
				Mod.Log.Info($"Icon generated in {sw.Elapsed.TotalSeconds}s");

				ProgressText = "Finalizing thumbnail...";

				Directory.CreateDirectory(folder);

				output.Save(Path.Combine(folder, fileName));

				if (Mod.Settings.SaveThumbnailsPermanently)
				{
					var folderPermanent = Mod.Settings.ThumbnailsFolder;

					Directory.CreateDirectory(folderPermanent);

					output.Save(Path.Combine(folderPermanent, $"{prefab.GetType().Name}.{prefab.name}.png"));
				}
			}
			catch (System.Exception ex)
			{
				InProcess = false;
				Mod.Log.Error(ex);
				return;
			}

			if (!AssetDatabase.global.TryGetDatabase("User", out var database) || database is not ILocalAssetDatabase localAssetDatabase)
			{
				InProcess = false;
				Mod.Log.Error("Failed to get User database to import thumbnail");
				return;
			}

			GameManager.instance.RegisterUpdater(() =>
			{
				Object.DestroyImmediate(texture2D);

				var asset = localAssetDatabase.AddAsset(AssetDataPath.Create(Path.Combine("ModsData", nameof(AssetIconCreator), "Temp"), fileName, true, EscapeStrategy.None));
				var icon = $"assetdb://global/{asset.id.guid}";

				if (Mod.Settings.AutoSetIcon)
				{
					if (prefab.TryGet<UIObject>(out var uIObject))
					{
						uIObject.m_Icon = icon;
					}
					else
					{
						uIObject = prefab.AddComponent<UIObject>();
						uIObject.m_Priority = 1;
						uIObject.m_Icon = icon;
					}

					_prefabSystem.UpdatePrefab(prefab);

					Mod.Log.Info($"Changed {prefab.name} icon to 'assetdb://global/{asset.id.guid}'");
				}

				ProgressText = GetAssetName(prefab) + " Thumbnail";
				ResultThumbnail = "coui://aic/" + fileName;

				Task.Run(async () =>
				{
					await Task.Delay(6000);

					InProcess = false;
				});
			});
		}

		private static void SetGraphicSettings()
		{
			var volume = typeof(GraphicsSettings).GetField("m_VolumeOverride", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null) as UnityEngine.Rendering.Volume;

			var list = new List<QualitySetting>
			{
				new CloudsQualitySettings(QualitySetting.Level.Disabled, volume.profile),
				new ShadowsQualitySettings(QualitySetting.Level.Disabled, volume.profile),
				new FogQualitySettings(QualitySetting.Level.Disabled, volume.profile),
				new VolumetricsQualitySettings(QualitySetting.Level.Disabled, volume.profile),
				new SSGIQualitySettings(QualitySetting.Level.Disabled, volume.profile),
				new SSAOQualitySettings(QualitySetting.Level.Disabled, volume.profile),
				new SSRQualitySettings(QualitySetting.Level.Disabled, volume.profile),
				new MotionBlurQualitySettings(QualitySetting.Level.Disabled, volume.profile),
				new DynamicResolutionScaleSettings(QualitySetting.Level.Disabled),
				new AntiAliasingQualitySettings(QualitySetting.Level.Low)
			};

			var field = typeof(GlobalQualitySettings).GetField("m_QualitySettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var qualityList = (field.GetValue(SharedSettings.instance.graphics) as List<QualitySetting>).ToList();

			for (var i = 0; i < qualityList.Count; i++)
			{
				foreach (var item in list)
				{
					if (qualityList[i].GetType() == item.GetType())
					{
						qualityList[i] = item;
						break;
					}
				}
			}

			field.SetValue(SharedSettings.instance.graphics, qualityList);

			SharedSettings.instance.graphics.Apply();
		}

		private static Dictionary<string, Texture> LoadTextureInGame()
		{
			var dic = new Dictionary<string, Texture>();

			foreach (var shaderProperty in new string[]
			{
				"colossal_TerrainGrassDiffuse",
				"colossal_TerrainGrassNormal",
				"colossal_TerrainDirtDiffuse",
				"colossal_TerrainDirtNormal",
				"colossal_TerrainRockDiffuse",
				"colossal_TerrainRockNormal"
			})
			{
				dic[shaderProperty] = Shader.GetGlobalTexture(Shader.PropertyToID(shaderProperty));

				Shader.SetGlobalTexture(Shader.PropertyToID(shaderProperty), Mod.Magenta);
			}

			return dic;
		}

		private static void UnloadTextureInGame(Dictionary<string, Texture> dictionary)
		{
			foreach (var kvp in dictionary)
			{
				Shader.SetGlobalTexture(Shader.PropertyToID(kvp.Key), kvp.Value);
			}
		}

		private void SetOverlayHidden(bool overlayHidden)
		{
			_renderingSystem.hideOverlay = overlayHidden;

			if (overlayHidden)
			{
				_toolRaycastSystem.raycastFlags |= RaycastFlags.FreeCameraDisable;
				_toolSystem.activeTool = _defaultToolSystem;
			}
			else
			{
				_toolRaycastSystem.raycastFlags &= ~RaycastFlags.FreeCameraDisable;
			}
		}

		private string GetAssetName(PrefabBase prefab)
		{
			_prefabUISystem.GetTitleAndDescription(_prefabSystem.GetEntity(prefab), out var titleId, out var _);

			if (GameManager.instance.localizationManager.activeDictionary.TryGetValue(titleId, out var name))
			{
				return name;
			}

			return Regex.Replace(Regex.Replace(prefab.name.Replace('_', ' '),
				@"([a-z])([A-Z])", x => $"{x.Groups[1].Value} {x.Groups[2].Value}"),
				@"(\b)(?<!')([a-z])", x => $"{x.Groups[1].Value}{x.Groups[2].Value.ToUpper()}");

		}
	}
}