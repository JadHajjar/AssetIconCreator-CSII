// stand-ins for the game-dependent classes IconMakerUtil references
namespace AssetIconCreator
{
	internal static class Mod
	{
		public static Setting Settings { get; } = new Setting();
	}

	internal class Setting
	{
		public int OutputSize { get; set; } = 256;
		public bool CompressOutput { get; set; }
	}
}
