using nQuant;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace AssetIconCreator
{
	internal static class IconMakerUtil
	{
		private const int alphaTransparency = 10;
		private const int alphaFader = 70;

		private static readonly WuQuantizer quantizer = new();

		internal static Image LoadImage(Bitmap bitmap)
		{
			if (bitmap == null)
			{
				return null;
			}

			var mask = GetInvisibleMask(bitmap, out var avgHue);

			var width = bitmap.Width;
			var height = bitmap.Height;
			var magentaBackground = avgHue is > 0.6f and < 0.875f;

			for (var x = 0; x < width; x++)
			{
				for (var y = 0; y < height; y++)
				{
					if (mask[x].TryGetValue(y, out var b) && b > 0)
					{
						bitmap.SetPixel(x, y, Color.FromArgb(255 - mask[x][y], ProcessPixel(bitmap.GetPixel(x, y), avgHue, magentaBackground)));
					}
					else
					{
						bitmap.SetPixel(x, y, Color.FromArgb(255, ProcessPixel(bitmap.GetPixel(x, y), avgHue, magentaBackground)));
					}
				}
			}

			var visibleBounds = GetVisibleBounds(bitmap);

			using var tempBitmap = new Bitmap(visibleBounds.Width, visibleBounds.Height);

			using (var graphics = Graphics.FromImage(tempBitmap))
			{
				graphics.DrawImage(bitmap, new Rectangle(new Point(-visibleBounds.X, -visibleBounds.Y), bitmap.Size));
			}

			var padding = Mod.Settings.OutputSize / 64;
			var paddedSize = Mod.Settings.OutputSize - (padding * 2);
			var ratio = Math.Max(visibleBounds.Width, visibleBounds.Height) / (float)paddedSize;
			var processedImage = new Bitmap(Mod.Settings.OutputSize, Mod.Settings.OutputSize);
			var targetRectangle = new Rectangle(
				padding,
				padding,
				(int)(visibleBounds.Width / ratio),
				(int)(visibleBounds.Height / ratio));

			targetRectangle.X += (paddedSize - targetRectangle.Width) / 2;
			targetRectangle.Y += (paddedSize - targetRectangle.Height) / 2;

			using (var graphics = Graphics.FromImage(processedImage))
			{
				graphics.DrawImage(Tint(tempBitmap, 0.035f, 1.035f), targetRectangle);
			}

			if (Mod.Settings.CompressOutput)
			{
				using (processedImage)
				{
					return quantizer.QuantizeImage(processedImage, alphaTransparency, alphaFader);
				}
			}

			return processedImage;
		}

		private static Bitmap Tint(Bitmap bitmap, float lum, float mult)
		{
			if (bitmap == null)
			{
				return null;
			}

			var width = bitmap.Width;
			var height = bitmap.Height;

			for (var i = 0; i < height; i++)
			{
				for (var j = 0; j < width; j++)
				{
					var color = bitmap.GetPixel(j, i);
					RgbToHsl(color, out var cHue, out var cSat, out var cLum);

					bitmap.SetPixel(j, i, System.Drawing.Color.FromArgb(bitmap.GetPixel(j, i).A,
						ColorFromHSL(cHue, cSat, Math.Min(1f, (cLum + lum) * mult))));
				}
			}

			return bitmap;
		}

		private static void RgbToHsl(Color rgbColor, out float hue, out float saturation, out float lightness)
		{
			var r = rgbColor.R / 255.0f;
			var g = rgbColor.G / 255.0f;
			var b = rgbColor.B / 255.0f;

			var cMax = Math.Max(r, Math.Max(g, b));
			var cMin = Math.Min(r, Math.Min(g, b));
			var delta = cMax - cMin;

			// Calculate hue
			if (delta == 0)
			{
				hue = 0; // Undefined (monochromatic)
			}
			else if (cMax == r)
			{
				hue = 60 * ((g - b) / delta % 6);
			}
			else if (cMax == g)
			{
				hue = 60 * (((b - r) / delta) + 2);
			}
			else // cMax == b
			{
				hue = 60 * (((r - g) / delta) + 4);
			}

			// Calculate lightness
			lightness = (cMax + cMin) / 2;

			// Calculate saturation
			if (delta == 0)
			{
				saturation = 0;
			}
			else
			{
				saturation = delta / (1 - Math.Abs((2 * lightness) - 1));
			}

			// Ensure hue is non-negative
			hue = hue < 0 ? hue + 360 : hue;
		}

		private static Color ColorFromHSL(float hue, float saturation, float lightness)
		{
			var chroma = (1 - Math.Abs((2 * lightness) - 1)) * saturation;
			var huePrime = hue / 60.0f;
			var x = chroma * (1 - Math.Abs((huePrime % 2) - 1));

			float r1, g1, b1;
			if (huePrime is >= 0 and < 1)
			{
				r1 = chroma;
				g1 = x;
				b1 = 0;
			}
			else if (huePrime is >= 1 and < 2)
			{
				r1 = x;
				g1 = chroma;
				b1 = 0;
			}
			else if (huePrime is >= 2 and < 3)
			{
				r1 = 0;
				g1 = chroma;
				b1 = x;
			}
			else if (huePrime is >= 3 and < 4)
			{
				r1 = 0;
				g1 = x;
				b1 = chroma;
			}
			else if (huePrime is >= 4 and < 5)
			{
				r1 = x;
				g1 = 0;
				b1 = chroma;
			}
			else // if (huePrime >= 5 && huePrime < 6)
			{
				r1 = chroma;
				g1 = 0;
				b1 = x;
			}

			var m = lightness - (chroma / 2.0);
			var r = (byte)((r1 + m) * 255);
			var g = (byte)((g1 + m) * 255);
			var b = (byte)((b1 + m) * 255);

			return System.Drawing.Color.FromArgb(r, g, b);
		}

		private static Rectangle GetVisibleBounds(Bitmap bitmap)
		{
			var left = bitmap.Width;
			var top = bitmap.Height;
			var right = 0;
			var bottom = 0;

			// Iterate through each pixel in the bitmap
			for (var x = 0; x < bitmap.Width; x++)
			{
				for (var y = 0; y < bitmap.Height; y++)
				{
					var pixelColor = bitmap.GetPixel(x, y);
					if (pixelColor.A != 0) // Non-transparent pixel
					{
						// Update position values if necessary
						left = Math.Min(left, x);
						top = Math.Min(top, y);
						right = Math.Max(right, x);
						bottom = Math.Max(bottom, y);
					}
				}
			}

			return Rectangle.FromLTRB(left, top, right, bottom);
		}

		private static Dictionary<int, Dictionary<int, byte>> GetInvisibleMask(Bitmap bitmap, out float avgHue)
		{
			var height = bitmap.Height;
			var width = bitmap.Width;

			var workedList = new List<Point> { new(0, 0), new(width - 1, 0), new(0, height - 1), new(width - 1, height - 1) };
			var matchList = new List<Point> { new(0, 0), new(width - 1, 0), new(0, height - 1), new(width - 1, height - 1) };
			var invisibleMask = new Dictionary<int, Dictionary<int, byte>>();
			var pixelMatchMap = new Dictionary<int, Dictionary<int, int>>();
			var corners = GetCornerColors(bitmap, height, width);

			avgHue = corners.Average(x => x.Hue / 360f);

			var maxHue = corners.Max(x => x.Hue / 360f);
			var maxBrightness = corners.Max(x => x.Luminance);
			var maxSaturation = corners.Max(x => x.Saturation);

			var minHue = corners.Min(x => x.Hue / 360f);
			var minBrightness = corners.Min(x => x.Luminance);
			var minSaturation = corners.Min(x => x.Saturation);

			for (var x = 0; x < width; x++)
			{
				pixelMatchMap[x] = new();
				invisibleMask[x] = new();

				for (var y = 0; y < height; y++)
				{
					var pixel = bitmap.GetPixel(x, y);

					RgbToHsl(pixel, out var hue, out var saturation, out var brightness);

					hue /= 360f;

					var pixelMap = 0f;

					if (x <= 0 || y <= 0 || x > width - 1 || y > height - 1)
					{
						pixelMatchMap[x][y] = 5;
						continue;
					}

					var margin = 0.06f;
					var hueScore = CalculateScore(minHue, maxHue, hue, margin * 3);
					var brightnessScore = CalculateScore(minBrightness, maxBrightness, brightness, margin);
					var saturationScore = CalculateScore(minSaturation, maxSaturation, saturation, margin);

					pixelMap = hueScore * brightnessScore * saturationScore;

					if (pixelMap >= 1f)
					{
						pixelMatchMap[x][y] = 5;
					}
					else if (pixelMap >= 0.75f)
					{
						pixelMatchMap[x][y] = 4;
					}
					else if (pixelMap >= 0.5f)
					{
						pixelMatchMap[x][y] = 3;
					}
					else if (pixelMap >= 0.25f)
					{
						pixelMatchMap[x][y] = 2;
					}
					else if (pixelMap > 0f)
					{
						pixelMatchMap[x][y] = 1;
					}
					else
					{
						pixelMatchMap[x][y] = 0;
					}
				}
			}

			for (var x = 0; x < width; x++)
			{
				for (var y = 0; y < height; y++)
				{
					switch (pixelMatchMap[x][y])
					{
						case 5:
							invisibleMask[x][y] = 255;
							break;
						case 4:
							invisibleMask[x][y] = 255;
							fillArea(x, y, 150);
							break;
						case 3:
							invisibleMask[x][y] = 175;
							fillArea(x, y, 125);
							break;
						case 2:
							fillArea(x, y, 100);
							break;
						case 1:
							invisibleMask[x][y] = (byte)Math.Min(255, (invisibleMask[x].TryGetValue(y, out var b) ? b : 0) + 75);
							break;
					}
				}
			}

			var tempInvisibleMask = new Dictionary<int, Dictionary<int, byte>>();

			for (var x = 0; x < width; x++)
			{
				tempInvisibleMask[x] = new();

				for (var y = 0; y < height; y++)
				{
					tempInvisibleMask[x][y] = invisibleMask[x].TryGetValue(y, out var b) ? b : default;
				}
			}

			var radius = Mod.Settings.CompressOutput 
				? Math.Max(0.5f, (height / 600f) + 0.4f)
				: Math.Max(0.5f, (height / 600f) - 0.6f);

			for (var x = 0; x < width; x++)
			{
				for (var y = 0; y < height; y++)
				{
					if (tempInvisibleMask[x][y] > 25)
					{
						fillArea(x, y, 25, radius);
					}
				}
			}

			return invisibleMask;

			void fillArea(int x, int y, byte amount, float range = 1)
			{
				if (amount == 0)
				{
					return;
				}

				var ceil = (int)Math.Ceiling(range);

				for (var i = -ceil; i <= ceil; i++)
				{
					var iDiff = Math.Max(0, Math.Min(1, 1 - (Math.Abs(i) - range)));

					for (var j = -ceil; j <= ceil; j++)
					{
						var jDiff = Math.Max(0, Math.Min(1, 1 - (Math.Abs(j) - range)));

						if (x + i >= 0 && x + i < width && y + j >= 0 && y + j < height)
						{
							invisibleMask[x + i][y + j] = (byte)Math.Min(255, (invisibleMask[x + i].TryGetValue(y + j, out var b) ? b : 0) + (amount * jDiff * iDiff));
						}
					}
				}
			}
		}

		private static (float Hue, float Saturation, float Luminance)[] GetCornerColors(Bitmap bitmap, int height, int width)
		{
			var array = new (float Hue, float Saturation, float Luminance)[4];

			RgbToHsl(bitmap.GetPixel(0, 0), out var h, out var s, out var l);
			array[0] = (h, s, l);

			RgbToHsl(bitmap.GetPixel(width - 1, 0), out h, out s, out l);
			array[1] = (h, s, l);

			RgbToHsl(bitmap.GetPixel(0, height - 1), out h, out s, out l);
			array[2] = (h, s, l);

			RgbToHsl(bitmap.GetPixel(width - 1, height - 1), out h, out s, out l);
			array[3] = (h, s, l);

			return array;
		}

		private static float CalculateScore(float minValue, float maxValue, float value, float errorMargin)
		{
			var range = maxValue - minValue;
			var minRange = minValue - errorMargin;
			var maxRange = maxValue + errorMargin;

			if (value < minRange || value > maxRange)
			{
				return 0f; // Outside the range including margin of error
			}

			if (value >= minValue || value <= maxValue)
			{
				return 1f; // Outside the range including margin of error
			}

			if (value < minValue)
			{
				return (minValue - value) / errorMargin;
			}

			return (value - maxValue) / errorMargin;
		}

		private static Color ProcessPixel(Color color, float backgroundHue, bool magentaBackground)
		{
			RgbToHsl(color, out var hue, out var sat, out var lit);

			if (magentaBackground)
			{
				var score = !magentaBackground ? CalculateScore((backgroundHue * 360f) - 10f, (backgroundHue * 360f) + 10f, hue, 35f)
					: CalculateScore(290f, 320f, hue, 20f);

				return HSLToRGB((hue + (-20 * score)) % 360, sat * (1 - (score * 7 / 10)), Math.Min(1f, lit + (0.033f * (1 - (score * 9 / 10)))));
			}

			return HSLToRGB(hue, sat, Math.Min(1f, lit + 0.033f));
		}

		private static Color HSLToRGB(float hue, float saturation, float lightness)
		{
			var chroma = (1 - Math.Abs((2 * lightness) - 1)) * saturation;
			var huePrime = hue / 60.0f;
			var x = chroma * (1 - Math.Abs((huePrime % 2) - 1));

			float r1, g1, b1;
			if (huePrime is >= 0 and < 1)
			{
				r1 = chroma;
				g1 = x;
				b1 = 0;
			}
			else if (huePrime is >= 1 and < 2)
			{
				r1 = x;
				g1 = chroma;
				b1 = 0;
			}
			else if (huePrime is >= 2 and < 3)
			{
				r1 = 0;
				g1 = chroma;
				b1 = x;
			}
			else if (huePrime is >= 3 and < 4)
			{
				r1 = 0;
				g1 = x;
				b1 = chroma;
			}
			else if (huePrime is >= 4 and < 5)
			{
				r1 = x;
				g1 = 0;
				b1 = chroma;
			}
			else // if (huePrime >= 5 && huePrime < 6)
			{
				r1 = chroma;
				g1 = 0;
				b1 = x;
			}

			var m = lightness - (chroma / 2.0);
			var r = (byte)((r1 + m) * 255);
			var g = (byte)((g1 + m) * 255);
			var b = (byte)((b1 + m) * 255);

			return Color.FromArgb(r, g, b);
		}
	}
}
