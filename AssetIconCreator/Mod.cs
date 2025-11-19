using Colossal.Core;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Colossal.UI;

using Game;
using Game.Modding;
using Game.SceneFlow;

using System.IO;

using UnityEngine;

namespace AssetIconCreator
{
	public class Mod : IMod
	{
		public static ILog Log { get; } = LogManager.GetLogger(nameof(AssetIconCreator)).SetShowsErrorsInUI(false);
		public static Texture2D Magenta { get; private set; }
		public static Setting Settings { get; private set; }
		public static string Id { get; } = nameof(AssetIconCreator);
		public static string ContentFolder { get; } = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(AssetIconCreator), "Temp");

		public void OnLoad(UpdateSystem updateSystem)
		{
			Log.Info(nameof(OnLoad));

			if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
			{
				Magenta = new Texture2D(4096, 4096);
				Magenta.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(asset.path), "Magenta.png")));

				var contentDir = new DirectoryInfo(ContentFolder);

				if (contentDir.Exists)
				{
					contentDir.Delete(true);
				}

				contentDir.Create();

				File.Copy(Path.Combine(Path.GetDirectoryName(asset.path), "AIC_Icon.png"), Path.Combine(contentDir.FullName, "AIC_Icon.png"), true);

				AssetIconCreatorEditorTool.Thumbnail = "coui://aic/AIC_Icon.png";
			}

			Settings = new Setting(this);
			Settings.RegisterInOptionsUI();
			GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));

			Settings.RegisterKeyBindings();

			AssetDatabase.global.LoadSettings(nameof(AssetIconCreator), Settings, new Setting(this));

			updateSystem.UpdateAt<AssetSetupToolSystem>(SystemUpdatePhase.ToolUpdate);
			updateSystem.UpdateAt<AssetCreatorUISystem>(SystemUpdatePhase.UIUpdate);

			GameManager.instance.RegisterUpdater(RegisterHostLocation);
		}

		private void RegisterHostLocation()
		{
			UIManager.defaultUISystem.AddHostLocation("aic", ContentFolder, true);

			Log.Info("Registered UI Host Location");
		}

		public void OnDispose()
		{
			Log.Info(nameof(OnDispose));

			var tempDir = new DirectoryInfo(ContentFolder);

			if (tempDir.Exists)
			{
				tempDir.Delete(true);
			}

			if (Settings != null)
			{
				Settings.UnregisterInOptionsUI();
				Settings = null;
			}
		}
	}
}
