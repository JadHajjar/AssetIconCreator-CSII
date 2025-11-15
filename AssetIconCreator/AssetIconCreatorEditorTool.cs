using Game.Tools;
using Game.UI.Editor;
using Game.UI.InGame;

using Unity.Entities;

namespace AssetIconCreator
{
	internal class AssetIconCreatorEditorTool : EditorTool
	{
		private readonly AssetCreatorUISystem _assetCreatorUISystem;
		private ToolSystem _baseToolSystem;

		public static string Thumbnail { get; internal set; }

		public AssetIconCreatorEditorTool(World world, AssetSetupToolSystem toolSystem, ToolSystem baseToolSystem) : base(world)
		{
			_baseToolSystem = baseToolSystem;

			id = toolSystem.toolID;
			icon = Thumbnail;
			tool = toolSystem;
		}

		protected override void OnEnable()
		{
			_baseToolSystem.activeTool = tool;
			//_assetCreatorUISystem.ToggleTool(true);
		}

		protected override void OnDisable()
		{
			//_assetCreatorUISystem.ToggleTool(false);
		}
	}
}
