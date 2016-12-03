/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using dnSpy.Contracts.Themes;

namespace dnSpy.Themes {
	sealed class Theme : ITheme {
		static readonly Dictionary<string, ColorType> nameToColorType = new Dictionary<string, ColorType>(StringComparer.InvariantCultureIgnoreCase);
		static readonly ColorInfo[] colorInfos = new ColorInfo[(ColorType.LastNR - ColorType.FirstNR) + (ColorType.LastUI - ColorType.FirstUI)];
		readonly Color[] hlColors = new Color[colorInfos.Length];

		static int ToIndex(ColorType colorType) {
			if (ColorType.FirstNR <= colorType && colorType < ColorType.LastNR)
				return (int)(colorType - ColorType.FirstNR);
			if (ColorType.FirstUI <= colorType && colorType < ColorType.LastUI)
				return (int)(colorType - ColorType.FirstUI + ColorType.LastNR - ColorType.FirstNR);
			Debug.Fail(string.Format("Invalid color: {0}", colorType));
			return 0;
		}

		static ColorType ToColorType(int i) {
			if (0 <= i && i < ColorType.LastNR - ColorType.FirstNR)
				return ColorType.FirstNR + (uint)i;
			if ((int)(ColorType.LastNR - ColorType.FirstNR) <= i && i < (int)((ColorType.LastNR - ColorType.FirstNR) + (ColorType.LastUI - ColorType.FirstUI)))
				return ColorType.FirstUI + ((uint)i - (ColorType.LastNR - ColorType.FirstNR));
			Debug.Fail(string.Format("Invalid color index: {0}", i));
			return 0;
		}

		static Theme() {
			foreach (var fi in typeof(ColorType).GetFields()) {
				if (!fi.IsLiteral)
					continue;
				var val = (ColorType)fi.GetValue(null);
				if (val == ColorType.LastNR || val == ColorType.LastUI)
					continue;
				nameToColorType[fi.Name] = val;
			}

			InitColorInfos(ColorInfos.RootColorInfos);
			for (int i = 0; i < colorInfos.Length; i++) {
				var colorType = ToColorType(i);
				if (colorInfos[i] == null) {
					Debug.Fail(string.Format("Missing info: {0}", colorType));
					throw new Exception(string.Format("Missing info: {0}", colorType));
				}
			}
		}

		static void InitColorInfos(ColorInfo[] infos) {
			foreach (var info in infos) {
				int i = ToIndex(info.ColorType);
				if (colorInfos[i] != null) {
					Debug.Fail("Duplicate");
					throw new Exception("Duplicate");
				}
				colorInfos[i] = info;
				InitColorInfos(info.Children);
			}
		}

		public Guid Guid { get; }
		public string Name { get; }
		public string MenuName { get; }
		public bool IsHighContrast { get; }
		public bool IsDark { get; }
		public bool IsLight { get; }
		public double Order { get; }

		public Theme(XElement root) {
			var guid = root.Attribute("guid");
			if (guid == null || string.IsNullOrEmpty(guid.Value))
				throw new Exception("Missing or empty guid attribute");
			Guid = new Guid(guid.Value);

			var name = root.Attribute("name");
			Name = name == null ? string.Empty : (string)name;

			var menuName = root.Attribute("menu-name");
			if (menuName == null || string.IsNullOrEmpty(menuName.Value))
				throw new Exception("Missing or empty menu-name attribute");
			MenuName = menuName.Value;

			var hcName = root.Attribute("is-high-contrast");
			IsHighContrast = hcName != null && (bool)hcName;

			var darkThemeName = root.Attribute("is-dark");
			IsDark = darkThemeName != null && (bool)darkThemeName;

			var lightThemeName = root.Attribute("is-light");
			IsLight = lightThemeName != null && (bool)lightThemeName;

			var sort = root.Attribute("order");
			Order = sort == null ? 1 : (double)sort;

			for (int i = 0; i < hlColors.Length; i++)
				hlColors[i] = new Color(colorInfos[i]);

			var colors = root.Element("colors");
			if (colors != null) {
				foreach (var color in colors.Elements("color")) {
					ColorType colorType = 0;
					var hl = ReadColor(color, ref colorType);
					if (hl == null)
						continue;
					hlColors[ToIndex(colorType)].OriginalColor = hl;
				}
			}
			for (int i = 0; i < hlColors.Length; i++) {
				if (hlColors[i].OriginalColor == null)
					hlColors[i].OriginalColor = CreateThemeColor(ToColorType(i));
				hlColors[i].TextInheritedColor = new ThemeColor { Name = hlColors[i].OriginalColor.Name };
				hlColors[i].InheritedColor = new ThemeColor { Name = hlColors[i].OriginalColor.Name };
			}

			RecalculateInheritedColorProperties();
		}

