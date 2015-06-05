/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.dntheme
{
	public sealed class MyHighlightingColor : HighlightingColor
	{
		HighlightingBrush color3;
		HighlightingBrush color4;

		public HighlightingBrush Color3 {
			get { return color3; }
			set {
				if (IsFrozen)
					throw new InvalidOperationException();
				color3 = value;
			}
		}

		public HighlightingBrush Color4 {
			get { return color4; }
			set {
				if (IsFrozen)
					throw new InvalidOperationException();
				color4 = value;
			}
		}

		public HighlightingBrush GetHighlightingBrush(int index)
		{
			switch (index) {
			case 0: return Foreground;
			case 1: return Background;
			case 2: return Color3;
			case 3: return Color4;
			default: throw new ArgumentOutOfRangeException();
			}
		}
	}

	[DebuggerDisplay("{ColorType}, Children={Children.Length}")]
	public abstract class ColorInfo
	{
		public readonly ColorType ColorType;
		public readonly string Description;
		public string DefaultForeground;
		public string DefaultBackground;
		public string DefaultColor3;
		public string DefaultColor4;
		public ColorInfo Parent;

		public ColorInfo[] Children {
			get { return children; }
			set {
				children = value ?? new ColorInfo[0];
				foreach (var child in children)
					child.Parent = this;
			}
		}
		ColorInfo[] children = new ColorInfo[0];

		public abstract IEnumerable<Tuple<object, object>> GetResourceKeyValues(MyHighlightingColor hlColor);

		protected ColorInfo(ColorType colorType, string description)
		{
			this.ColorType = colorType;
			this.Description = description;
		}
	}

	public sealed class ColorColorInfo : ColorInfo
	{
		public object BackgroundResourceKey;
		public object ForegroundResourceKey;

		public ColorColorInfo(ColorType colorType, string description)
			: base(colorType, description)
		{
		}

		public override IEnumerable<Tuple<object, object>> GetResourceKeyValues(MyHighlightingColor hlColor)
		{
			if (ForegroundResourceKey != null) {
				Debug.Assert(hlColor.Foreground != null);
				yield return new Tuple<object, object>(ForegroundResourceKey, ((SolidColorBrush)hlColor.Foreground.GetBrush(null)).Color);
			}
			if (BackgroundResourceKey != null) {
				Debug.Assert(hlColor.Background != null);
				yield return new Tuple<object, object>(BackgroundResourceKey, ((SolidColorBrush)hlColor.Background.GetBrush(null)).Color);
			}
		}
	}

	public sealed class BrushColorInfo : ColorInfo
	{
		public object BackgroundResourceKey;
		public object ForegroundResourceKey;
		public double? BackgroundOpacity;
		public double? ForegroundOpacity;

		public BrushColorInfo(ColorType colorType, string description)
			: base(colorType, description)
		{
		}

		public override IEnumerable<Tuple<object, object>> GetResourceKeyValues(MyHighlightingColor hlColor)
		{
			if (ForegroundResourceKey != null) {
				Debug.Assert(hlColor.Foreground != null);
				if (ForegroundOpacity == null)
					yield return new Tuple<object, object>(ForegroundResourceKey, hlColor.Foreground.GetBrush(null));
				else {
					var color = hlColor.Foreground.GetColor(null).Value;
					var brush = new SolidColorBrush(color) { Opacity = ForegroundOpacity.Value };
					yield return new Tuple<object, object>(ForegroundResourceKey, brush);
				}
			}
			if (BackgroundResourceKey != null) {
				Debug.Assert(hlColor.Background != null);
				if (BackgroundOpacity == null)
					yield return new Tuple<object, object>(BackgroundResourceKey, hlColor.Background.GetBrush(null));
				else {
					var color = hlColor.Background.GetColor(null).Value;
					var brush = new SolidColorBrush(color) { Opacity = BackgroundOpacity.Value };
					yield return new Tuple<object, object>(BackgroundResourceKey, brush);
				}
			}
		}
	}

	public sealed class LinearGradientColorInfo : ColorInfo
	{
		public object ResourceKey;
		public Point StartPoint;
		public Point EndPoint;
		public double[] GradientOffsets;
		public BrushMappingMode? MappingMode;

		public LinearGradientColorInfo(ColorType colorType, Point endPoint, string description, params double[] gradientOffsets)
			: this(colorType, new Point(0, 0), endPoint, description, gradientOffsets)
		{
		}

		public LinearGradientColorInfo(ColorType colorType, Point startPoint, Point endPoint, string description, params double[] gradientOffsets)
			: base(colorType, description)
		{
			this.StartPoint = startPoint;
			this.EndPoint = endPoint;
			this.GradientOffsets = gradientOffsets;
		}

		public override IEnumerable<Tuple<object, object>> GetResourceKeyValues(MyHighlightingColor hlColor)
		{
			var br = new LinearGradientBrush() {
				StartPoint = StartPoint,
				EndPoint = EndPoint,
			};
			if (MappingMode != null)
				br.MappingMode = MappingMode.Value;
			for (int i = 0; i < GradientOffsets.Length; i++)
				br.GradientStops.Add(new GradientStop(((SolidColorBrush)hlColor.GetHighlightingBrush(i).GetBrush(null)).Color, GradientOffsets[i]));
			br.Freeze();
			yield return new Tuple<object, object>(ResourceKey, br);
		}
	}

	public sealed class RadialGradientColorInfo : ColorInfo
	{
		public object ResourceKey;
		public Transform RelativeTransform;
		public double[] GradientOffsets;
		public Point? Center;
		public Point? GradientOrigin;
		public double? RadiusX, RadiusY;
		public double? Opacity;

		public RadialGradientColorInfo(ColorType colorType, string description, params double[] gradientOffsets)
			: base(colorType, description)
		{
			this.GradientOffsets = gradientOffsets;
		}

		public RadialGradientColorInfo(ColorType colorType, string relativeTransformString, string description, params double[] gradientOffsets)
			: base(colorType, description)
		{
			this.GradientOffsets = gradientOffsets;
			this.RelativeTransform = (Transform)transformConverter.ConvertFromInvariantString(relativeTransformString);
		}
		static readonly TransformConverter transformConverter = new TransformConverter();

		public override IEnumerable<Tuple<object, object>> GetResourceKeyValues(MyHighlightingColor hlColor)
		{
			var br = new RadialGradientBrush() {
				RadiusX = 1,
				RadiusY = 1,
			};
			if (RelativeTransform != null)
				br.RelativeTransform = RelativeTransform;
			if (Center != null)
				br.Center = Center.Value;
			if (GradientOrigin != null)
				br.GradientOrigin = GradientOrigin.Value;
			if (RadiusX != null)
				br.RadiusX = RadiusX.Value;
			if (RadiusY != null)
				br.RadiusY = RadiusY.Value;
			if (Opacity != null)
				br.Opacity = Opacity.Value;
			for (int i = 0; i < GradientOffsets.Length; i++)
				br.GradientStops.Add(new GradientStop(((SolidColorBrush)hlColor.GetHighlightingBrush(i).GetBrush(null)).Color, GradientOffsets[i]));
			br.Freeze();
			yield return new Tuple<object, object>(ResourceKey, br);
		}
	}

	[DebuggerDisplay("{ColorInfo.ColorType}")]
	public sealed class Color
	{
		/// <summary>
		/// Color info
		/// </summary>
		public readonly ColorInfo ColorInfo;

		/// <summary>
		/// Original color with no inherited properties. If this one or any of its properties
		/// get modified, <see cref="Theme.RecalculateInheritedColorProperties()"/> must be
		/// called.
		/// </summary>
		public MyHighlightingColor OriginalColor;

		/// <summary>
		/// Color with inherited properties, but doesn't include inherited default text (because
		/// it messes up with selection in text editor). See also <see cref="InheritedColor"/>
		/// </summary>
		public MyHighlightingColor TextInheritedColor;

		/// <summary>
		/// Color with inherited properties. See also <see cref="TextInheritedColor"/>
		/// </summary>
		public MyHighlightingColor InheritedColor;

		public Color(ColorInfo colorInfo)
		{
			this.ColorInfo = colorInfo;
		}
	}

	public sealed class Theme
	{
		static readonly Dictionary<string, ColorType> nameToColorType = new Dictionary<string, ColorType>(StringComparer.InvariantCultureIgnoreCase);

		static readonly ColorInfo[] rootColorInfos = new ColorInfo[] {
			new BrushColorInfo(ColorType.Selection, "Selected text") {
				DefaultBackground = "#663399FF",
			},
			new BrushColorInfo(ColorType.SpecialCharacterBox, "Special character box") {
				DefaultBackground = "#C8808080",
			},
			new BrushColorInfo(ColorType.SearchResultMarker, "Search result marker") {
				DefaultBackground = "#FFFFB7",
			},
			new BrushColorInfo(ColorType.CurrentLine, "Current line") {
				DefaultForeground = "#EAEAF2",
				DefaultBackground = "#00000000",
			},
			new BrushColorInfo(ColorType.SystemColorsControl, "SystemColors.Control") {
				DefaultBackground = "#FFF0F0F0",
				BackgroundResourceKey = "SystemColorsControl"
			},
			new BrushColorInfo(ColorType.SystemColorsControlDark, "SystemColors.ControlDark") {
				DefaultBackground = "#FFA0A0A0",
				BackgroundResourceKey = "SystemColorsControlDark"
			},
			new BrushColorInfo(ColorType.SystemColorsControlDarkDark, "SystemColors.ControlDarkDark") {
				DefaultBackground = "#FF696969",
				BackgroundResourceKey = "SystemColorsControlDarkDark"
			},
			new BrushColorInfo(ColorType.SystemColorsControlLight, "SystemColors.ControlLight") {
				DefaultBackground = "#FFE3E3E3",
				BackgroundResourceKey = "SystemColorsControlLight"
			},
			new BrushColorInfo(ColorType.SystemColorsControlLightLight, "SystemColors.ControlLightLight") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "SystemColorsControlLightLight"
			},
			new BrushColorInfo(ColorType.SystemColorsControlText, "SystemColors.ControlText") {
				DefaultForeground = "#FF000000",
				ForegroundResourceKey = "SystemColorsControlText"
			},
			new BrushColorInfo(ColorType.SystemColorsGrayText, "SystemColors.GrayText") {
				DefaultForeground = "#FF6D6D6D",
				ForegroundResourceKey = "SystemColorsGrayText"
			},
			new BrushColorInfo(ColorType.SystemColorsHighlight, "SystemColors.Highlight") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "SystemColorsHighlight"
			},
			new BrushColorInfo(ColorType.SystemColorsHighlightText, "SystemColors.HighlightText") {
				DefaultForeground = "#FFFFFFFF",
				ForegroundResourceKey = "SystemColorsHighlightText"
			},
			new BrushColorInfo(ColorType.SystemColorsInactiveCaption, "SystemColors.InactiveCaption") {
				DefaultBackground = "#FFBFCDDB",
				BackgroundResourceKey = "SystemColorsInactiveCaption"
			},
			new BrushColorInfo(ColorType.SystemColorsInactiveCaptionText, "SystemColors.InactiveCaptionText") {
				DefaultForeground = "#FF000000",
				ForegroundResourceKey = "SystemColorsInactiveCaptionText"
			},
			new BrushColorInfo(ColorType.SystemColorsInactiveSelectionHighlight, "SystemColors.InactiveSelectionHighlight") {
				DefaultBackground = "#CCCCCC",
				BackgroundResourceKey = "SystemColorsInactiveSelectionHighlight"
			},
			new BrushColorInfo(ColorType.SystemColorsInactiveSelectionHighlightText, "SystemColors.InactiveSelectionHighlightText") {
				DefaultForeground = "#FF000000",
				ForegroundResourceKey = "SystemColorsInactiveSelectionHighlightText"
			},
			new BrushColorInfo(ColorType.SystemColorsMenuText, "SystemColors.MenuText") {
				DefaultForeground = "#FF000000",
				ForegroundResourceKey = "SystemColorsMenuText"
			},
			new BrushColorInfo(ColorType.SystemColorsWindow, "SystemColors.Window") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "SystemColorsWindow"
			},
			new BrushColorInfo(ColorType.SystemColorsWindowText, "SystemColors.WindowText") {
				DefaultForeground = "#FF000000",
				ForegroundResourceKey = "SystemColorsWindowText"
			},
			new BrushColorInfo(ColorType.DialogWindow, "Dialog Window") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "DialogWindowForeground",
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "DialogWindowBackground"
			},
			new LinearGradientColorInfo(ColorType.EnvironmentBackground, new Point(0, 1), "MainWindow background", 0, 0.4, 0.6, 1) {
				ResourceKey = "EnvironmentBackground",
				DefaultForeground = "#FFEEEEF2",// Environment.EnvironmentBackgroundGradientBegin
				DefaultBackground = "#FFEEEEF2",// Environment.EnvironmentBackgroundGradientMiddle1
				DefaultColor3 = "#FFEEEEF2",// Environment.EnvironmentBackgroundGradientMiddle2
				DefaultColor4 = "#FFEEEEF2",// Environment.EnvironmentBackgroundGradientEnd
			},
			new BrushColorInfo(ColorType.EnvironmentForeground, "MainWindow foreground") {
				DefaultForeground = "#FF000000",
				ForegroundResourceKey = "EnvironmentForeground",
			},
			new ColorColorInfo(ColorType.ControlShadow, "Control shadow") {
				DefaultBackground = "#71000000",
				BackgroundResourceKey = "ControlShadow",
			},
			new BrushColorInfo(ColorType.GridSplitterPreviewFill, "Grid splitter preview fill") {
				DefaultBackground = "#80000000",
				BackgroundResourceKey = "GridSplitterPreviewFill",
			},
			new BrushColorInfo(ColorType.GroupBoxBorderBrush, "GroupBox border brush") {
				DefaultBackground = "#D5DFE5",
				BackgroundResourceKey = "GroupBoxBorderBrush",
			},
			new BrushColorInfo(ColorType.GroupBoxBorderBrushOuter, "GroupBox outer border brush") {
				DefaultBackground = "White",
				BackgroundResourceKey = "GroupBoxBorderBrushOuter",
			},
			new BrushColorInfo(ColorType.GroupBoxBorderBrushInner, "GroupBox inner border brush") {
				DefaultBackground = "White",
				BackgroundResourceKey = "GroupBoxBorderBrushInner",
			},
			new BrushColorInfo(ColorType.MenuItemSeparatorFillTop, "MenuItem Separator fill (top)") {
				DefaultBackground = "#E0E3E6",
				BackgroundResourceKey = "MenuItemSeparatorFillTop",
			},
			new BrushColorInfo(ColorType.MenuItemSeparatorFillBottom, "MenuItem Separator fill (bottom)") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "MenuItemSeparatorFillBottom",
			},
			new LinearGradientColorInfo(ColorType.MenuItemSelectionFill, new Point(0, 1), "MenuItem selection fill", 0, 1) {
				ResourceKey = "MenuItemSelectionFill",
				DefaultForeground = "#C9DEF5",
				DefaultBackground = "#C9DEF5",
			},
			new BrushColorInfo(ColorType.MenuItemGlyphPanelBackground, "MenuItem glyph panel background") {
				DefaultBackground = "#E6EFF4",
				BackgroundResourceKey = "MenuItemGlyphPanelBackground",
			},
			new BrushColorInfo(ColorType.MenuItemGlyphPanelBorderBrush, "MenuItem glyph panel border brush") {
				DefaultBackground = "#CDD3E6",
				BackgroundResourceKey = "MenuItemGlyphPanelBorderBrush",
			},
			new BrushColorInfo(ColorType.MenuItemGlyphFill, "MenuItem glyph fill") {
				DefaultBackground = "#1E1E1E",
				BackgroundResourceKey = "MenuItemGlyphFill",
			},
			new BrushColorInfo(ColorType.MenuItemHighlightedStroke, "MenuItem highlighted stroke") {
				DefaultBackground = "#C9DEF5",
				BackgroundResourceKey = "MenuItemHighlightedStroke",
			},
			new BrushColorInfo(ColorType.MenuItemHighlightedInnerBorder, "MenuItem highlighted inner border") {
				DefaultBackground = "#C9DEF5",
				BackgroundResourceKey = "MenuItemHighlightedInnerBorder",
			},
			new BrushColorInfo(ColorType.MenuItemDisabledForeground, "MenuItem disabled foreground") {
				DefaultForeground = "#FF9A9A9A",
				ForegroundResourceKey = "MenuItemDisabledForeground",
			},
			new BrushColorInfo(ColorType.MenuItemDisabledGlyphPanelBackground, "MenuItem disabled glyph panel background") {
				DefaultBackground = "#EEE9E9",
				BackgroundResourceKey = "MenuItemDisabledGlyphPanelBackground",
			},
			new BrushColorInfo(ColorType.MenuItemDisabledGlyphPanelBorderBrush, "MenuItem disabled glyph panel border brush") {
				DefaultBackground = "#DBD6D6",
				BackgroundResourceKey = "MenuItemDisabledGlyphPanelBorderBrush",
			},
			new BrushColorInfo(ColorType.MenuItemDisabledGlyphFill, "MenuItem disabled glyph fill") {
				DefaultBackground = "#848589",
				BackgroundResourceKey = "MenuItemDisabledGlyphFill",
			},
			new BrushColorInfo(ColorType.ToolBarDarkFill, "Selected color of menu item's checkbox") {
				DefaultBackground = "#C9DEF5",
				BackgroundResourceKey = "ToolBarDarkFill",
			},
			new BrushColorInfo(ColorType.ToolBarButtonPressed, "Toolbar button pressed") {
				DefaultBackground = "#99CCFF",
				BackgroundResourceKey = "ToolBarButtonPressed",
			},
			new BrushColorInfo(ColorType.ToolBarSeparatorFill, "Toolbar separator fill color") {
				DefaultBackground = "#E0E3E6",
				BackgroundResourceKey = "ToolBarSeparatorFill",
			},
			new BrushColorInfo(ColorType.ToolBarButtonHover, "Toolbar button hover color") {
				DefaultBackground = "#C9DEF5",
				BackgroundResourceKey = "ToolBarButtonHover",
			},
			new BrushColorInfo(ColorType.ToolBarButtonHoverBorder, "Toolbar button hover border") {
				DefaultBackground = "#CCCCCC",
				BackgroundResourceKey = "ToolBarButtonHoverBorder",
			},
			new BrushColorInfo(ColorType.ToolBarButtonPressedBorder, "Toolbar button pressed border") {
				DefaultBackground = "#888888",
				BackgroundResourceKey = "ToolBarButtonPressedBorder",
			},
			new BrushColorInfo(ColorType.ToolBarMenuBorder, "Toolbar menu border") {
				DefaultBackground = "#CCCEDB",
				BackgroundResourceKey = "ToolBarMenuBorder",
			},
			new BrushColorInfo(ColorType.ToolBarSubMenuBackground, "Toolbar sub menu") {
				DefaultBackground = "#F6F6F6",
				BackgroundResourceKey = "ToolBarSubMenuBackground",
			},
			new BrushColorInfo(ColorType.ToolBarMenuCheckFill, "Toolbar menu check fill") {
				DefaultBackground = "#EEEEF2",
				BackgroundResourceKey = "ToolBarMenuCheckFill",
			},
			new BrushColorInfo(ColorType.ToolBarButtonChecked, "Toolbar button checked") {
				DefaultBackground = "#E6F0FA",
				BackgroundResourceKey = "ToolBarButtonChecked",
			},
			new LinearGradientColorInfo(ColorType.ToolBarOpenHeaderBackground, new Point(0, 1), "Toolbar open header. Color of top level menu item text when the sub menu is open.", 0, 1) {
				ResourceKey = "ToolBarOpenHeaderBackground",
				DefaultForeground = "#F6F6F6",
				DefaultBackground = "#F6F6F6",
			},
			new BrushColorInfo(ColorType.ToolBarIconVerticalBackground, "ToolBar icon vertical background. Makes sure icons look good with this background color.") {
				DefaultBackground = "#F6F6F6",
			},
			new LinearGradientColorInfo(ColorType.ToolBarVerticalBackground, new Point(1, 0), "Toolbar vertical header. Color of left vertical part of menu items.", 0, 0.5, 1) {
				ResourceKey = "ToolBarVerticalBackground",
				DefaultForeground = "#F6F6F6",
				DefaultBackground = "#F6F6F6",
				DefaultColor3 = "#F6F6F6",
			},
			new BrushColorInfo(ColorType.ToolBarIconBackground, "ToolBar icon background. Makes sure icons look good with this background color.") {
				DefaultBackground = "#EEEEF2",
			},
			new LinearGradientColorInfo(ColorType.ToolBarHorizontalBackground, new Point(0, 1), "Toolbar horizontal background", 0, 0.5, 1) {
				ResourceKey = "ToolBarHorizontalBackground",
				DefaultForeground = "#EEEEF2",
				DefaultBackground = "#EEEEF2",
				DefaultColor3 = "#EEEEF2",
			},
			new BrushColorInfo(ColorType.ToolBarDisabledFill, "Toolbar disabled fill (combobox & textbox)") {
				DefaultBackground = "#FFDADADA",
				BackgroundResourceKey = "ToolBarDisabledFill",
			},
			new BrushColorInfo(ColorType.ToolBarDisabledBorder, "Toolbar disabled border (combobox & textbox)") {
				DefaultBackground = "#FFDADADA",
				BackgroundResourceKey = "ToolBarDisabledBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsButtonIconBackground, "Button icon background. Makes sure icons look good with this background color.") {
				DefaultBackground = "#FFECECF0",
			},
			new BrushColorInfo(ColorType.CommonControlsButton, "CommonControls Button") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "CommonControlsButtonText",
				DefaultBackground = "#FFECECF0",
				BackgroundResourceKey = "CommonControlsButton",
			},
			new BrushColorInfo(ColorType.CommonControlsButtonBorder, "CommonControls Button Border") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CommonControlsButtonBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsButtonBorderDisabled, "CommonControls Button Border Disabled") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CommonControlsButtonBorderDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsButtonBorderFocused, "CommonControls Button Border Focused") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsButtonBorderFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsButtonBorderHover, "CommonControls Button Border Hover") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsButtonBorderHover",
			},
			new BrushColorInfo(ColorType.CommonControlsButtonBorderPressed, "CommonControls Button Border Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsButtonBorderPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsButtonDisabled, "CommonControlsButtonDisabled") {
				DefaultForeground = "#FFA2A4A5",
				ForegroundResourceKey = "CommonControlsButtonDisabledText",
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "CommonControlsButtonDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsButtonFocused, "CommonControls Button Focused") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "CommonControlsButtonFocusedText",
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "CommonControlsButtonFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsButtonHover, "CommonControls Button Hover") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "CommonControlsButtonHoverText",
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "CommonControlsButtonHover",
			},
			new BrushColorInfo(ColorType.CommonControlsButtonPressed, "CommonControls Button Pressed") {
				DefaultForeground = "#FFFFFFFF",
				ForegroundResourceKey = "CommonControlsButtonPressedText",
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsButtonPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxBackground, "CommonControls CheckBox Background") {
				DefaultBackground = "#FFFEFEFE",
				BackgroundResourceKey = "CommonControlsCheckBoxBackground",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxBackgroundDisabled, "CommonControls CheckBox Background Disabled") {
				DefaultBackground = "#FFF6F6F6",
				BackgroundResourceKey = "CommonControlsCheckBoxBackgroundDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxBackgroundFocused, "CommonControls CheckBox Background Focused") {
				DefaultBackground = "#FFF3F9FF",
				BackgroundResourceKey = "CommonControlsCheckBoxBackgroundFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxBackgroundHover, "CommonControls CheckBox Background Hover") {
				DefaultBackground = "#FFF3F9FF",
				BackgroundResourceKey = "CommonControlsCheckBoxBackgroundHover",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxBackgroundPressed, "CommonControls CheckBox Background Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsCheckBoxBackgroundPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxBorder, "CommonControls CheckBox Border") {
				DefaultBackground = "#FF717171",
				BackgroundResourceKey = "CommonControlsCheckBoxBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxBorderDisabled, "CommonControls CheckBox Border Disabled") {
				DefaultBackground = "#FFC6C6C6",
				BackgroundResourceKey = "CommonControlsCheckBoxBorderDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxBorderFocused, "CommonControls CheckBox Border Focused") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsCheckBoxBorderFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxBorderHover, "CommonControls CheckBox Border Hover") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsCheckBoxBorderHover",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxBorderPressed, "CommonControls CheckBox Border Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsCheckBoxBorderPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxGlyph, "CommonControls CheckBox Glyph") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsCheckBoxGlyph",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxGlyphDisabled, "CommonControls CheckBox Glyph Disabled") {
				DefaultBackground = "#FFA2A4A5",
				BackgroundResourceKey = "CommonControlsCheckBoxGlyphDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxGlyphFocused, "CommonControls CheckBox Glyph Focused") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsCheckBoxGlyphFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxGlyphHover, "CommonControls CheckBox Glyph Hover") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsCheckBoxGlyphHover",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxGlyphPressed, "CommonControls CheckBox Glyph Pressed") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "CommonControlsCheckBoxGlyphPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxText, "CommonControls CheckBox Text") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsCheckBoxText",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxTextDisabled, "CommonControls CheckBox Text Disabled") {
				DefaultBackground = "#FFA2A4A5",
				BackgroundResourceKey = "CommonControlsCheckBoxTextDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxTextFocused, "CommonControls CheckBox Text Focused") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsCheckBoxTextFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxTextHover, "CommonControls CheckBox Text Hover") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsCheckBoxTextHover",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxTextPressed, "CommonControls CheckBox Text Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsCheckBoxTextPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxBackground, "CommonControls ComboBox Background") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "CommonControlsComboBoxBackground",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxBackgroundDisabled, "CommonControls ComboBox Background Disabled") {
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "CommonControlsComboBoxBackgroundDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxBackgroundFocused, "CommonControls ComboBox Background Focused") {
				DefaultBackground = "#FFF6F6F6",
				BackgroundResourceKey = "CommonControlsComboBoxBackgroundFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxBackgroundHover, "CommonControls ComboBox Background Hover") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "CommonControlsComboBoxBackgroundHover",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxBackgroundPressed, "CommonControls ComboBox Background Pressed") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "CommonControlsComboBoxBackgroundPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxBorder, "CommonControls ComboBox Border") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CommonControlsComboBoxBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxBorderDisabled, "CommonControls ComboBox Border Disabled") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CommonControlsComboBoxBorderDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxBorderFocused, "CommonControls ComboBox Border Focused") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsComboBoxBorderFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxBorderHover, "CommonControls ComboBox Border Hover") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsComboBoxBorderHover",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxBorderPressed, "CommonControls ComboBox Border Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsComboBoxBorderPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxGlyph, "CommonControls ComboBox Glyph") {
				DefaultBackground = "#FF717171",
				BackgroundResourceKey = "CommonControlsComboBoxGlyph",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxGlyphBackground, "CommonControls ComboBox Glyph Background") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "CommonControlsComboBoxGlyphBackground",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxGlyphBackgroundDisabled, "CommonControls ComboBox Glyph Background Disabled") {
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "CommonControlsComboBoxGlyphBackgroundDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxGlyphBackgroundFocused, "CommonControls ComboBox Glyph Background Focused") {
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "CommonControlsComboBoxGlyphBackgroundFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxGlyphBackgroundHover, "CommonControls ComboBox Glyph Background Hover") {
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "CommonControlsComboBoxGlyphBackgroundHover",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxGlyphBackgroundPressed, "CommonControls ComboBox Glyph Background Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsComboBoxGlyphBackgroundPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxGlyphDisabled, "CommonControls ComboBox Glyph Disabled") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CommonControlsComboBoxGlyphDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxGlyphFocused, "CommonControls ComboBox Glyph Focused") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsComboBoxGlyphFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxGlyphHover, "CommonControls ComboBox Glyph Hover") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsComboBoxGlyphHover",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxGlyphPressed, "CommonControls ComboBox Glyph Pressed") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "CommonControlsComboBoxGlyphPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxListBackground, "CommonControls ComboBox List Background") {
				DefaultBackground = "#FFF6F6F6",
				BackgroundResourceKey = "CommonControlsComboBoxListBackground",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxListBorder, "CommonControls ComboBox ListBorder") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CommonControlsComboBoxListBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxListItemBackgroundHover, "CommonControls ComboBox ListItem Background Hover") {
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "CommonControlsComboBoxListItemBackgroundHover",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxListItemBorderHover, "CommonControls ComboBox ListItem Border Hover") {
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "CommonControlsComboBoxListItemBorderHover",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxListItemText, "CommonControls ComboBox ListItem Text") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsComboBoxListItemText",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxListItemTextHover, "CommonControls ComboBox ListItem Text Hover") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsComboBoxListItemTextHover",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxSeparator, "CommonControls ComboBox Separator") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CommonControlsComboBoxSeparator",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxSeparatorFocused, "CommonControls ComboBox Separator Focused") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsComboBoxSeparatorFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxSeparatorHover, "CommonControls ComboBox Separator Hover") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsComboBoxSeparatorHover",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxSeparatorPressed, "CommonControls ComboBox Separator Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsComboBoxSeparatorPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxText, "CommonControls ComboBox Text") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsComboBoxText",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxTextDisabled, "CommonControls ComboBox Text Disabled") {
				DefaultBackground = "#FFA2A4A5",
				BackgroundResourceKey = "CommonControlsComboBoxTextDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxTextFocused, "CommonControls ComboBox Text Focused") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsComboBoxTextFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxTextHover, "CommonControls ComboBox Text Hover") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsComboBoxTextHover",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxTextInputSelection, "CommonControls ComboBox Text Input Selection") {
				DefaultBackground = "#66007ACC",
				BackgroundResourceKey = "CommonControlsComboBoxTextInputSelection",
			},
			new BrushColorInfo(ColorType.CommonControlsComboBoxTextPressed, "CommonControls ComboBox Text Pressed") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsComboBoxTextPressed",
			},

			new BrushColorInfo(ColorType.CommonControlsRadioButtonBackground, "CommonControls RadioButton Background") {
				DefaultBackground = "#FFFEFEFE",
				BackgroundResourceKey = "CommonControlsRadioButtonBackground",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonBackgroundDisabled, "CommonControls RadioButton Background Disabled") {
				DefaultBackground = "#FFF6F6F6",
				BackgroundResourceKey = "CommonControlsRadioButtonBackgroundDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonBackgroundFocused, "CommonControls RadioButton Background Focused") {
				DefaultBackground = "#FFF3F9FF",
				BackgroundResourceKey = "CommonControlsRadioButtonBackgroundFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonBackgroundHover, "CommonControls RadioButton Background Hover") {
				DefaultBackground = "#FFF3F9FF",
				BackgroundResourceKey = "CommonControlsRadioButtonBackgroundHover",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonBackgroundPressed, "CommonControls RadioButton Background Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsRadioButtonBackgroundPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonBorder, "CommonControls RadioButton Border") {
				DefaultBackground = "#FF717171",
				BackgroundResourceKey = "CommonControlsRadioButtonBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonBorderDisabled, "CommonControls RadioButton Border Disabled") {
				DefaultBackground = "#FFC6C6C6",
				BackgroundResourceKey = "CommonControlsRadioButtonBorderDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonBorderFocused, "CommonControls RadioButton Border Focused") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsRadioButtonBorderFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonBorderHover, "CommonControls RadioButton Border Hover") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsRadioButtonBorderHover",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonBorderPressed, "CommonControls RadioButton Border Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsRadioButtonBorderPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonGlyph, "CommonControls RadioButton Glyph") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsRadioButtonGlyph",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonGlyphDisabled, "CommonControls RadioButton Glyph Disabled") {
				DefaultBackground = "#FFA2A4A5",
				BackgroundResourceKey = "CommonControlsRadioButtonGlyphDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonGlyphFocused, "CommonControls RadioButton Glyph Focused") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsRadioButtonGlyphFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonGlyphHover, "CommonControls RadioButton Glyph Hover") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsRadioButtonGlyphHover",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonGlyphPressed, "CommonControls RadioButton Glyph Pressed") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "CommonControlsRadioButtonGlyphPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonText, "CommonControls RadioButton Text") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsRadioButtonText",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonTextDisabled, "CommonControls RadioButton Text Disabled") {
				DefaultBackground = "#FFA2A4A5",
				BackgroundResourceKey = "CommonControlsRadioButtonTextDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonTextFocused, "CommonControls RadioButton Text Focused") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsRadioButtonTextFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonTextHover, "CommonControls RadioButton Text Hover") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsRadioButtonTextHover",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonTextPressed, "CommonControls RadioButton Text Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsRadioButtonTextPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBox, "CommonControls TextBox") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "CommonControlsTextBoxForeground",
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "CommonControlsTextBoxBackground",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxBorder, "CommonControls TextBox Border") {
				DefaultBackground = "#FFDBDDE6",
				BackgroundResourceKey = "CommonControlsTextBoxBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxDisabled, "CommonControls TextBox Disabled") {
				DefaultForeground = "#FFA2A4A5",
				ForegroundResourceKey = "CommonControlsTextBoxDisabledForeground",
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "CommonControlsTextBoxDisabledBackground",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxDisabledBorder, "CommonControls TextBox Disabled Border") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CommonControlsTextBoxDisabledBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxError, "CommonControls TextBox Error") {
				DefaultForeground = "#FF000000",
				ForegroundResourceKey = "CommonControlsTextBoxErrorForeground",
				DefaultBackground = "Pink",
				BackgroundResourceKey = "CommonControlsTextBoxErrorBackground",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxErrorBorder, "CommonControls TextBox Error Border") {
				DefaultBackground = "Red",
				BackgroundResourceKey = "CommonControlsTextBoxErrorBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxFocusedBorder, "CommonControls TextBox Focused Border") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsTextBoxFocusedBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxMouseOverBorder, "CommonControls TextBox Mouse Over Border") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsTextBoxMouseOverBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxSelection, "CommonControls TextBox Selection") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsTextBoxSelection",
			},
			new BrushColorInfo(ColorType.CommonControlsFocusVisual, "CommonControlsFocusVisual") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "CommonControlsFocusVisualText",
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "CommonControlsFocusVisual",
			},
			new BrushColorInfo(ColorType.TabItemForeground, "TabItem Foreground") {
				DefaultBackground = "#FF000000",
				BackgroundResourceKey = "TabItemForeground",
			},
			new BrushColorInfo(ColorType.TabItemStaticBackground, "TabItem Static Background") {
				DefaultBackground = "#FFF0F0F0",
				BackgroundResourceKey = "TabItem.Static.Background",
			},
			new BrushColorInfo(ColorType.TabItemStaticBorder, "TabItem Static Border") {
				DefaultBackground = "#FFACACAC",
				BackgroundResourceKey = "TabItem.Static.Border",
			},
			new BrushColorInfo(ColorType.TabItemMouseOverBackground, "TabItem MouseOver Background") {
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "TabItem.MouseOver.Background",
			},
			new BrushColorInfo(ColorType.TabItemMouseOverBorder, "TabItem MouseOver Border") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "TabItem.MouseOver.Border",
			},
			new BrushColorInfo(ColorType.TabItemSelectedBackground, "TabItem Selected Background") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "TabItem.Selected.Background",
			},
			new BrushColorInfo(ColorType.TabItemSelectedBorder, "TabItem Selected Border") {
				DefaultBackground = "#FFACACAC",
				BackgroundResourceKey = "TabItem.Selected.Border",
			},
			new BrushColorInfo(ColorType.TabItemDisabledBackground, "TabItem Disabled Background") {
				DefaultBackground = "#FFF0F0F0",
				BackgroundResourceKey = "TabItem.Disabled.Background",
			},
			new BrushColorInfo(ColorType.TabItemDisabledBorder, "TabItem Disabled Border") {
				DefaultBackground = "#FFD9D9D9",
				BackgroundResourceKey = "TabItem.Disabled.Border",
			},
			new BrushColorInfo(ColorType.ListBoxBackground, "ListBox background") {
				DefaultBackground = "#F5F5F5",
				BackgroundResourceKey = "ListBoxBackground",
			},
			new BrushColorInfo(ColorType.ListBoxBorder, "ListBox border") {
				DefaultBackground = "#CCCEDB",
				BackgroundResourceKey = "ListBoxBorder",
			},
			new BrushColorInfo(ColorType.ListBoxItemMouseOverBackground, "ListBoxItem MouseOver Background") {
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "ListBoxItem.MouseOver.Background",
			},
			new BrushColorInfo(ColorType.ListBoxItemMouseOverBorder, "ListBoxItem MouseOver Border") {
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "ListBoxItem.MouseOver.Border",
			},
			new BrushColorInfo(ColorType.ListBoxItemSelectedInactiveBackground, "ListBoxItem SelectedInactive Background") {
				DefaultBackground = "#80C9DEF5",
				BackgroundResourceKey = "ListBoxItem.SelectedInactive.Background",
			},
			new BrushColorInfo(ColorType.ListBoxItemSelectedInactiveBorder, "ListBoxItem SelectedInactive Border") {
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "ListBoxItem.SelectedInactive.Border",
			},
			new BrushColorInfo(ColorType.ListBoxItemSelectedActiveBackground, "ListBoxItem SelectedActive Background") {
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "ListBoxItem.SelectedActive.Background",
			},
			new BrushColorInfo(ColorType.ListBoxItemSelectedActiveBorder, "ListBoxItem SelectedActive Border") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "ListBoxItem.SelectedActive.Border",
			},
			new BrushColorInfo(ColorType.ContextMenuBackground, "Context menu background") {
				DefaultBackground = "#F6F6F6",
				BackgroundResourceKey = "ContextMenuBackground",
			},
			new BrushColorInfo(ColorType.ContextMenuBorderBrush, "Context menu border brush") {
				DefaultBackground = "#FF959595",
				BackgroundResourceKey = "ContextMenuBorderBrush",
			},
			new BrushColorInfo(ColorType.ContextMenuRectangleFill1, "Context menu rectangle fill #1. It's the vertical rectangle on the left side.") {
				DefaultBackground = "#F6F6F6",
				BackgroundResourceKey = "ContextMenuRectangleFill1",
			},
			new BrushColorInfo(ColorType.ContextMenuRectangleFill2, "Context menu rectangle fill #2. It's the small vertical rectangle to the right of the left most vertical rectangle.") {
				DefaultBackground = "#F6F6F6",
				BackgroundResourceKey = "ContextMenuRectangleFill2",
			},
			new BrushColorInfo(ColorType.ContextMenuRectangleFill3, "Context menu rectangle fill #3") {
				DefaultBackground = "#F6F6F6",
				BackgroundResourceKey = "ContextMenuRectangleFill3",
			},
			new BrushColorInfo(ColorType.ExpanderStaticCircleStroke, "Expander Static Circle Stroke") {
				DefaultBackground = "#FF333333",
				BackgroundResourceKey = "Expander.Static.Circle.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderStaticCircleFill, "Expander Static Circle Fill") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "Expander.Static.Circle.Fill",
			},
			new BrushColorInfo(ColorType.ExpanderStaticArrowStroke, "Expander Static Arrow Stroke") {
				DefaultBackground = "#FF333333",
				BackgroundResourceKey = "Expander.Static.Arrow.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderMouseOverCircleStroke, "Expander MouseOver Circle Stroke") {
				DefaultBackground = "#FF5593FF",
				BackgroundResourceKey = "Expander.MouseOver.Circle.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderMouseOverCircleFill, "Expander MouseOver Circle Fill") {
				DefaultBackground = "#FFF3F9FF",
				BackgroundResourceKey = "Expander.MouseOver.Circle.Fill",
			},
			new BrushColorInfo(ColorType.ExpanderMouseOverArrowStroke, "Expander MouseOver Arrow Stroke") {
				DefaultBackground = "#FF000000",
				BackgroundResourceKey = "Expander.MouseOver.Arrow.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderPressedCircleStroke, "Expander Pressed Circle Stroke") {
				DefaultBackground = "#FF3C77DD",
				BackgroundResourceKey = "Expander.Pressed.Circle.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderPressedCircleFill, "Expander.Pressed.Circle.Fill") {
				DefaultBackground = "#FFD9ECFF",
				BackgroundResourceKey = "Expander.Pressed.Circle.Fill",
			},
			new BrushColorInfo(ColorType.ExpanderPressedArrowStroke, "Expander Pressed Arrow Stroke") {
				DefaultBackground = "#FF000000",
				BackgroundResourceKey = "Expander.Pressed.Arrow.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderDisabledCircleStroke, "Expander Disabled Circle Stroke") {
				DefaultBackground = "#FFBCBCBC",
				BackgroundResourceKey = "Expander.Disabled.Circle.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderDisabledCircleFill, "Expander Disabled Circle Fill") {
				DefaultBackground = "#FFE6E6E6",
				BackgroundResourceKey = "Expander.Disabled.Circle.Fill",
			},
			new BrushColorInfo(ColorType.ExpanderDisabledArrowStroke, "Expander Disabled Arrow Stroke") {
				DefaultBackground = "#FF707070",
				BackgroundResourceKey = "Expander.Disabled.Arrow.Stroke",
			},
			new BrushColorInfo(ColorType.ProgressBarProgress, "ProgressBar Progress") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "ProgressBarProgress",
			},
			new BrushColorInfo(ColorType.ProgressBarBackground, "ProgressBar Background") {
				DefaultBackground = "#FFFEFEFE",
				BackgroundResourceKey = "ProgressBarBackground",
			},
			new BrushColorInfo(ColorType.ProgressBarBorder, "ProgressBar Border") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "ProgressBarBorder",
			},
			new LinearGradientColorInfo(ColorType.ResizeGripperForeground, new Point(0, 0.25), new Point(1, 0.75), "ResizeGripper foreground", 0.3, 0.75, 1) {
				ResourceKey = "ResizeGripperForeground",
				DefaultForeground = "#FFFFFF",
				DefaultBackground = "#BBC5D7",
				DefaultColor3 = "#6D83A9",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarArrowBackground, "ScrollBar arrow background") {
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "EnvironmentScrollBarArrowBackground",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarArrowDisabledBackground, "ScrollBar arrow disabled background") {
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "EnvironmentScrollBarArrowDisabledBackground",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarArrowGlyph, "ScrollBar arrow glyph") {
				DefaultBackground = "#FF868999",
				BackgroundResourceKey = "EnvironmentScrollBarArrowGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarArrowGlyphDisabled, "ScrollBar arrow glyph disabled") {
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "EnvironmentScrollBarArrowGlyphDisabled",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarArrowGlyphMouseOver, "ScrollBar arrow glyph mouse over") {
				DefaultBackground = "#FF1C97EA",
				BackgroundResourceKey = "EnvironmentScrollBarArrowGlyphMouseOver",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarArrowGlyphPressed, "ScrollBar arrow glyph pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "EnvironmentScrollBarArrowGlyphPressed",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarArrowMouseOverBackground, "ScrollBar arrow mouse over background") {
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "EnvironmentScrollBarArrowMouseOverBackground",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarArrowPressedBackground, "ScrollBar arrow pressed background") {
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "EnvironmentScrollBarArrowPressedBackground",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarBackground, "ScrollBar background") {
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "EnvironmentScrollBarBackground",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarBorder, "ScrollBar border") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentScrollBarBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarThumbBackground, "ScrollBar thumb background") {
				DefaultBackground = "#FFDEDFE7",
				BackgroundResourceKey = "EnvironmentScrollBarThumbBackground",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarThumbDisabled, "ScrollBar thumb disabled") {
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "EnvironmentScrollBarThumbDisabled",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarThumbMouseOverBackground, "ScrollBar thumb mouse over background") {
				DefaultBackground = "#FF888888",
				BackgroundResourceKey = "EnvironmentScrollBarThumbMouseOverBackground",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarThumbPressedBackground, "ScrollBar thumb pressed background") {
				DefaultBackground = "#FF6A6A6A",
				BackgroundResourceKey = "EnvironmentScrollBarThumbPressedBackground",
			},
			new BrushColorInfo(ColorType.StatusBarDebugging, "StatusBar debugging") {
				DefaultBackground = "#CA5100",
				BackgroundResourceKey = "StatusBarDebuggingBackground",
				DefaultForeground = "White",
				ForegroundResourceKey = "StatusBarDebuggingForeground",
			},
			new LinearGradientColorInfo(ColorType.ToolTipBackground, new Point(0, 1), "ToolTip background", 0, 1) {
				ResourceKey = "ToolTipBackground",
				DefaultForeground = "White",
				DefaultBackground = "White",
			},
			new BrushColorInfo(ColorType.ToolTipBorderBrush, "ToolTip border brush") {
				DefaultBackground = "#767676",
				BackgroundResourceKey = "ToolTipBorderBrush",
			},
			new BrushColorInfo(ColorType.ToolTipForeground, "ToolTip foreground") {
				DefaultForeground = "Black",
				ForegroundResourceKey = "ToolTipForeground",
			},
			new BrushColorInfo(ColorType.CodeToolTip, "Code ToolTip") {
				DefaultForeground = "#FF1E1E1E",// Environment.ToolTip (fg)
				ForegroundResourceKey = "CodeToolTipForeground",
				DefaultBackground = "#FFF6F6F6",// Environment.ToolTip (bg)
				BackgroundResourceKey = "CodeToolTipBackground",
				Children = new ColorInfo[] {
					new BrushColorInfo(ColorType.XmlDocToolTipDescriptionText, "XML doc tooltip: base class of most XML doc tooltip classes") {
						Children = new ColorInfo[] {
							new BrushColorInfo(ColorType.XmlDocToolTipColon, "XML doc tooltip: colon"),
							new BrushColorInfo(ColorType.XmlDocToolTipExample, "XML doc tooltip: \"Example\" string"),
							new BrushColorInfo(ColorType.XmlDocToolTipExceptionCref, "XML doc tooltip: cref attribute in an exception tag"),
							new BrushColorInfo(ColorType.XmlDocToolTipReturns, "XML doc tooltip: \"Returns\" string"),
							new BrushColorInfo(ColorType.XmlDocToolTipSeeCref, "XML doc tooltip: cref attribute in a see tag"),
							new BrushColorInfo(ColorType.XmlDocToolTipSeeLangword, "XML doc tooltip: langword attribute in a see tag"),
							new BrushColorInfo(ColorType.XmlDocToolTipSeeAlso, "XML doc tooltip: \"See also\" string"),
							new BrushColorInfo(ColorType.XmlDocToolTipSeeAlsoCref, "XML doc tooltip: cref attribute in a seealso tag"),
							new BrushColorInfo(ColorType.XmlDocToolTipParamRefName, "XML doc tooltip: name attribute in a paramref tag"),
							new BrushColorInfo(ColorType.XmlDocToolTipParamName, "XML doc tooltip: name attribute in a param tag"),
							new BrushColorInfo(ColorType.XmlDocToolTipTypeParamName, "XML doc tooltip: name attribute in a typeparam tag"),
							new BrushColorInfo(ColorType.XmlDocToolTipValue, "XML doc tooltip: \"Value\" string"),
						}
					},
					new BrushColorInfo(ColorType.XmlDocSummary, "XML doc tooltip: summary text"),
					new BrushColorInfo(ColorType.XmlDocToolTipText, "XML doc tooltip: XML doc text"),
				}
			},
			new BrushColorInfo(ColorType.CodeToolTipBorder, "Code ToolTip border") {
				DefaultBackground = "#FFCCCEDB",// Environment.ToolTipBorder
				BackgroundResourceKey = "CodeToolTipBorder",
			},
			new BrushColorInfo(ColorType.TVExpanderBorderBrush, "TreeView expander border brush") {
				DefaultBackground = "#FF7898B5",
				BackgroundResourceKey = "TVExpanderBorderBrush",
			},
			new LinearGradientColorInfo(ColorType.TVExpanderBorderBackground, new Point(1, 1), "TreeView expander border background", 0.2, 1) {
				ResourceKey = "TVExpanderBorderBackground",
				DefaultForeground = "#FFFFFFFF",
				DefaultBackground = "#FFFFFFFF",
			},
			new BrushColorInfo(ColorType.TVExpanderPathFill, "TreeView expander path fill") {
				DefaultBackground = "Black",
				BackgroundResourceKey = "TVExpanderPathFill",
			},
			new BrushColorInfo(ColorType.TVExpanderMouseOverBorderBrush, "TreeView expander mouse over border brush") {
				DefaultBackground = "#37CAF7",
				BackgroundResourceKey = "TVExpanderMouseOverBorderBrush",
			},
			new BrushColorInfo(ColorType.TVExpanderMouseOverPathFill, "TreeView expander mouse over path fill") {
				DefaultBackground = "#37CAF7",
				BackgroundResourceKey = "TVExpanderMouseOverPathFill",
			},
			new BrushColorInfo(ColorType.TVItemAlternationBackground, "TreeViewItem alternation background") {
				DefaultBackground = "WhiteSmoke",
				BackgroundResourceKey = "TVItemAlternationBackground",
			},
			new BrushColorInfo(ColorType.GridViewBackground, "GridView background") {
				DefaultBackground = "#F5F5F5",
				BackgroundResourceKey = "GridViewBackground",
			},
			new BrushColorInfo(ColorType.GridViewBorder, "GridView border") {
				DefaultBackground = "#CCCEDB",
				BackgroundResourceKey = "GridViewBorder",
			},
			new LinearGradientColorInfo(ColorType.GridViewColumnHeaderBorderBackground, new Point(0, 1), "GridViewColumnHeader border background", 0, 1) {
				ResourceKey = "GridViewColumnHeaderBorderBackground",
				DefaultForeground = "#E0E3E6",
				DefaultBackground = "#E0E3E6",
			},
			new LinearGradientColorInfo(ColorType.GridViewColumnHeaderBackground, new Point(0, 1), "GridViewColumnHeader background", 0, 0.4091, 1) {
				ResourceKey = "GridViewColumnHeaderBackground",
				DefaultForeground = "#F5F5F5",
				DefaultBackground = "#F5F5F5",
				DefaultColor3 = "#F5F5F5",
			},
			new LinearGradientColorInfo(ColorType.GridViewColumnHeaderHoverBackground, new Point(0, 1), "GridViewColumnHeader hover background", 0, 1) {
				ResourceKey = "GridViewColumnHeaderHoverBackground",
				DefaultForeground = "#1C97EA",
				DefaultBackground = "#1C97EA",
			},
			new LinearGradientColorInfo(ColorType.GridViewColumnHeaderPressBackground, new Point(0, 1), "GridViewColumnHeader press background", 0, 1) {
				ResourceKey = "GridViewColumnHeaderPressBackground",
				DefaultForeground = "#007ACC",
				DefaultBackground = "#007ACC",
			},
			new BrushColorInfo(ColorType.GridViewColumnHeaderUpperHighlightFill, "GridViewColumnHeader upper highlight fill") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "GridViewColumnHeaderUpperHighlightFill",
			},
			new BrushColorInfo(ColorType.GridViewColumnHeaderMouseOverHeaderHoverBorder, "GridViewColumnHeader mouse over header hover border") {
				DefaultBackground = "#1C97EA",
				BackgroundResourceKey = "GridViewColumnHeaderMouseOverHeaderHoverBorder",
			},
			new BrushColorInfo(ColorType.GridViewColumnHeaderPressHoverBorder, "GridViewColumnHeader press hover border") {
				DefaultBackground = "#007ACC",
				BackgroundResourceKey = "GridViewColumnHeaderPressHoverBorder",
			},
			new BrushColorInfo(ColorType.GridViewColumnHeaderPressBorder, "GridViewColumnHeader press border") {
				DefaultBackground = "#007ACC",
				BackgroundResourceKey = "GridViewColumnHeaderPressBorder",
			},
			new BrushColorInfo(ColorType.GridViewColumnHeaderPressUpperHighlightFill, "GridViewColumnHeader press upper highlight fill") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "GridViewColumnHeaderPressUpperHighlightFill",
			},
			new BrushColorInfo(ColorType.GridViewColumnHeaderFloatingHeaderCanvasFill, "GridViewColumnHeaderFloatingHeaderCanvasFill") {
				DefaultBackground = "#FF000000",
				BackgroundResourceKey = "GridViewColumnHeaderFloatingHeaderCanvasFill",
			},
			new BrushColorInfo(ColorType.GridViewListViewForeground, "GridView ListView foreground") {
				DefaultBackground = "#1E1E1E",
				BackgroundResourceKey = "GridViewListViewForeground",
			},
			new BrushColorInfo(ColorType.GridViewListViewItemFocusVisualStroke, "GridView ListViewItem FocusVisual stroke") {
				DefaultBackground = "#3399FF",
				BackgroundResourceKey = "GridViewListViewItemFocusVisualStroke",
			},
			new LinearGradientColorInfo(ColorType.GridViewListItemHoverFill, new Point(0, 1), "GridView ListItem hover fill", 0, 1) {
				ResourceKey = "ListItemHoverFill",
				DefaultForeground = "#5CADFF",
				DefaultBackground = "#5CADFF",
			},
			new LinearGradientColorInfo(ColorType.GridViewListItemSelectedFill, new Point(0, 1), "GridView ListItem selected fill", 0, 1) {
				ResourceKey = "ListItemSelectedFill",
				DefaultForeground = "#3399FF",
				DefaultBackground = "#3399FF",
			},
			new LinearGradientColorInfo(ColorType.GridViewListItemSelectedHoverFill, new Point(0, 1), "GridView ListItem selected hover fill", 0, 1) {
				ResourceKey = "ListItemSelectedHoverFill",
				DefaultForeground = "#85C2FF",
				DefaultBackground = "#85C2FF",
			},
			new LinearGradientColorInfo(ColorType.GridViewListItemSelectedInactiveFill, new Point(0, 1), "GridView ListItem selected inactive fill", 0, 1) {
				ResourceKey = "ListItemSelectedInactiveFill",
				DefaultForeground = "#AAAAAA",
				DefaultBackground = "#AAAAAA",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerUpperHighlightFill, "GridView ItemContainer upper highlight fill") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "GridViewItemContainerUpperHighlightFill",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerMouseOverHoverBorder, "GridView ItemContainer mouse over hover border") {
				DefaultBackground = "#5CADFF",
				BackgroundResourceKey = "GridViewItemContainerMouseOverHoverBorder",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedBorder, "GridView ItemContainer selected border") {
				DefaultBackground = "#3399FF",
				BackgroundResourceKey = "GridViewItemContainerSelectedBorder",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedInnerBorder, "GridView ItemContainer selected inner border") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "GridViewItemContainerSelectedInnerBorder",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedUpperHighlightFill, "GridView ItemContainer selected upper highlight fill") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "GridViewItemContainerSelectedUpperHighlightFill",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedInactiveBorder, "GridView ItemContainer selected inactive border") {
				DefaultBackground = "#AAAAAA",
				BackgroundResourceKey = "GridViewItemContainerSelectedInactiveBorder",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedMouseOverBorder, "GridView ItemContainer selected mouse over border brush") {
				DefaultBackground = "#85C2FF",
				BackgroundResourceKey = "GridViewItemContainerSelectedMouseOverBorder",
			},
			new BrushColorInfo(ColorType.SortableGridViewColumnArrowBackground, "SortableGridViewColumn arrow background") {
				DefaultBackground = "#1E1E1E",
				BackgroundResourceKey = "SortableGridViewColumnArrowBackground",
			},
			new BrushColorInfo(ColorType.PaneBorder, "Pane border") {
				DefaultBackground = "#828790",
				BackgroundResourceKey = "PaneBorder",
			},
			new LinearGradientColorInfo(ColorType.DockedPaneCloseButtonBackground, new Point(0, 1), "DockedPane close button background", 0, 0.5, 0.5, 1) {
				ResourceKey = "DockedPaneCloseButtonBackground",
				DefaultForeground = "#F4F4F4",
				DefaultBackground = "#EBEBEB",
				DefaultColor3 = "#DEDEDE",
				DefaultColor4 = "#D0D0D0",
			},
			new BrushColorInfo(ColorType.DockedPaneCloseButtonBorder, "DockedPane close button border") {
				DefaultBackground = "#FF989898",
				BackgroundResourceKey = "DockedPaneCloseButtonBorder",
			},
			new LinearGradientColorInfo(ColorType.DockedPaneCloseButtonMouseOverBackground, new Point(0, 1), "DockedPane close button mouse over background", 0, 1) {
				ResourceKey = "DockedPaneCloseButtonMouseOverBackground",
				DefaultForeground = "#D7E3FC",
				DefaultBackground = "#B5C8ED",
			},
			new LinearGradientColorInfo(ColorType.DockedPaneCloseButtonPressedBackground, new Point(0, 1), "DockedPane close button pressed background", 0, 1) {
				ResourceKey = "DockedPaneCloseButtonPressedBackground",
				DefaultForeground = "#E1E1E1",
				DefaultBackground = "#F8F8F8",
			},
			new BrushColorInfo(ColorType.DockedPaneCloseButtonPathStroke, "DockedPane close button path stroke") {
				DefaultBackground = "#FF333333",
				BackgroundResourceKey = "DockedPaneCloseButtonPathStroke",
			},
			new BrushColorInfo(ColorType.DockedPaneCloseButtonPathFill, "DockedPane close button path fill") {
				DefaultBackground = "#FF969696",
				BackgroundResourceKey = "DockedPaneCloseButtonPathFill",
			},
			new BrushColorInfo(ColorType.DockedPaneTitleForeground, "DockedPane title foreground") {
				DefaultForeground = "#FF000000",
				ForegroundResourceKey = "DockedPaneTitleForeground",
			},
			new BrushColorInfo(ColorType.DecompilerTextViewWaitAdorner, "DecompilerTextView wait adorner") {
				DefaultForeground = "Black",
				ForegroundResourceKey = "DecompilerTextViewWaitAdornerForeground",
				DefaultBackground = "#C0FFFFFF",
				BackgroundResourceKey = "DecompilerTextViewWaitAdornerBackground",
			},
			new ColorColorInfo(ColorType.ResourceTableAlternationBackground1, "Resource table alternation background #1") {
				DefaultBackground = "White",
				BackgroundResourceKey = "ResourceTableAlternationBackground1",
			},
			new ColorColorInfo(ColorType.ResourceTableAlternationBackground2, "Resource table alternation background #2") {
				DefaultBackground = "Beige",
				BackgroundResourceKey = "ResourceTableAlternationBackground2",
			},
			new BrushColorInfo(ColorType.AvalonEditSearchDropDownButtonActiveBorder, "AvalonEdit search drop down button active border") {
				DefaultBackground = "#FF0A246A",
				BackgroundResourceKey = "AvalonEditSearchDropDownButtonActiveBorder",
			},
			new BrushColorInfo(ColorType.AvalonEditSearchDropDownButtonActiveBackground, "AvalonEdit search drop down button active Background") {
				DefaultBackground = "#FFB6BDD2",
				BackgroundResourceKey = "AvalonEditSearchDropDownButtonActiveBackground",
			},
			new BrushColorInfo(ColorType.ListArrowBackground, "List arrow background") {
				DefaultBackground = "Black",
				BackgroundResourceKey = "ListArrowBackground",
			},
			new BrushColorInfo(ColorType.TreeViewLineBackground, "TreeView line background") {
				DefaultBackground = "LightGray",
				BackgroundResourceKey = "TreeViewLineBackground",
			},
			new BrushColorInfo(ColorType.TreeViewItemMouseOver, "TreeViewItem mouse over") {
				DefaultBackground = "#006699",
				BackgroundResourceKey = "TreeViewItemMouseOverTextBackground",
				DefaultForeground = "White",
				ForegroundResourceKey = "TreeViewItemMouseOverForeground",
			},
			new BrushColorInfo(ColorType.TreeViewBackground, "TreeView background") {
				DefaultBackground = "#F5F5F5",
				BackgroundResourceKey = "TreeViewBackground",
			},
			new BrushColorInfo(ColorType.TreeViewBorder, "TreeView border") {
				DefaultBackground = "#CCCEDB",
				BackgroundResourceKey = "TreeViewBorder",
			},
			new BrushColorInfo(ColorType.IconBar, "IconBar") {
				DefaultBackground = "#E6E7E8",
			},
			new BrushColorInfo(ColorType.IconBarBorder, "IconBar") {
				DefaultBackground = "#CFD0D1",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabBackground, "FileTab background") {
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "EnvironmentFileTabBackground",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabBorder, "FileTab border") {
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "EnvironmentFileTabBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonDownInactiveBorder, "FileTab button down inactive border") {
				DefaultBackground = "#FF0E6198",
				BackgroundResourceKey = "EnvironmentFileTabButtonDownInactiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonDownInactive, "FileTab button down inactive") {
				DefaultBackground = "#FF0E6198",
				BackgroundResourceKey = "EnvironmentFileTabButtonDownInactive",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonDownInactiveGlyph, "FileTab button down inactive glyph") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentFileTabButtonDownInactiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonDownSelectedActiveBorder, "FileTab button down selected active border") {
				DefaultBackground = "#FF0E6198",
				BackgroundResourceKey = "EnvironmentFileTabButtonDownSelectedActiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonDownSelectedActive, "FileTab button down selected active") {
				DefaultBackground = "#FF0E6198",
				BackgroundResourceKey = "EnvironmentFileTabButtonDownSelectedActive",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonDownSelectedActiveGlyph, "FileTab button down selected active glyph") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentFileTabButtonDownSelectedActiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonDownSelectedInactiveBorder, "FileTab button down selected inactive border") {
				DefaultBackground = "#FFB7B9C5",
				BackgroundResourceKey = "EnvironmentFileTabButtonDownSelectedInactiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonDownSelectedInactive, "FileTab button down selected inactive") {
				DefaultBackground = "#FFB7B9C5",
				BackgroundResourceKey = "EnvironmentFileTabButtonDownSelectedInactive",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonDownSelectedInactiveGlyph, "FileTab button down selected inactive glyph") {
				DefaultBackground = "#FF2D2D2D",
				BackgroundResourceKey = "EnvironmentFileTabButtonDownSelectedInactiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonHoverInactiveBorder, "FileTab button hover inactive border") {
				DefaultBackground = "#FF52B0EF",
				BackgroundResourceKey = "EnvironmentFileTabButtonHoverInactiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonHoverInactive, "FileTab button hover inactive") {
				DefaultBackground = "#FF52B0EF",
				BackgroundResourceKey = "EnvironmentFileTabButtonHoverInactive",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonHoverInactiveGlyph, "FileTab button hover inactive glyph") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentFileTabButtonHoverInactiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonHoverSelectedActiveBorder, "FileTab button hover selected active border") {
				DefaultBackground = "#FF1C97EA",
				BackgroundResourceKey = "EnvironmentFileTabButtonHoverSelectedActiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonHoverSelectedActive, "FileTab button hover selected active") {
				DefaultBackground = "#FF1C97EA",
				BackgroundResourceKey = "EnvironmentFileTabButtonHoverSelectedActive",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonHoverSelectedActiveGlyph, "FileTab button hover selected active glyph") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentFileTabButtonHoverSelectedActiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonHoverSelectedInactiveBorder, "FileTab button hover selected inactive border") {
				DefaultBackground = "#FFE6E7ED",
				BackgroundResourceKey = "EnvironmentFileTabButtonHoverSelectedInactiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonHoverSelectedInactive, "FileTab button hover selected inactive") {
				DefaultBackground = "#FFE6E7ED",
				BackgroundResourceKey = "EnvironmentFileTabButtonHoverSelectedInactive",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonHoverSelectedInactiveGlyph, "FileTab button hover selected inactive glyph") {
				DefaultBackground = "#FF717171",
				BackgroundResourceKey = "EnvironmentFileTabButtonHoverSelectedInactiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonSelectedActiveGlyph, "FileTab button selected active glyph") {
				DefaultBackground = "#FFD0E6F5",
				BackgroundResourceKey = "EnvironmentFileTabButtonSelectedActiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabButtonSelectedInactiveGlyph, "FileTab button selected inactive glyph") {
				DefaultBackground = "#FF6D6D70",
				BackgroundResourceKey = "EnvironmentFileTabButtonSelectedInactiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabInactiveBorder, "FileTab inactive border") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "EnvironmentFileTabInactiveBorder",
			},
			new LinearGradientColorInfo(ColorType.EnvironmentFileTabInactiveGradient, new Point(0, 1), "FileTab inactive gradient", 0, 1) {
				ResourceKey = "EnvironmentFileTabInactiveGradient",
				DefaultForeground = "#FFCCCEDB",// Environment.FileTabInactiveGradientTop
				DefaultBackground = "#FFCCCEDB",// Environment.FileTabInactiveGradientBottom
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabInactiveText, "FileTab inactive text") {
				DefaultBackground = "#FF717171",
				BackgroundResourceKey = "EnvironmentFileTabInactiveText",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabSelectedBorder, "FileTab selected border") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "EnvironmentFileTabSelectedBorder",
			},
			new LinearGradientColorInfo(ColorType.EnvironmentFileTabSelectedGradient, new Point(0, 1), "FileTab selected gradient", 0, 0.5, 0.5, 1) {
				ResourceKey = "EnvironmentFileTabSelectedGradient",
				DefaultForeground = "#FF007ACC",// Environment.FileTabSelectedGradientTop
				DefaultBackground = "#FF007ACC",// Environment.FileTabSelectedGradientMiddle1
				DefaultColor3 = "#FF007ACC",// Environment.FileTabSelectedGradientMiddle2
				DefaultColor4 = "#FF007ACC",// Environment.FileTabSelectedGradientBottom
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabSelectedText, "FileTab selected text") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentFileTabSelectedText",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabText, "FileTab text") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "EnvironmentFileTabText",
			},
			new LinearGradientColorInfo(ColorType.EnvironmentFileTabHotGradient, new Point(0, 1), "FileTab hot gradient", 0, 1) {
				ResourceKey = "EnvironmentFileTabHotGradient",
				DefaultForeground = "#FF1C97EA",// Environment.FileTabHotGradientTop
				DefaultBackground = "#FF1C97EA",// Environment.FileTabHotGradientBottom
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabHotBorder, "FileTab hot border") {
				DefaultBackground = "#FF1C97EA",
				BackgroundResourceKey = "EnvironmentFileTabHotBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabHotText, "FileTab hot text") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentFileTabHotText",
			},
			new BrushColorInfo(ColorType.EnvironmentFileTabHotGlyph, "FileTab hot glyph") {
				DefaultBackground = "#FFD0E6F5",
				BackgroundResourceKey = "EnvironmentFileTabHotGlyph",
			},
			new BrushColorInfo(ColorType.SearchBoxWatermark, "SearchBox Watermark") {
				DefaultForeground = "#FF6D6D6D",
				ForegroundResourceKey = "SearchBoxWatermarkForeground",
			},
			new BrushColorInfo(ColorType.NodeAutoLoaded, "TreeView node auto loaded") {
				DefaultForeground = "SteelBlue",
			},
			new BrushColorInfo(ColorType.NodePublic, "TreeView node public") {
				DefaultForeground = "#FF000000",
			},
			new BrushColorInfo(ColorType.NodeNotPublic, "TreeView node not public") {
				DefaultForeground = "#FF6D6D6D",
			},
			new BrushColorInfo(ColorType.DefaultText, "Default text") {
				DefaultForeground = "Black",
				DefaultBackground = "White",
				Children = new ColorInfo[] {
					new BrushColorInfo(ColorType.Text, "Default text color in text view") {
						Children = new ColorInfo[] {
							new BrushColorInfo(ColorType.Punctuation, "Punctuation") {
								Children = new ColorInfo[] {
									new BrushColorInfo(ColorType.Brace, "Braces: {}"),
									new BrushColorInfo(ColorType.Operator, "+-/etc and other special chars like ,; etc"),
								},
							},
							new BrushColorInfo(ColorType.Comment, "Comments"),
							new BrushColorInfo(ColorType.Xml, "XML") {
								Children = new ColorInfo[] {
									new BrushColorInfo(ColorType.XmlDocTag, "XML doc tag"),
									new BrushColorInfo(ColorType.XmlDocAttribute, "XML doc attribute"),
									new BrushColorInfo(ColorType.XmlDocComment, "XML doc comment"),
									new BrushColorInfo(ColorType.XmlComment, "XML comment"),
									new BrushColorInfo(ColorType.XmlCData, "XML CData"),
									new BrushColorInfo(ColorType.XmlDocType, "XML doc type"),
									new BrushColorInfo(ColorType.XmlDeclaration, "XML declaration"),
									new BrushColorInfo(ColorType.XmlTag, "XML tag"),
									new BrushColorInfo(ColorType.XmlAttributeName, "XML attribute name"),
									new BrushColorInfo(ColorType.XmlAttributeValue, "XML attribute value"),
									new BrushColorInfo(ColorType.XmlEntity, "XML entity"),
									new BrushColorInfo(ColorType.XmlBrokenEntity, "XML broken entity")
								},
							},
							new BrushColorInfo(ColorType.Literal, "Literal") {
								Children = new ColorInfo[] {
									new BrushColorInfo(ColorType.Number, "Numbers"),
									new BrushColorInfo(ColorType.String, "String"),
									new BrushColorInfo(ColorType.Char, "Char")
								},
							},
							new BrushColorInfo(ColorType.Identifier, "Identifier") {
								Children = new ColorInfo[] {
									new BrushColorInfo(ColorType.Keyword, "Keyword"),
									new BrushColorInfo(ColorType.NamespacePart, "Namespace"),
									new BrushColorInfo(ColorType.Type, "Type") {
										Children = new ColorInfo[] {
											new BrushColorInfo(ColorType.StaticType, "Static type"),
											new BrushColorInfo(ColorType.Delegate, "Delegate"),
											new BrushColorInfo(ColorType.Enum, "Enum"),
											new BrushColorInfo(ColorType.Interface, "Interface"),
											new BrushColorInfo(ColorType.ValueType, "Value type")
										},
									},
									new BrushColorInfo(ColorType.GenericParameter, "Generic parameter") {
										Children = new ColorInfo[] {
											new BrushColorInfo(ColorType.TypeGenericParameter, "Generic type parameter"),
											new BrushColorInfo(ColorType.MethodGenericParameter, "Generic method parameter")
										},
									},
									new BrushColorInfo(ColorType.Method, "Method") {
										Children = new ColorInfo[] {
											new BrushColorInfo(ColorType.InstanceMethod, "Instance method"),
											new BrushColorInfo(ColorType.StaticMethod, "Static method"),
											new BrushColorInfo(ColorType.ExtensionMethod, "Extension method")
										},
									},
									new BrushColorInfo(ColorType.Field, "Field") {
										Children = new ColorInfo[] {
											new BrushColorInfo(ColorType.InstanceField, "Instance field"),
											new BrushColorInfo(ColorType.EnumField, "Enum field"),
											new BrushColorInfo(ColorType.LiteralField, "Literal field"),
											new BrushColorInfo(ColorType.StaticField, "Static field")
										},
									},
									new BrushColorInfo(ColorType.Event, "Event") {
										Children = new ColorInfo[] {
											new BrushColorInfo(ColorType.InstanceEvent, "Instance event"),
											new BrushColorInfo(ColorType.StaticEvent, "Static event")
										},
									},
									new BrushColorInfo(ColorType.Property, "Property") {
										Children = new ColorInfo[] {
											new BrushColorInfo(ColorType.InstanceProperty, "Instance property"),
											new BrushColorInfo(ColorType.StaticProperty, "Static property")
										},
									},
									new BrushColorInfo(ColorType.Variable, "Local/parameter") {
										Children = new ColorInfo[] {
											new BrushColorInfo(ColorType.Local, "Local variable"),
											new BrushColorInfo(ColorType.Parameter, "Method parameter")
										},
									},
									new BrushColorInfo(ColorType.Label, "Label"),
									new BrushColorInfo(ColorType.OpCode, "Opcode"),
									new BrushColorInfo(ColorType.ILDirective, "IL directive"),
									new BrushColorInfo(ColorType.ILModule, "IL module")
								},
							},
							new BrushColorInfo(ColorType.LineNumber, "Line number"),
							new BrushColorInfo(ColorType.Link, "Link"),
							new BrushColorInfo(ColorType.LocalDefinition, "Local definition"),
							new BrushColorInfo(ColorType.LocalReference, "Local reference"),
							new BrushColorInfo(ColorType.CurrentStatement, "Current statement"),
							new BrushColorInfo(ColorType.ReturnStatement, "Return statement"),
							new BrushColorInfo(ColorType.SelectedReturnStatement, "Selected return statement"),
							new BrushColorInfo(ColorType.BreakpointStatement, "Breakpoint statement"),
							new BrushColorInfo(ColorType.DisabledBreakpointStatement, "Disabled breakpoint statement"),
						},
					},
				},
			},
		};
		static readonly ColorInfo[] colorInfos = new ColorInfo[(int)ColorType.Last];

		static Theme()
		{
			for (int i = 0; i < (int)TextTokenType.Last; i++) {
				var tt = ((TextTokenType)i).ToString();
				var ct = ((ColorType)i).ToString();
				if (tt != ct) {
					Debug.Fail("Token type is not a sub set of color type or order is not correct");
					throw new Exception("Token type is not a sub set of color type or order is not correct");
				}
			}

			foreach (var fi in typeof(ColorType).GetFields()) {
				if (!fi.IsLiteral)
					continue;
				var val = (ColorType)fi.GetValue(null);
				if (val == ColorType.Last)
					continue;
				nameToColorType[fi.Name] = val;
			}

			InitColorInfos(rootColorInfos);
			for (int i = 0; i < colorInfos.Length; i++) {
				var colorType = (ColorType)i;
				if (colorInfos[i] == null) {
					Debug.Fail(string.Format("Missing info: {0}", colorType));
					throw new Exception(string.Format("Missing info: {0}", colorType));
				}
			}
		}

		static void InitColorInfos(ColorInfo[] infos)
		{
			foreach (var info in infos) {
				int i = (int)info.ColorType;
				if (colorInfos[i] != null) {
					Debug.Fail("Duplicate");
					throw new Exception("Duplicate");
				}
				colorInfos[i] = info;
				InitColorInfos(info.Children);
			}
		}

		public Color[] Colors {
			get { return hlColors; }
		}
		Color[] hlColors = new Color[(int)ColorType.Last];

		public string Name { get; private set; }
		public string MenuName { get; private set; }
		public bool IsHighContrast { get; private set; }
		public int Sort { get; private set; }

		public Theme(XElement root)
		{
			var name = root.Attribute("name");
			if (name == null || string.IsNullOrEmpty(name.Value))
				throw new Exception("Missing or empty name attribute");
			this.Name = name.Value;

			var menuName = root.Attribute("menu-name");
			if (menuName == null || string.IsNullOrEmpty(menuName.Value))
				throw new Exception("Missing or empty menu-name attribute");
			this.MenuName = menuName.Value;

			var hcName = root.Attribute("is-high-contrast");
			if (hcName != null)
				this.IsHighContrast = (bool)hcName;
			else
				this.IsHighContrast = false;

			var sort = root.Attribute("sort");
			this.Sort = sort == null ? 1 : (int)sort;

			for (int i = 0; i < hlColors.Length; i++)
				hlColors[i] = new Color(colorInfos[i]);

			var colors = root.Element("colors");
			if (colors != null) {
				foreach (var color in colors.Elements("color")) {
					ColorType colorType = 0;
					var hl = ReadColor(color, ref colorType);
					if (hl == null)
						continue;
					hlColors[(int)colorType].OriginalColor = hl;
				}
			}
			for (int i = 0; i < hlColors.Length; i++) {
				if (hlColors[i].OriginalColor == null)
					hlColors[i].OriginalColor = CreateHighlightingColor((ColorType)i);
				hlColors[i].TextInheritedColor = new MyHighlightingColor { Name = hlColors[i].OriginalColor.Name };
				hlColors[i].InheritedColor = new MyHighlightingColor { Name = hlColors[i].OriginalColor.Name };
			}

			RecalculateInheritedColorProperties();
		}

		/// <summary>
		/// Recalculates the inherited color properties and should be called whenever any of the
		/// color properties have been modified.
		/// </summary>
		public void RecalculateInheritedColorProperties()
		{
			for (int i = 0; i < hlColors.Length; i++) {
				var info = colorInfos[i];
				var textColor = hlColors[i].TextInheritedColor;
				var color = hlColors[i].InheritedColor;
				if (info.ColorType == ColorType.DefaultText) {
					color.Foreground = textColor.Foreground = hlColors[(int)info.ColorType].OriginalColor.Foreground;
					color.Background = textColor.Background = hlColors[(int)info.ColorType].OriginalColor.Background;
					color.Color3 = textColor.Color3 = hlColors[(int)info.ColorType].OriginalColor.Color3;
					color.Color4 = textColor.Color4 = hlColors[(int)info.ColorType].OriginalColor.Color4;
					color.FontStyle = textColor.FontStyle = hlColors[(int)info.ColorType].OriginalColor.FontStyle;
					color.FontWeight = textColor.FontWeight = hlColors[(int)info.ColorType].OriginalColor.FontWeight;
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

		HighlightingBrush GetForeground(ColorInfo info, bool canIncludeDefault)
		{
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[(int)info.ColorType];
				var val = color.OriginalColor.Foreground;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		HighlightingBrush GetBackground(ColorInfo info, bool canIncludeDefault)
		{
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[(int)info.ColorType];
				var val = color.OriginalColor.Background;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		HighlightingBrush GetColor3(ColorInfo info, bool canIncludeDefault)
		{
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[(int)info.ColorType];
				var val = color.OriginalColor.Color3;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		HighlightingBrush GetColor4(ColorInfo info, bool canIncludeDefault)
		{
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[(int)info.ColorType];
				var val = color.OriginalColor.Color4;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		FontStyle? GetFontStyle(ColorInfo info, bool canIncludeDefault)
		{
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[(int)info.ColorType];
				var val = color.OriginalColor.FontStyle;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		FontWeight? GetFontWeight(ColorInfo info, bool canIncludeDefault)
		{
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[(int)info.ColorType];
				var val = color.OriginalColor.FontWeight;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		public Color GetColor(TextTokenType tokenType)
		{
			return GetColor((ColorType)tokenType);
		}

		public Color GetColor(ColorType colorType)
		{
			uint i = (uint)colorType;
			if (i >= (uint)hlColors.Length)
				return hlColors[(int)ColorType.DefaultText];
			return hlColors[i];
		}

		MyHighlightingColor ReadColor(XElement color, ref ColorType colorType)
		{
			var name = color.Attribute("name");
			if (name == null)
				return null;
			colorType = ToColorType(name.Value);
			if (colorType == ColorType.Last)
				return null;

			var colorInfo = colorInfos[(int)colorType];

			var hl = new MyHighlightingColor();
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

		MyHighlightingColor CreateHighlightingColor(ColorType colorType)
		{
			var hl = new MyHighlightingColor { Name = colorType.ToString() };

			var colorInfo = colorInfos[(int)colorType];

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

		static string GetAttribute(XElement xml, string attr, string defVal)
		{
			var a = xml.Attribute(attr);
			if (a != null)
				return a.Value;
			return defVal;
		}

		static readonly ColorConverter colorConverter = new ColorConverter();
		static HighlightingBrush CreateColor(string color)
		{
			if (color.StartsWith("SystemColors.")) {
				string shortName = color.Substring(13);
				var property = typeof(SystemColors).GetProperty(shortName + "Brush");
				Debug.Assert(property != null);
				if (property == null)
					return null;
				return new SystemColorHighlightingBrush(property);
			}

			try {
				var clr = (System.Windows.Media.Color?)colorConverter.ConvertFromInvariantString(color);
				return clr == null ? null : new SimpleHighlightingBrush(clr.Value);
			}
			catch {
				Debug.Fail(string.Format("Couldn't convert color '{0}'", color));
				throw;
			}
		}

		static ColorType ToColorType(string name)
		{
			ColorType type;
			if (nameToColorType.TryGetValue(name, out type))
				return type;
			return ColorType.Last;
		}

		public override string ToString()
		{
			return string.Format("Theme: {0}", Name);
		}
	}
}
