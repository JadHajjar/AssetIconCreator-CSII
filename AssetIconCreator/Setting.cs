using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Environment;

using Game.Input;
using Game.Modding;
using Game.Settings;
using Game.UI;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AssetIconCreator
{
	[FileLocation(nameof(AssetIconCreator))]
	[SettingsUIGroupOrder(MAIN_GROUP, OUTPUT_GROUP, SAVING_GROUP)]
	[SettingsUIShowGroupName(OUTPUT_GROUP, SAVING_GROUP)]
	public class Setting : ModSetting
	{
		public const string MAIN_SECTION = "Main";
		public const string MAIN_GROUP = "Settings";
		public const string OUTPUT_GROUP = "Output";
		public const string SAVING_GROUP = "Saving";

		private string _thumbnailsFolder;
		private bool _autoSetIcon = true;

		public Setting(IMod mod) : base(mod)
		{

		}

		[SettingsUIKeyboardBinding(BindingKeyboard.Q, nameof(ToolKeyBinding), shift: true)]
		[SettingsUISection(MAIN_SECTION, MAIN_GROUP)]
		public ProxyBinding ToolKeyBinding { get; set; }

		[SettingsUISection(MAIN_SECTION, MAIN_GROUP)]
		public bool ClearMap { get; set; } = true;

		[SettingsUISlider(min = 128, max = 1024, step = 128, scalarMultiplier = 1, unit = Unit.kInteger)]
		[SettingsUISection(MAIN_SECTION, OUTPUT_GROUP)]
		public int OutputSize { get; set; } = 256;

		[SettingsUISection(MAIN_SECTION, OUTPUT_GROUP)]
		public bool CompressOutput { get; set; }

		[SettingsUISection(MAIN_SECTION, SAVING_GROUP)]
		public bool SaveThumbnailsPermanently { get; set; }

		[SettingsUISection(MAIN_SECTION, SAVING_GROUP)]
		[SettingsUIHideByCondition(typeof(Setting), nameof(HideFolderButton))]
		public bool AutoSetIcon { get => _autoSetIcon || !SaveThumbnailsPermanently; set => _autoSetIcon = value; }

		[SettingsUISection(MAIN_SECTION, SAVING_GROUP)]
		[SettingsUIDirectoryPicker]
		[SettingsUIHideByCondition(typeof(Setting), nameof(HideFolderButton))]
		public string ThumbnailsFolder
		{
			get => Directory.Exists(_thumbnailsFolder) ? _thumbnailsFolder : Path.Combine(EnvPath.kUserDataPath, "ModsData", "AssetIconCreator");
			set => _thumbnailsFolder = value;
		}

		[SettingsUISection(MAIN_SECTION, SAVING_GROUP)]
		[SettingsUIHideByCondition(typeof(Setting), nameof(HideFolderButton))]
		public bool OpenFolder
		{
			set
			{
				Directory.CreateDirectory(ThumbnailsFolder);
				Process.Start(ThumbnailsFolder);
			}
		}

		public override void SetDefaults()
		{
		}

		public static bool HideFolderButton()
		{
			return !Mod.Settings.SaveThumbnailsPermanently;
		}
	}

	public class LocaleEN : IDictionarySource
	{
		private readonly Setting m_Setting;
		public LocaleEN(Setting setting)
		{
			m_Setting = setting;
		}
		public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
		{
			return new Dictionary<string, string>
			{
				{ $"Editor.TOOL[{nameof(AssetSetupToolSystem)}]", "Asset Icon Creator" },
				{ m_Setting.GetSettingsLocaleID(), "Asset Icon Creator" },
				{ m_Setting.GetOptionTabLocaleID(Setting.MAIN_SECTION), "Main" },

				{ m_Setting.GetOptionGroupLocaleID(Setting.MAIN_GROUP), "Settings" },
				{ m_Setting.GetOptionGroupLocaleID(Setting.OUTPUT_GROUP), "Output" },
				{ m_Setting.GetOptionGroupLocaleID(Setting.SAVING_GROUP), "Saving" },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearMap)), "Clear Scene" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearMap)), $"Clears the scene from any object other than the asset that you're making a icon for." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.AutoSetIcon)), "Automatically set up UI Object component" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.AutoSetIcon)), $"Automatically add or update the UI Object component of the asset with the new icon." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.CompressOutput)), "Compress icon" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.CompressOutput)), $"Compresses the generated icon to have smaller file size at the expense of worse transparency at the edges." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.ThumbnailsFolder)), "Permanent Folder" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.ThumbnailsFolder)), $"Determines the folder where icons are kept for later use." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.SaveThumbnailsPermanently)), "Keep a copy of generated icons" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.SaveThumbnailsPermanently)), $"Saves a copy of generated icons in the permanent folder for easy access." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.OutputSize)), "Output Image Size" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.OutputSize)), $"Changes the size in pixels of the generated icon. Larger values may not always result in better quality based on your screen resolution." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenFolder)), "Open Icons Folder" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenFolder)), $"Open the folder where icons are saved in Explorer." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.ToolKeyBinding)), "Tool hotkey" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.ToolKeyBinding)), $"Keyboard binding to toggle the tool" },
				{ m_Setting.GetBindingKeyLocaleID(nameof(Setting.ToolKeyBinding)), "Activate Tool" },

				{ m_Setting.GetBindingMapLocaleID(), "Asset Icon Creator" },
			};
		}

		public void Unload()
		{

		}
	}
}