		/// <summary>
		/// Recalculates the inherited color properties and should be called whenever any of the
		/// color properties have been modified.
		/// </summary>
		void RecalculateInheritedColorProperties() {
			for (int i = 0; i < hlColors.Length; i++) {
				var info = colorInfos[i];
				var textColor = hlColors[i].TextInheritedColor;
				var color = hlColors[i].InheritedColor;
				if (info.ColorType == ColorType.DefaultText) {
					color.Foreground = textColor.Foreground = hlColors[ToIndex(info.ColorType)].OriginalColor.Foreground;
					color.Background = textColor.Background = hlColors[ToIndex(info.ColorType)].OriginalColor.Background;
					color.Color3 = textColor.Color3 = hlColors[ToIndex(info.ColorType)].OriginalColor.Color3;
					color.Color4 = textColor.Color4 = hlColors[ToIndex(info.ColorType)].OriginalColor.Color4;
					color.FontStyle = textColor.FontStyle = hlColors[ToIndex(info.ColorType)].OriginalColor.FontStyle;
					color.FontWeight = textColor.FontWeight = hlColors[ToIndex(info.ColorType)].OriginalColor.FontWeight;
				}
				else {
					textColor.Foreground = GetForeground(info, false);
					textColor.Background = GetBackground(info, false);
					textColor.Color3 = GetColor3(info, false);
					textColor.Color4 = GetColor4(info, false);
					textColor.FontStyle = GetFontStyle(info, false);
					textColor.FontWeight = GetFontWeight(info, false);

					color.Foreground = GetForeground(info, true);
					color.Background = GetBackground(info, true);
					color.Color3 = GetColor3(info, true);
					color.Color4 = GetColor4(info, true);
					color.FontStyle = GetFontStyle(info, true);
					color.FontWeight = GetFontWeight(info, true);
				}
			}
		}

