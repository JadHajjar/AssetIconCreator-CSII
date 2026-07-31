using AssetIconCreator;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IconProcessingTest
{
	public partial class Form1 : Form
	{
		private string _currentFile;
		private Image _resultImage;
		private Dictionary<string, Image> _heatMaps = new Dictionary<string, Image>();

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
				var heatMaps = new Dictionary<string, Image>();

				var result = await Task.Run(() =>
				{
					Image output;

					using (var bitmap = LoadBitmap(_currentFile))
					{
						output = IconMakerUtil.LoadImage(bitmap);
					}

#if DEBUG
					foreach (var map in IconMakerUtil.DebugMaps)
					{
						heatMaps[map.Key] = RenderHeatMap(map.Value);
					}

					IconMakerUtil.DebugMaps.Clear();
#endif

					return output;
				});

				sw.Stop();

				SetResultImages(result, heatMaps);

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

		private void SetResultImages(Image result, Dictionary<string, Image> heatMaps)
		{
			var selected = comboView.SelectedItem as string ?? "Result";

			pictureBoxResult.Image = null;

			_resultImage?.Dispose();

			foreach (var map in _heatMaps.Values)
			{
				map.Dispose();
			}

			_resultImage = result;
			_heatMaps = heatMaps;

			comboView.Items.Clear();
			comboView.Items.Add("Result");

			foreach (var name in heatMaps.Keys)
			{
				comboView.Items.Add(name);
			}

			comboView.SelectedItem = comboView.Items.Contains(selected) ? selected : "Result";
		}

		private void comboView_SelectedIndexChanged(object sender, EventArgs e)
		{
			var key = comboView.SelectedItem as string;

			pictureBoxResult.Image = key != null && _heatMaps.TryGetValue(key, out var map) ? map : _resultImage;
		}

		private static Bitmap RenderHeatMap(float[,] values)
		{
			var width = values.GetLength(0);
			var height = values.GetLength(1);
			var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			var buffer = new byte[data.Stride * height];

			for (var y = 0; y < height; y++)
			{
				var row = y * data.Stride;

				for (var x = 0; x < width; x++)
				{
					var color = HeatColor(values[x, y]);
					var i = row + (x * 4);

					buffer[i] = color.B;
					buffer[i + 1] = color.G;
					buffer[i + 2] = color.R;
					buffer[i + 3] = 255;
				}
			}

			Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
			bitmap.UnlockBits(data);

			return bitmap;
		}

		private static Color HeatColor(float value)
		{
			var gray = (int)(Math.Max(0f, Math.Min(1f, value)) * 255);

			return Color.FromArgb(255, gray, gray, gray);
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
