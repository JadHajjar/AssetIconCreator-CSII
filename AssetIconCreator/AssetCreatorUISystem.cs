using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetIconCreator
{
	internal partial class AssetCreatorUISystem : ExtendedUISystemBase
	{
		private AssetSetupToolSystem _assetSetupToolSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			_assetSetupToolSystem = World.GetOrCreateSystemManaged<AssetSetupToolSystem>();

			CreateBinding("InProcess", () => _assetSetupToolSystem.ScreenshotUtility?.InProcess ?? false);
			CreateBinding("SettingUp", () => _assetSetupToolSystem.ScreenshotUtility?.SettingUp ?? false);
			CreateBinding("ProgressText", () => _assetSetupToolSystem.ScreenshotUtility?.ProgressText);
			CreateBinding("ResultThumbnail", () => _assetSetupToolSystem.ScreenshotUtility?.ResultThumbnail);
		}
	}
}
