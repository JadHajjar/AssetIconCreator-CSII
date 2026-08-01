using Game.Tools;
using Game.UI.Localization;
using Game.UI.Tooltip;

using Unity.Entities;

namespace AssetIconCreator
{
	internal partial class AssetSetupTooltipSystem : TooltipSystemBase
	{
		public const string TOOLTIP_KEY = "AssetIconCreator.Tooltip";
		public const string FLIP_TOOLTIP_KEY = "AssetIconCreator.Tooltip[Flip]";
		public const string LIFT_TOOLTIP_KEY = "AssetIconCreator.Tooltip[Lift]";

		private ToolSystem _toolSystem;
		private AssetSetupToolSystem _assetSetupToolSystem;
		private StringTooltip flipTooltip;
		private StringTooltip liftTooltip;
		private StringTooltip tooltip;

		protected override void OnCreate()
		{
			base.OnCreate();

			_toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
			_assetSetupToolSystem = World.GetOrCreateSystemManaged<AssetSetupToolSystem>();

			tooltip = new StringTooltip { icon = "Media/Mouse/LMB.svg", path = "assetIconCreator", value = LocalizedString.Id(TOOLTIP_KEY) };
			flipTooltip = new StringTooltip { path = "assetIconCreatorFlip", value = LocalizedString.Id(FLIP_TOOLTIP_KEY) };
			liftTooltip = new StringTooltip { path = "assetIconCreatorLift", value = LocalizedString.Id(LIFT_TOOLTIP_KEY) };
		}

		protected override void OnUpdate()
		{
			if (_toolSystem.activeTool != _assetSetupToolSystem || _assetSetupToolSystem.ScreenshotUtility.SettingUp)
			{
				return;
			}

			AddMouseTooltip(tooltip);
			AddMouseTooltip(flipTooltip);
			AddMouseTooltip(liftTooltip);
		}
	}
}
