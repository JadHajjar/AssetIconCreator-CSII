using AssetIconCreator;

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IconProcessingTest
{
	public partial class Form1 : Form
	{
		private string _currentFile;

		public Form1()
		{
			InitializeComponent();

			openFileDialog.InitialDirectory = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				"AppData", "LocalLow", "Colossal Order", "Cities Skylines II", "ModsData", "AssetIconCreator");
		}

		private async void buttonSelect_Click(object sender, EventArgs e)
		{
			if (openFileDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			_currentFile = openFileDialog.FileName;

			await ProcessCurrentFile();
		}

		private async void buttonRefresh_Click(object sender, EventArgs e)
		{
			await ProcessCurrentFile();
		}

		private async Task ProcessCurrentFile()
		{
			if (_currentFile == null)
			{
				return;
			}

			buttonSelect.Enabled = buttonRefresh.Enabled = false;
			labelStatus.Text = "Processing...";

			try
			{
				SetImage(pictureBoxSource, LoadBitmap(_currentFile));

				var sw = Stopwatch.StartNew();

				var result = await Task.Run(() =>
				{
					using (var bitmap = LoadBitmap(_currentFile))
					{
						return IconMakerUtil.LoadImage(bitmap);
					}
				});

				sw.Stop();

				SetImage(pictureBoxResult, result);

				labelStatus.Text = $"Processed in {sw.Elapsed.TotalSeconds:0.00}s";
			}
			catch (Exception ex)
			{
				labelStatus.Text = ex.Message;
			}
			finally
			{
				buttonSelect.Enabled = true;
				buttonRefresh.Enabled = _currentFile != null;
			}
		}

		// loads a detached copy so the file isn't locked and can be overwritten between refreshes
		private static Bitmap LoadBitmap(string path)
		{
			using (var ms = new MemoryStream(File.ReadAllBytes(path)))
			using (var temp = new Bitmap(ms))
			{
				return new Bitmap(temp);
			}
		}

		private static void SetImage(PictureBox box, Image image)
		{
			box.Image?.Dispose();
			box.Image = image;
		}
	}
}