		Brush GetForeground(ColorInfo info, bool canIncludeDefault) {
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[ToIndex(info.ColorType)];
				var val = color.OriginalColor.Foreground;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		Brush GetBackground(ColorInfo info, bool canIncludeDefault) {
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[ToIndex(info.ColorType)];
				var val = color.OriginalColor.Background;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		Brush GetColor3(ColorInfo info, bool canIncludeDefault) {
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[ToIndex(info.ColorType)];
				var val = color.OriginalColor.Color3;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		Brush GetColor4(ColorInfo info, bool canIncludeDefault) {
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[ToIndex(info.ColorType)];
				var val = color.OriginalColor.Color4;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		FontStyle? GetFontStyle(ColorInfo info, bool canIncludeDefault) {
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[ToIndex(info.ColorType)];
				var val = color.OriginalColor.FontStyle;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		FontWeight? GetFontWeight(ColorInfo info, bool canIncludeDefault) {
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[ToIndex(info.ColorType)];
				var val = color.OriginalColor.FontWeight;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		public IThemeColor GetExplicitColor(ColorType colorType) => GetColorInternal(colorType).OriginalColor;
		public IThemeColor GetTextColor(ColorType colorType) => GetColorInternal(colorType).TextInheritedColor;
		public IThemeColor GetColor(ColorType colorType) => GetColorInternal(colorType).InheritedColor;

		Color GetColorInternal(ColorType colorType) {
			uint i = (uint)ToIndex(colorType);
			if (i >= (uint)hlColors.Length)
				return hlColors[ToIndex(ColorType.DefaultText)];
			return hlColors[i];
		}

		ThemeColor ReadColor(XElement color, ref ColorType colorType) {
			var name = color.Attribute("name");
			if (name == null)
				return null;
			colorType = ToColorType(name.Value);
			if (colorType == ColorType.LastUI)
				return null;

			var colorInfo = colorInfos[ToIndex(colorType)];

			var hl = new ThemeColor();
			hl.Name = colorType.ToString();

			var fg = GetAttribute(color, "fg", colorInfo.DefaultForeground);
			if (fg != null)
				hl.Foreground = CreateColor(fg);

			var bg = GetAttribute(color, "bg", colorInfo.DefaultBackground);
			if (bg != null)
				hl.Background = CreateColor(bg);

			var color3 = GetAttribute(color, "color3", colorInfo.DefaultColor3);
			if (color3 != null)
				hl.Color3 = CreateColor(color3);

			var color4 = GetAttribute(color, "color4", colorInfo.DefaultColor4);
			if (color4 != null)
				hl.Color4 = CreateColor(color4);

			var italics = color.Attribute("italics") ?? color.Attribute("italic");
			if (italics != null)
				hl.FontStyle = (bool)italics ? FontStyles.Italic : FontStyles.Normal;

			var bold = color.Attribute("bold");
			if (bold != null)
				hl.FontWeight = (bool)bold ? FontWeights.Bold : FontWeights.Normal;

			return hl;
		}

		ThemeColor CreateThemeColor(ColorType colorType) {
			var hl = new ThemeColor { Name = colorType.ToString() };

			var colorInfo = colorInfos[ToIndex(colorType)];

			if (colorInfo.DefaultForeground != null)
				hl.Foreground = CreateColor(colorInfo.DefaultForeground);

			if (colorInfo.DefaultBackground != null)
				hl.Background = CreateColor(colorInfo.DefaultBackground);

			if (colorInfo.DefaultColor3 != null)
				hl.Color3 = CreateColor(colorInfo.DefaultColor3);

			if (colorInfo.DefaultColor4 != null)
				hl.Color4 = CreateColor(colorInfo.DefaultColor4);

			return hl;
		}

		static string GetAttribute(XElement xml, string attr, string defVal) {
			var a = xml.Attribute(attr);
			if (a != null)
				return a.Value;
			return defVal;
		}

		static readonly ColorConverter colorConverter = new ColorConverter();
		static Brush CreateColor(string color) {
			if (color.StartsWith("SystemColors.")) {
				string shortName = color.Substring(13);
				var property = typeof(SystemColors).GetProperty(shortName + "Brush");
				Debug.Assert(property != null);
				if (property == null)
					return null;
				return (Brush)property.GetValue(null, null);
			}

			try {
				var clr = (System.Windows.Media.Color?)colorConverter.ConvertFromInvariantString(color);
				if (clr == null)
					return null;
				var brush = new SolidColorBrush(clr.Value);
				brush.Freeze();
				return brush;
			}
			catch {
				Debug.Fail(string.Format("Couldn't convert color '{0}'", color));
				throw;
			}
		}

		static ColorType ToColorType(string name) {
			ColorType type;
			if (nameToColorType.TryGetValue(name, out type))
				return type;
			Debug.Fail(string.Format("Invalid color found: {0}", name));
			return ColorType.LastUI;
		}

		internal void UpdateResources(ResourceDictionary resources) {
			foreach (var color in hlColors) {
				foreach (var kv in color.ColorInfo.GetResourceKeyValues(color.InheritedColor))
					resources[kv.Item1] = kv.Item2;
			}
		}

		public override string ToString() => $"Theme: {Guid} {MenuName}";
	}
}
