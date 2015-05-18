// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace ICSharpCode.ILSpy
{
	// Fixes the VS2013 images to work with any theme. Same algo as VS2015 itself uses...
	public static class ThemedImageCreator
	{
		public struct HslColor
		{
			public readonly double Hue;
			public readonly double Saturation;
			public readonly double Luminosity;
			public readonly double Alpha;

			public HslColor(double hue, double saturation, double luminosity, double alpha) {
				Hue = hue < 0 ? 0 : hue > 360 ? 360 : hue;
				Saturation = saturation < 0 ? 0 : saturation > 1 ? 1 : saturation;
				Luminosity = luminosity < 0 ? 0 : luminosity > 1 ? 1 : luminosity;
				Alpha = alpha < 0 ? 0 : alpha > 1 ? 1 : alpha;
			}

			public static HslColor FromColor(Color color)
			{
				unchecked {
					byte n1 = Math.Max(color.R, Math.Max(color.G, color.B));
					byte n2 = Math.Min(color.R, Math.Min(color.G, color.B));
					double n3 = (double)(n1 - n2);
					double n4 = (double)n1 / 255.0;
					double n5 = (double)n2 / 255.0;
					double hue;
					if (n1 == n2)
						hue = 0.0;
					else if (n1 == color.R)
						hue = (double)((int)(60.0 * (double)(color.G - color.B) / n3 + 360.0) % 360);
					else if (n1 == color.G)
						hue = 60.0 * (double)(color.B - color.R) / n3 + 120.0;
					else
						hue = 60.0 * (double)(color.R - color.G) / n3 + 240.0;
					double alpha = (double)color.A / 255.0;
					double luminosity = 0.5 * (n4 + n5);
					double saturation;
					if (n1 == n2)
						saturation = 0.0;
					else if (luminosity <= 0.5)
						saturation = (n4 - n5) / (2.0 * luminosity);
					else
						saturation = (n4 - n5) / (2.0 - 2.0 * luminosity);
					return new HslColor(hue, saturation, luminosity, alpha);
				}
			}

			public Color ToColor()
			{
				unchecked {
					double n1 = (Luminosity < 0.5) ? (Luminosity * (1.0 + Saturation)) : (Luminosity + Saturation - Luminosity * Saturation);
					double p = 2.0 * Luminosity - n1;
					double tC2 = Hue / 360.0;
					double tC = HslColor.ModOne(tC2 + 1.0 / 3.0);
					double tC3 = HslColor.ModOne(tC2 - 1.0 / 3.0);
					byte r = (byte)(HslColor.ComputeRGBComponent(p, n1, tC) * 255.0);
					byte g = (byte)(HslColor.ComputeRGBComponent(p, n1, tC2) * 255.0);
					byte b = (byte)(HslColor.ComputeRGBComponent(p, n1, tC3) * 255.0);
					return Color.FromArgb((byte)(Alpha * 255.0), r, g, b);
				}
			}

			private static double ModOne(double value)
			{
				unchecked {
					if (value < 0.0)
						return value + 1.0;
					if (value > 1.0)
						return value - 1.0;
					return value;
				}
			}

			private static double ComputeRGBComponent(double p, double q, double tC)
			{
				unchecked {
					if (tC < 1.0 / 6.0)
						return p + (q - p) * 6.0 * tC;
					if (tC < 0.5)
						return q;
					if (tC < 2.0 / 3.0)
						return p + (q - p) * 6.0 * (2.0 / 3.0 - tC);
					return p;
				}
			}
		}

		public static BitmapSource CreateThemedBitmapSource(BitmapSource src, Color bgColor)
		{
			unchecked {
				if (src.Format != PixelFormats.Bgra32)
					src = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0.0);
				var pixels = new byte[src.PixelWidth * src.PixelHeight * 4];
				src.CopyPixels(pixels, src.PixelWidth * 4, 0);

				var bg = HslColor.FromColor(bgColor);
				int offs = 0;
				while (offs + 4 <= pixels.Length) {
					var hslColor = HslColor.FromColor(Color.FromRgb(pixels[offs + 2], pixels[offs + 1], pixels[offs]));
					double hue = hslColor.Hue;
					double saturation = hslColor.Saturation;
					double luminosity = hslColor.Luminosity;
					double n1 = Math.Abs(0.96470588235294119 - luminosity);
					double n2 = Math.Max(0.0, 1.0 - saturation * 4.0) * Math.Max(0.0, 1.0 - n1 * 4.0);
					double n3 = Math.Max(0.0, 1.0 - saturation * 4.0);
					luminosity = TransformLuminosity(hue, saturation, luminosity, bg.Luminosity);
					hue = hue * (1.0 - n3) + bg.Hue * n3;
					saturation = saturation * (1.0 - n2) + bg.Saturation * n2;
					if (SystemParameters.HighContrast)
						luminosity = ((luminosity <= 0.3) ? 0.0 : ((luminosity >= 0.7) ? 1.0 : ((luminosity - 0.3) / 0.4))) * (1.0 - saturation) + luminosity * saturation;
					var color = new HslColor(hue, saturation, luminosity, 1.0).ToColor();
					pixels[offs + 2] = color.R;
					pixels[offs + 1] = color.G;
					pixels[offs] = color.B;
					offs += 4;
				}

				BitmapSource newImage = BitmapSource.Create(src.PixelWidth, src.PixelHeight, src.DpiX, src.DpiY, PixelFormats.Bgra32, src.Palette, pixels, src.PixelWidth * 4);
				newImage.Freeze();
				return newImage;
			}
		}

		static double TransformLuminosity(double hue, double sat, double lum, double bgLum)
		{
			unchecked {
				if (bgLum < 0.5) {
					if (lum >= 0.96470588235294119)
						return bgLum * (lum - 1.0) / -0.035294117647058809;
					double val = sat < 0.2 ? 1.0 :
								sat > 0.3 ? 0.0 :
								1.0 - (sat - 0.2) / 0.099999999999999978;
					double n1 = Math.Max(Math.Min(1.0, Math.Abs(hue - 37.0) / 20.0), val);
					double n2 = ((bgLum - 1.0) * 0.66 / 0.96470588235294119 + 1.0) * n1 + 0.66 * (1.0 - n1);
					return lum < 0.66 ?
							(n2 - 1.0) / 0.66 * lum + 1.0 :
							(n2 - bgLum) / -0.30470588235294116 * (lum - 0.96470588235294119) + bgLum;
				}
				else {
					return lum < 0.96470588235294119 ?
						lum * bgLum / 0.96470588235294119 :
						(1.0 - bgLum) * (lum - 1.0) / 0.035294117647058809 + 1.0;
				}
			}
		}
	}

	public enum BackgroundType
	{
		Button,
		Window,
		TreeNode,
		Search,
		ComboBox,
		Toolbar,
		MainMenuMenuItem,
		ContextMenuItem,
		GridViewItem,
		DebuggerToolTip,
	}

	public struct ImageInfo
	{
		public readonly string Name;
		public readonly BackgroundType BackgroundType;

		public ImageInfo(string name, BackgroundType bgType)
		{
			this.Name = name;
			this.BackgroundType = bgType;
		}
	}

	public sealed class Images
	{
		public static readonly Images Instance = new Images();

		readonly Dictionary<Tuple<string, Color>, BitmapSource> imageCache = new Dictionary<Tuple<string, Color>, BitmapSource>();

		internal void OnThemeChanged()
		{
			imageCache.Clear();
		}

		public BitmapSource GetImage(ImageInfo info)
		{
			if (info.Name == null)
				return null;
			return GetImage(info.Name, info.BackgroundType);
		}

		public BitmapSource GetImage(string name, BackgroundType bgType)
		{
			return GetImage(name, GetColor(bgType));
		}

		public static Color GetColor(BackgroundType bgType)
		{
			switch (bgType) {
			case BackgroundType.Button: return GetColorBackground(dntheme.ColorType.ButtonIconBackground);
			case BackgroundType.Window: return GetColorForeground(dntheme.ColorType.SystemColorsWindow);
			case BackgroundType.TreeNode: return GetColorForeground(dntheme.ColorType.SystemColorsWindow);
			case BackgroundType.Search: return GetColorForeground(dntheme.ColorType.SystemColorsWindow);
			case BackgroundType.ComboBox: return GetColorForeground(dntheme.ColorType.SystemColorsWindow);
			case BackgroundType.Toolbar: return GetColorBackground(dntheme.ColorType.ToolBarIconBackground);
			case BackgroundType.MainMenuMenuItem: return GetColorBackground(dntheme.ColorType.ToolBarIconVerticalBackground);
			case BackgroundType.ContextMenuItem: return GetColorBackground(dntheme.ColorType.ContextMenuRectangleFill1);
			case BackgroundType.GridViewItem: return GetColorForeground(dntheme.ColorType.SystemColorsWindow);
			case BackgroundType.DebuggerToolTip: return Colors.White;//TODO: Update this when the debugger tooltips have been fixed
			default:
				Debug.Fail("Invalid bg type");
				return GetColorForeground(dntheme.ColorType.SystemColorsWindow);
			}
		}

		static Color GetColorForeground(dntheme.ColorType colorType)
		{
			return dntheme.Themes.Theme.GetColor(colorType).InheritedColor.Foreground.GetColor(null).Value;
		}

		static Color GetColorBackground(dntheme.ColorType colorType)
		{
			return dntheme.Themes.Theme.GetColor(colorType).InheritedColor.Background.GetColor(null).Value;
		}

		static string GetUri(object part, string icon)
		{
			var assembly = part.GetType().Assembly;
			var name = assembly.GetName();
			return "pack://application:,,,/" + name.Name + ";v" + name.Version + ";component/" + icon;
		}

		public BitmapSource GetImage(string name, Color bgColor)
		{
			return GetImage(this, name, bgColor);
		}

		public BitmapSource GetImage(object part, string icon, BackgroundType bgType)
		{
			return GetImage(part, icon, GetColor(bgType));
		}

		public BitmapSource GetImage(object part, string icon, Color bgColor)
		{
			var assembly = part.GetType().Assembly;
			var name = assembly.GetName();
			var uri = "pack://application:,,,/" + name.Name + ";v" + name.Version + ";component/Images/" + icon + ".png";
			return GetImageUsingUri(uri, bgColor);
		}

		public BitmapSource GetImageUsingUri(string key, BackgroundType bgType)
		{
			return GetImageUsingUri(key, GetColor(bgType));
		}

		public BitmapSource GetImageUsingUri(string uri, Color bgColor)
		{
			var key = Tuple.Create(uri, bgColor);
			BitmapSource image;
			if (imageCache.TryGetValue(key, out image))
				return image;

			image = ThemedImageCreator.CreateThemedBitmapSource(new BitmapImage(new Uri(uri)), bgColor);
			imageCache.Add(key, image);
			return image;
		}

		public static ImageSource GetIcon(TypeIcon icon, BackgroundType bgType)
		{
			return TreeNodes.TypeTreeNode.GetIcon(icon, bgType);
		}

		public static ImageSource GetIcon(MemberIcon icon, BackgroundType bgType)
		{
			return TreeNodes.FieldTreeNode.GetIcon(icon, bgType);
		}
	}
}
