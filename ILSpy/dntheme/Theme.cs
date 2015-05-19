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

		public static BrushColorInfo CreateSystemColor(ColorType colorType, string name)
		{
			return new BrushColorInfo(colorType, "SystemColors." + name + "Brush") {
				DefaultForeground = "SystemColors." + name,
				ForegroundResourceKey = "SystemColors" + name,
			};
		}

		public BrushColorInfo(ColorType colorType, string description)
			: base(colorType, description)
		{
		}

		public override IEnumerable<Tuple<object, object>> GetResourceKeyValues(MyHighlightingColor hlColor)
		{
			if (ForegroundResourceKey != null) {
				Debug.Assert(hlColor.Foreground != null);
				yield return new Tuple<object, object>(ForegroundResourceKey, hlColor.Foreground.GetBrush(null));
			}
			if (BackgroundResourceKey != null) {
				Debug.Assert(hlColor.Background != null);
				yield return new Tuple<object, object>(BackgroundResourceKey, hlColor.Background.GetBrush(null));
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
				RelativeTransform = RelativeTransform,
			};
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
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsMenuText, "MenuText"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsGrayText, "GrayText"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsControl, "Control"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsControlText, "ControlText"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsControlLight, "ControlLight"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsControlLightLight, "ControlLightLight"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsControlDark, "ControlDark"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsControlDarkDark, "ControlDarkDark"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsWindowText, "WindowText"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsWindow, "Window"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsHighlight, "Highlight"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsHighlightText, "HighlightText"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsInactiveSelectionHighlight, "InactiveSelectionHighlight"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsInactiveSelectionHighlightText, "InactiveSelectionHighlightText"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsInactiveCaption, "InactiveCaption"),
			BrushColorInfo.CreateSystemColor(ColorType.SystemColorsInactiveCaptionText, "InactiveCaptionText"),
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
			new BrushColorInfo(ColorType.ListBorder, "List border") {
				DefaultBackground = "#828790",
				BackgroundResourceKey = "ListBorder",
			},
			new BrushColorInfo(ColorType.MenuItemSeparatorFillTop, "MenuItem Separator fill (top)") {
				DefaultBackground = "#E0E0E0",
				BackgroundResourceKey = "MenuItemSeparatorFillTop",
			},
			new BrushColorInfo(ColorType.MenuItemSeparatorFillBottom, "MenuItem Separator fill (bottom)") {
				DefaultBackground = "White",
				BackgroundResourceKey = "MenuItemSeparatorFillBottom",
			},
			new LinearGradientColorInfo(ColorType.MenuItemSelectionFill, new Point(0, 1), "MenuItem selection fill", 0, 1) {
				ResourceKey = "MenuItemSelectionFill",
				DefaultForeground = "#34C5EBFF",
				DefaultBackground = "#3481D8FF",
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
				DefaultBackground = "#0C12A1",
				BackgroundResourceKey = "MenuItemGlyphFill",
			},
			new BrushColorInfo(ColorType.MenuItemHighlightedStroke, "MenuItem highlighted stroke") {
				DefaultBackground = "#8071CBF1",
				BackgroundResourceKey = "MenuItemHighlightedStroke",
			},
			new BrushColorInfo(ColorType.MenuItemHighlightedInnerBorder, "MenuItem highlighted inner border") {
				DefaultBackground = "#40FFFFFF",
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
				DefaultBackground = "#99CCFF",
				BackgroundResourceKey = "ToolBarDarkFill",
			},
			new BrushColorInfo(ColorType.ToolBarButtonPressed, "Toolbar button pressed") {
				DefaultBackground = "#99CCFF",
				BackgroundResourceKey = "ToolBarButtonPressed",
			},
			new BrushColorInfo(ColorType.ToolBarSeparatorFill, "Toolbar separator fill color") {
				DefaultBackground = "#C6C7C6",
				BackgroundResourceKey = "ToolBarSeparatorFill",
			},
			new BrushColorInfo(ColorType.ToolBarButtonHover, "Toolbar button hover color") {
				DefaultBackground = "#C2E0FF",
				BackgroundResourceKey = "ToolBarButtonHover",
			},
			new BrushColorInfo(ColorType.ToolBarButtonHoverBorder, "Toolbar button hover border") {
				DefaultBackground = "#3399FF",
				BackgroundResourceKey = "ToolBarButtonHoverBorder",
			},
			new BrushColorInfo(ColorType.ToolBarButtonPressedBorder, "Toolbar button pressed border") {
				DefaultBackground = "#3399FF",
				BackgroundResourceKey = "ToolBarButtonPressedBorder",
			},
			new BrushColorInfo(ColorType.ToolBarMenuBorder, "Toolbar menu border") {
				DefaultBackground = "#808080",
				BackgroundResourceKey = "ToolBarMenuBorder",
			},
			new BrushColorInfo(ColorType.ToolBarSubMenuBackground, "Toolbar sub menu") {
				DefaultBackground = "#FDFDFD",
				BackgroundResourceKey = "ToolBarSubMenuBackground",
			},
			new BrushColorInfo(ColorType.ToolBarMenuCheckFill, "Toolbar menu check fill") {
				DefaultBackground = "#E6F0FA",
				BackgroundResourceKey = "ToolBarMenuCheckFill",
			},
			new BrushColorInfo(ColorType.ToolBarButtonChecked, "Toolbar button checked") {
				DefaultBackground = "#E6F0FA",
				BackgroundResourceKey = "ToolBarButtonChecked",
			},
			new LinearGradientColorInfo(ColorType.ToolBarOpenHeaderBackground, new Point(0, 1), "Toolbar open header. Color of top level menu item text when the sub menu is open.", 0, 1) {
				ResourceKey = "ToolBarOpenHeaderBackground",
				DefaultForeground = "#FFFFFF",
				DefaultBackground = "#FFFBFF",
			},
			new BrushColorInfo(ColorType.ToolBarIconVerticalBackground, "ToolBar icon vertical background. Makes sure icons look good with this background color.") {
				DefaultBackground = "#FFFBFF",
			},
			new LinearGradientColorInfo(ColorType.ToolBarVerticalBackground, new Point(1, 0), "Toolbar vertical header. Color of left vertical part of menu items.", 0, 0.5, 1) {
				ResourceKey = "ToolBarVerticalBackground",
				DefaultForeground = "#FFFFFF",
				DefaultBackground = "#FFFBFF",
				DefaultColor3 = "#F7F7F7",
			},
			new BrushColorInfo(ColorType.ToolBarIconBackground, "ToolBar icon background. Makes sure icons look good with this background color.") {
				DefaultBackground = "#FFFBFF",
			},
			new LinearGradientColorInfo(ColorType.ToolBarHorizontalBackground, new Point(0, 1), "Toolbar horizontal background", 0, 0.5, 1) {
				ResourceKey = "ToolBarHorizontalBackground",
				DefaultForeground = "#FFFFFF",
				DefaultBackground = "#FFFBFF",
				DefaultColor3 = "#F7F7F7",
			},
			new BrushColorInfo(ColorType.ToolBarDisabledFill, "Toolbar disabled fill (combobox & textbox)") {
				DefaultBackground = "#F7F7F7",
				BackgroundResourceKey = "ToolBarDisabledFill",
			},
			new BrushColorInfo(ColorType.ToolBarDisabledBorder, "Toolbar disabled border (combobox & textbox)") {
				DefaultBackground = "#B5B2B5",
				BackgroundResourceKey = "ToolBarDisabledBorder",
			},
			new BrushColorInfo(ColorType.ToolBarComboBoxToggleButtonBorder, "Toolbar combobox toggle button border") {
				DefaultBackground = "White",
				BackgroundResourceKey = "ToolBarComboBoxToggleButtonBorder",
			},
			new BrushColorInfo(ColorType.ToolBarComboBoxTransparentButtonFill, "Toolbar Combobox transparent button fill") {
				DefaultBackground = "Black",
				BackgroundResourceKey = "ToolBarComboBoxTransparentButtonFill",
			},
			new BrushColorInfo(ColorType.CheckBoxFillNormal, "Checkbox fill normal") {
				DefaultBackground = "#F4F4F4",
				BackgroundResourceKey = "CheckBoxFillNormal",
			},
			new BrushColorInfo(ColorType.CheckBoxStroke, "Checkbox stroke") {
				DefaultBackground = "#8E8F8F",
				BackgroundResourceKey = "CheckBoxStroke",
			},
			new BrushColorInfo(ColorType.RadioButtonBackground, "RadioButton background") {
				DefaultBackground = "#F4F4F4",
				BackgroundResourceKey = "RadioButtonBackground",
			},
			new BrushColorInfo(ColorType.ButtonIconBackground, "Button icon background. Makes sure icons look good with this background color.") {
				DefaultBackground = "#EEEEEE",
			},
			new LinearGradientColorInfo(ColorType.ButtonNormalBackground, new Point(0, 1), "Button normal background", 0, 0.5, 0.5, 1) {
				ResourceKey = "ButtonNormalBackground",
				DefaultForeground = "#F3F3F3",
				DefaultBackground = "#EBEBEB",
				DefaultColor3 = "#DDDDDD",
				DefaultColor4 = "#CDCDCD",
			},
			new BrushColorInfo(ColorType.ButtonNormalBorder, "Button normal border") {
				DefaultBackground = "#FF707070",
				BackgroundResourceKey = "ButtonNormalBorder",
			},
			new BrushColorInfo(ColorType.ButtonBaseDisabledForeground, "ButtonBase disabled foreground") {
				DefaultForeground = "#ADADAD",
				ForegroundResourceKey = "ButtonBaseDisabledForeground",
			},
			new LinearGradientColorInfo(ColorType.ButtonBaseCommonPressedBackgroundOverlay, new Point(0, 1), "Button common pressed background overlay", 0.5, 0.5, 1) {
				ResourceKey = "ButtonBaseCommonPressedBackgroundOverlay",
				DefaultForeground = "#FFC2E4F6",
				DefaultBackground = "#FFABDAF3",
				DefaultColor3 = "#FF90CBEB",
			},
			new LinearGradientColorInfo(ColorType.ButtonBaseCommonHoverBackgroundOverlay, new Point(0, 1), "Button common hover background overlay", 0, 0.5, 0.5, 1) {
				ResourceKey = "ButtonBaseCommonHoverBackgroundOverlay",
				DefaultForeground = "#FFEAF6FD",
				DefaultBackground = "#FFD9F0FC",
				DefaultColor3 = "#FFBEE6FD",
				DefaultColor4 = "#FFA7D9F5",
			},
			new LinearGradientColorInfo(ColorType.ButtonBaseCommonInnerBorder, new Point(0, 1), "Button common inner border", 0, 1) {
				ResourceKey = "ButtonBaseCommonInnerBorder",
				DefaultForeground = "#FAFFFFFF",
				DefaultBackground = "#85FFFFFF",
			},
			new BrushColorInfo(ColorType.ButtonBaseBorderOverlayBorder, "Button border overlay border") {
				DefaultBackground = "SystemColors.Control",
				BackgroundResourceKey = "ButtonBaseBorderOverlayBorder",
			},
			new BrushColorInfo(ColorType.ButtonBaseDisabledBorderBrush, "Button disabled border brush") {
				DefaultBackground = "#ADB2B5",
				BackgroundResourceKey = "ButtonBaseDisabledBorderBrush",
			},
			new BrushColorInfo(ColorType.ButtonBaseDisabledBorderOverlayBackground, "Button disabled border overlay background") {
				DefaultBackground = "#F4F4F4",
				BackgroundResourceKey = "ButtonBaseDisabledBorderOverlayBackground",
			},
			new BrushColorInfo(ColorType.ButtonBaseMouseOverBorderBrush, "Button mouse over border brush") {
				DefaultBackground = "#3C7FB1",
				BackgroundResourceKey = "ButtonBaseMouseOverBorderBrush",
			},
			new BrushColorInfo(ColorType.ButtonBasePressedBorderOverlayBrush, "Button pressed border overlay brush") {
				DefaultBackground = "#2C628B",
				BackgroundResourceKey = "ButtonBasePressedBorderOverlayBrush",
			},
			new BrushColorInfo(ColorType.ButtonBaseDefaultedBorderOverlayBrush, "Button defaulted border overlay brush") {
				DefaultBackground = "#F900CCFF",
				BackgroundResourceKey = "ButtonBaseDefaultedBorderOverlayBrush",
			},
			new BrushColorInfo(ColorType.TabControlNormalBorderBrush, "TabControl normal border brush") {
				DefaultBackground = "#8C8E94",
				BackgroundResourceKey = "TabControlNormalBorderBrush",
			},
			new BrushColorInfo(ColorType.TabControlBackground, "TabControl background") {
				DefaultBackground = "#F9F9F9",
				BackgroundResourceKey = "TabControlBackground",
			},
			new BrushColorInfo(ColorType.TabItemForeground, "TabItem foreground") {
				DefaultBackground = "Black",
				BackgroundResourceKey = "TabItemForeground",
			},
			new LinearGradientColorInfo(ColorType.TabItemHotBackground, new Point(0, 1), "TabItem hot background", 0.15, 0.5, 0.5, 1) {
				ResourceKey = "TabItemHotBackground",
				DefaultForeground = "#EAF6FD",
				DefaultBackground = "#D9F0FC",
				DefaultColor3 = "#BEE6FD",
				DefaultColor4 = "#A7D9F5",
			},
			new BrushColorInfo(ColorType.TabItemSelectedBackground, "TabItem selected background") {
				DefaultBackground = "#F9F9F9",
				BackgroundResourceKey = "TabItemSelectedBackground",
			},
			new BrushColorInfo(ColorType.TabItemDisabledBackground, "TabItem disabled background") {
				DefaultBackground = "#F4F4F4",
				BackgroundResourceKey = "TabItemDisabledBackground",
			},
			new BrushColorInfo(ColorType.TabItemHotBorderBrush, "TabItem hot border brush") {
				DefaultBackground = "#3C7FB1",
				BackgroundResourceKey = "TabItemHotBorderBrush",
			},
			new BrushColorInfo(ColorType.TabItemDisabledBorderBrush, "TabItem disabled border brush") {
				DefaultBackground = "#FFC9C7BA",
				BackgroundResourceKey = "TabItemDisabledBorderBrush",
			},
			new BrushColorInfo(ColorType.ContextMenuBackground, "Context menu background") {
				DefaultBackground = "#F5F5F5",
				BackgroundResourceKey = "ContextMenuBackground",
			},
			new BrushColorInfo(ColorType.ContextMenuBorderBrush, "Context menu border brush") {
				DefaultBackground = "#FF959595",
				BackgroundResourceKey = "ContextMenuBorderBrush",
			},
			new BrushColorInfo(ColorType.ContextMenuRectangleFill1, "Context menu rectangle fill #1. It's the vertical rectangle on the left side.") {
				DefaultBackground = "#F1F1F1",
				BackgroundResourceKey = "ContextMenuRectangleFill1",
			},
			new BrushColorInfo(ColorType.ContextMenuRectangleFill2, "Context menu rectangle fill #2. It's the small vertical rectangle to the right of the left most vertical rectangle.") {
				DefaultBackground = "#E2E3E3",
				BackgroundResourceKey = "ContextMenuRectangleFill2",
			},
			new BrushColorInfo(ColorType.ContextMenuRectangleFill3, "Context menu rectangle fill #3") {
				DefaultBackground = "White",
				BackgroundResourceKey = "ContextMenuRectangleFill3",
			},
			new BrushColorInfo(ColorType.ExpanderStaticCircleStroke, "Expander Static Circle Stroke") {
				DefaultBackground = "DarkGray",
				BackgroundResourceKey = "Expander.Static.Circle.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderStaticCircleFill, "Expander Static Circle Fill") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "Expander.Static.Circle.Fill",
			},
			new BrushColorInfo(ColorType.ExpanderStaticArrowStroke, "Expander Static Arrow Stroke") {
				DefaultBackground = "#666666",
				BackgroundResourceKey = "Expander.Static.Arrow.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderMouseOverCircleStroke, "Expander MouseOver Circle Stroke") {
				DefaultBackground = "#FF3C7FB1",
				BackgroundResourceKey = "Expander.MouseOver.Circle.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderMouseOverCircleFill, "Expander MouseOver Circle Fill") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "Expander.MouseOver.Circle.Fill",
			},
			new BrushColorInfo(ColorType.ExpanderMouseOverArrowStroke, "Expander MouseOver Arrow Stroke") {
				DefaultBackground = "#222222",
				BackgroundResourceKey = "Expander.MouseOver.Arrow.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderPressedCircleStroke, "Expander Pressed Circle Stroke") {
				DefaultBackground = "#FF526C7B",
				BackgroundResourceKey = "Expander.Pressed.Circle.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderPressedCircleFill, "Expander.Pressed.Circle.Fill") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "Expander.Pressed.Circle.Fill",
			},
			new BrushColorInfo(ColorType.ExpanderPressedArrowStroke, "Expander Pressed Arrow Stroke") {
				DefaultBackground = "#FF003366",
				BackgroundResourceKey = "Expander.Pressed.Arrow.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderDisabledCircleStroke, "Expander Disabled Circle Stroke") {
				DefaultBackground = "DarkGray",
				BackgroundResourceKey = "Expander.Disabled.Circle.Stroke",
			},
			new BrushColorInfo(ColorType.ExpanderDisabledCircleFill, "Expander Disabled Circle Fill") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "Expander.Disabled.Circle.Fill",
			},
			new BrushColorInfo(ColorType.ExpanderDisabledArrowStroke, "Expander Disabled Arrow Stroke") {
				DefaultBackground = "#666666",
				BackgroundResourceKey = "Expander.Disabled.Arrow.Stroke",
			},
			new LinearGradientColorInfo(ColorType.ProgressBarBorderBrush, new Point(0, 1), "ProgressBar border brush", 0, 1) {
				ResourceKey = "ProgressBarBorderBrush",
				DefaultForeground = "#B2B2B2",
				DefaultBackground = "#8C8C8C",
			},
			new BrushColorInfo(ColorType.ProgressBarForeground, "ProgressBar foreground") {
				DefaultForeground = "#01D328",
				ForegroundResourceKey = "ProgressBarForeground",
			},
			new LinearGradientColorInfo(ColorType.ProgressBarBackground, new Point(1, 0), "ProgressBar background", 0, 0.5, 1) {
				ResourceKey = "ProgressBarBackground",
				DefaultForeground = "#BABABA",
				DefaultBackground = "#C7C7C7",
				DefaultColor3 = "#BABABA",
			},
			new LinearGradientColorInfo(ColorType.ProgressBarTopHighlight, new Point(0, 1), "ProgressBar top highlight", 0.05, 0.25) {
				ResourceKey = "ProgressBarTopHighlight",
				DefaultForeground = "#80FFFFFF",
				DefaultBackground = "#00FFFFFF",
			},
			new LinearGradientColorInfo(ColorType.ProgressBarGlassyHighlight, new Point(0, 1), "ProgressBar glassy highlight", 0.5385, 0.5385) {
				ResourceKey = "ProgressBarGlassyHighlight",
				DefaultForeground = "#50FFFFFF",
				DefaultBackground = "#00FFFFFF",
			},
			new LinearGradientColorInfo(ColorType.ProgressBarIndicatorGlassyHighlight, new Point(0, 1), "ProgressBar indicator glassy highlight", 0.5385, 0.5385) {
				ResourceKey = "ProgressBarIndicatorGlassyHighlight",
				DefaultForeground = "#90FFFFFF",
				DefaultBackground = "#00FFFFFF",
			},
			new RadialGradientColorInfo(ColorType.ProgressBarIndicatorLightingEffectLeft, "1,0,0,1,0.5,0.5", "ProgressBar indicator lighting effect left", 0, 1) {
				ResourceKey = "ProgressBarIndicatorLightingEffectLeft",
				DefaultForeground = "#60FFFFC4",
				DefaultBackground = "#00FFFFC4",
			},
			new LinearGradientColorInfo(ColorType.ProgressBarIndicatorLightingEffect, new Point(0, 1), new Point(0, 0), "ProgressBar indicator lighting effect", 0, 1) {
				ResourceKey = "ProgressBarIndicatorLightingEffect",
				DefaultForeground = "#60FFFFC4",
				DefaultBackground = "#00FFFFC4",
			},
			new RadialGradientColorInfo(ColorType.ProgressBarIndicatorLightingEffectRight, "1,0,0,1,-0.5,0.5", "ProgressBar indicator lighting effect right", 0, 1) {
				ResourceKey = "ProgressBarIndicatorLightingEffectRight",
				DefaultForeground = "#60FFFFC4",
				DefaultBackground = "#00FFFFC4",
			},
			new LinearGradientColorInfo(ColorType.ProgressBarIndicatorDarkEdgeLeft, new Point(1, 0), "ProgressBar indicator dark edge left", 0, 0.3, 1) {
				ResourceKey = "ProgressBarIndicatorDarkEdgeLeft",
				DefaultForeground = "#0C000000",
				DefaultBackground = "#20000000",
				DefaultColor3 = "#00000000",
			},
			new LinearGradientColorInfo(ColorType.ProgressBarIndicatorDarkEdgeRight, new Point(1, 0), "ProgressBar indicator dark edge right", 0, 0.7, 1) {
				ResourceKey = "ProgressBarIndicatorDarkEdgeRight",
				DefaultForeground = "#00000000",
				DefaultBackground = "#20000000",
				DefaultColor3 = "#0C000000",
			},
			new LinearGradientColorInfo(ColorType.ProgressBarIndicatorAnimatedFill, new Point(1, 0), "ProgressBar indicator animated fill", 0, 0.4, 0.6, 1) {
				ResourceKey = "ProgressBarIndicatorAnimatedFill",
				DefaultForeground = "#00FFFFFF",
				DefaultBackground = "#60FFFFFF",
				DefaultColor3 = "#60FFFFFF",
				DefaultColor4 = "#00FFFFFF",
			},
			new BrushColorInfo(ColorType.ProgressBarBorderBrush2, "ProgressBar border brush #2") {
				DefaultBackground = "#80FFFFFF",
				BackgroundResourceKey = "ProgressBarBorderBrush2",
			},
			new BrushColorInfo(ColorType.ProgressBarIndeterminateBackground, "ProgressBar indeterminate background") {
				DefaultBackground = "#80B5FFA9",
				BackgroundResourceKey = "ProgressBarIndeterminateBackground",
			},
			new LinearGradientColorInfo(ColorType.ResizeGripperForeground, new Point(0, 0.25), new Point(1, 0.75), "ResizeGripper foreground", 0.3, 0.75, 1) {
				ResourceKey = "ResizeGripperForeground",
				DefaultForeground = "#FFFFFF",
				DefaultBackground = "#BBC5D7",
				DefaultColor3 = "#6D83A9",
			},
			new BrushColorInfo(ColorType.ScrollBarDisabledBackground, "ScrollBar disabled background") {
				DefaultBackground = "#F5F5F5",
				BackgroundResourceKey = "ScrollBarDisabledBackground",
			},
			new BrushColorInfo(ColorType.ScrollBarBackground, "ScrollBar background") {
				DefaultBackground = "#F1F1F1",
				BackgroundResourceKey = "ScrollBarBackground",
			},
			new BrushColorInfo(ColorType.ScrollBarArrowButtonForeground, "ScrollBar arrow button foreground") {
				DefaultBackground = "#505050",
				BackgroundResourceKey = "ScrollBarArrowButtonForeground",
			},
			new BrushColorInfo(ColorType.ScrollBarArrowButtonDisabled, "ScrollBar arrow button disabled") {
				DefaultBackground = "#F5F5F5",
				BackgroundResourceKey = "ScrollBarArrowButtonDisabledBackground",
				DefaultForeground = "#D8D8D8",
				ForegroundResourceKey = "ScrollBarArrowButtonDisabledForeground",
			},
			new BrushColorInfo(ColorType.ScrollBarArrowButtonMouseOverBackground, "ScrollBar arrow button mouse over background") {
				DefaultBackground = "#D2D2D2",
				BackgroundResourceKey = "ScrollBarArrowButtonMouseOverBackground",
			},
			new BrushColorInfo(ColorType.ScrollBarArrowButtonPressed, "ScrollBar arrow button pressed") {
				DefaultBackground = "#787878",
				BackgroundResourceKey = "ScrollBarArrowButtonPressedBackground",
				DefaultForeground = "#FFFFFF",
				ForegroundResourceKey = "ScrollBarArrowButtonPressedForeground",
			},
			new BrushColorInfo(ColorType.ScrollBarThumbBackground, "ScrollBar thumb background") {
				DefaultBackground = "#BCBCBC",
				BackgroundResourceKey = "ScrollBarThumbBackground",
			},
			new BrushColorInfo(ColorType.ScrollBarThumbBorder, "ScrollBar thumb border") {
				DefaultBackground = "#A8A8A8",
				BackgroundResourceKey = "ScrollBarThumbBorder",
			},
			new BrushColorInfo(ColorType.ScrollBarThumbDisabledBackground, "ScrollBar thumb disabled background") {
				DefaultBackground = "#BCBCBC",
				BackgroundResourceKey = "ScrollBarThumbDisabledBackground",
			},
			new BrushColorInfo(ColorType.ScrollBarThumbDisabledBorder, "ScrollBar thumb disabled border") {
				DefaultBackground = "#A8A8A8",
				BackgroundResourceKey = "ScrollBarThumbDisabledBorder",
			},
			new BrushColorInfo(ColorType.ScrollBarThumbMouseOverBackground, "ScrollBar thumb mouse over background") {
				DefaultBackground = "#AAAAAB",
				BackgroundResourceKey = "ScrollBarThumbMouseOverBackground",
			},
			new BrushColorInfo(ColorType.ScrollBarThumbMouseOverBorder, "ScrollBar thumb mouse over border") {
				DefaultBackground = "#9A9A9A",
				BackgroundResourceKey = "ScrollBarThumbMouseOverBorder",
			},
			new BrushColorInfo(ColorType.ScrollBarThumbPressedBackground, "ScrollBar thumb pressed background") {
				DefaultBackground = "#8D8D8E",
				BackgroundResourceKey = "ScrollBarThumbPressedBackground",
			},
			new BrushColorInfo(ColorType.ScrollBarThumbPressedBorder, "ScrollBar thumb pressed border") {
				DefaultBackground = "#787878",
				BackgroundResourceKey = "ScrollBarThumbPressedBorder",
			},
			new BrushColorInfo(ColorType.StatusBar, "StatusBar") {
				DefaultBackground = "#FFF1EDED",
				BackgroundResourceKey = "StatusBarBackground",
				DefaultForeground = "SystemColors.ControlText",
				ForegroundResourceKey = "StatusBarForeground",
			},
			new LinearGradientColorInfo(ColorType.TextBoxBorder, new Point(0, 20), "TextBox border", 0.05, 0.07, 1) {
				ResourceKey = "TextBoxBorder",
				DefaultForeground = "#ABADB3",
				DefaultBackground = "#E2E3EA",
				DefaultColor3 = "#E3E9EF",
				MappingMode = BrushMappingMode.Absolute,
			},
			new LinearGradientColorInfo(ColorType.ToolTipBackground, new Point(0, 1), "ToolTip background", 0, 1) {
				ResourceKey = "ToolTipBackground",
				DefaultForeground = "#FFFFFF",
				DefaultBackground = "#E4E5F0",
			},
			new BrushColorInfo(ColorType.ToolTipBorderBrush, "ToolTip border brush") {
				DefaultBackground = "#767676",
				BackgroundResourceKey = "ToolTipBorderBrush",
			},
			new BrushColorInfo(ColorType.ToolTipForeground, "ToolTip foreground") {
				DefaultForeground = "#575757",
				ForegroundResourceKey = "ToolTipForeground",
			},
			new BrushColorInfo(ColorType.TVEditListBorder, "TreeView Edit list border") {
				DefaultBackground = "#FF7F9DB9",
				BackgroundResourceKey = "TVEditListBorder",
			},
			new BrushColorInfo(ColorType.TVExpanderBorderBrush, "TreeView expander border brush") {
				DefaultBackground = "#FF7898B5",
				BackgroundResourceKey = "TVExpanderBorderBrush",
			},
			new LinearGradientColorInfo(ColorType.TVExpanderBorderBackground, new Point(1, 1), "TreeView expander border background", 0.2, 1) {
				ResourceKey = "TVExpanderBorderBackground",
				DefaultForeground = "White",
				DefaultBackground = "#FFC0B7A6",
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
			new BrushColorInfo(ColorType.GridViewScrollViewerLeftFill, "GridView ScrollViewer left fill") {
				DefaultBackground = "White",
				BackgroundResourceKey = "GridViewScrollViewerLeftFill",
			},
			new BrushColorInfo(ColorType.GridViewScrollViewerTopFill, "GridView ScrollViewer top fill") {
				DefaultBackground = "White",
				BackgroundResourceKey = "GridViewScrollViewerTopFill",
			},
			new LinearGradientColorInfo(ColorType.GridViewColumnHeaderBorderBackground, new Point(0, 1), "GridViewColumnHeader border background", 0, 1) {
				ResourceKey = "GridViewColumnHeaderBorderBackground",
				DefaultForeground = "#FFF2F2F2",
				DefaultBackground = "#FFD5D5D5",
			},
			new LinearGradientColorInfo(ColorType.GridViewColumnHeaderBackground, new Point(0, 1), "GridViewColumnHeader background", 0, 0.4091, 1) {
				ResourceKey = "GridViewColumnHeaderBackground",
				DefaultForeground = "#FFFFFFFF",
				DefaultBackground = "#FFFFFFFF",
				DefaultColor3 = "#FFF7F8F9",
			},
			new LinearGradientColorInfo(ColorType.GridViewColumnHeaderHoverBackground, new Point(0, 1), "GridViewColumnHeader hover background", 0, 1) {
				ResourceKey = "GridViewColumnHeaderHoverBackground",
				DefaultForeground = "#FFBDEDFF",
				DefaultBackground = "#FFB7E7FB",
			},
			new LinearGradientColorInfo(ColorType.GridViewColumnHeaderPressBackground, new Point(0, 1), "GridViewColumnHeader press background", 0, 1) {
				ResourceKey = "GridViewColumnHeaderPressBackground",
				DefaultForeground = "#FF8DD6F7",
				DefaultBackground = "#FF8AD1F5",
			},
			new BrushColorInfo(ColorType.GridViewColumnHeaderUpperHighlightFill, "GridViewColumnHeader upper highlight fill") {
				DefaultBackground = "#FFE3F7FF",
				BackgroundResourceKey = "GridViewColumnHeaderUpperHighlightFill",
			},
			new BrushColorInfo(ColorType.GridViewColumnHeaderMouseOverHeaderHoverBorder, "GridViewColumnHeader mouse over header hover border") {
				DefaultBackground = "#FF88CBEB",
				BackgroundResourceKey = "GridViewColumnHeaderMouseOverHeaderHoverBorder",
			},
			new BrushColorInfo(ColorType.GridViewColumnHeaderPressHoverBorder, "GridViewColumnHeader press hover border") {
				DefaultBackground = "#FF95DAF9",
				BackgroundResourceKey = "GridViewColumnHeaderPressHoverBorder",
			},
			new BrushColorInfo(ColorType.GridViewColumnHeaderPressBorder, "GridViewColumnHeader press border") {
				DefaultBackground = "#FF7A9EB1",
				BackgroundResourceKey = "GridViewColumnHeaderPressBorder",
			},
			new BrushColorInfo(ColorType.GridViewColumnHeaderPressUpperHighlightFill, "GridViewColumnHeader press upper highlight fill") {
				DefaultBackground = "#FFBCE4F9",
				BackgroundResourceKey = "GridViewColumnHeaderPressUpperHighlightFill",
			},
			new BrushColorInfo(ColorType.GridViewColumnHeaderFloatingHeaderCanvasFill, "GridViewColumnHeaderFloatingHeaderCanvasFill") {
				DefaultBackground = "#FF000000",
				BackgroundResourceKey = "GridViewColumnHeaderFloatingHeaderCanvasFill",
			},
			new BrushColorInfo(ColorType.GridViewListViewForeground, "GridView ListView foreground") {
				DefaultBackground = "#FF042271",
				BackgroundResourceKey = "GridViewListViewForeground",
			},
			new BrushColorInfo(ColorType.GridViewListViewItemFocusVisualStroke, "GridView ListViewItem FocusVisual stroke") {
				DefaultBackground = "#8E6EA6F5",
				BackgroundResourceKey = "GridViewListViewItemFocusVisualStroke",
			},
			new LinearGradientColorInfo(ColorType.GridViewListItemHoverFill, new Point(0, 1), "GridView ListItem hover fill", 0, 1) {
				ResourceKey = "ListItemHoverFill",
				DefaultForeground = "#FFF1FBFF",
				DefaultBackground = "#FFD5F1FE",
			},
			new LinearGradientColorInfo(ColorType.GridViewListItemSelectedFill, new Point(0, 1), "GridView ListItem selected fill", 0, 1) {
				ResourceKey = "ListItemSelectedFill",
				DefaultForeground = "#FFD9F4FF",
				DefaultBackground = "#FF9BDDFB",
			},
			new LinearGradientColorInfo(ColorType.GridViewListItemSelectedHoverFill, new Point(0, 1), "GridView ListItem selected hover fill", 0, 1) {
				ResourceKey = "ListItemSelectedHoverFill",
				DefaultForeground = "#FFEAF9FF",
				DefaultBackground = "#FFC9EDFD",
			},
			new LinearGradientColorInfo(ColorType.GridViewListItemSelectedInactiveFill, new Point(0, 1), "GridView ListItem selected inactive fill", 0, 1) {
				ResourceKey = "ListItemSelectedInactiveFill",
				DefaultForeground = "#FFEEEDED",
				DefaultBackground = "#FFDDDDDD",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerUpperHighlightFill, "GridView ItemContainer upper highlight fill") {
				DefaultBackground = "#75FFFFFF",
				BackgroundResourceKey = "GridViewItemContainerUpperHighlightFill",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerMouseOverHoverBorder, "GridView ItemContainer mouse over hover border") {
				DefaultBackground = "#FFCCF0FF",
				BackgroundResourceKey = "GridViewItemContainerMouseOverHoverBorder",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedBorder, "GridView ItemContainer selected border") {
				DefaultBackground = "#FF98DDFB",
				BackgroundResourceKey = "GridViewItemContainerSelectedBorder",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedInnerBorder, "GridView ItemContainer selected inner border") {
				DefaultBackground = "#80FFFFFF",
				BackgroundResourceKey = "GridViewItemContainerSelectedInnerBorder",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedUpperHighlightFill, "GridView ItemContainer selected upper highlight fill") {
				DefaultBackground = "#40FFFFFF",
				BackgroundResourceKey = "GridViewItemContainerSelectedUpperHighlightFill",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedInactiveBorder, "GridView ItemContainer selected inactive border") {
				DefaultBackground = "#FFCFCFCF",
				BackgroundResourceKey = "GridViewItemContainerSelectedInactiveBorder",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedMouseOverBorder, "GridView ItemContainer selected mouse over border brush") {
				DefaultBackground = "#FF98DDFB",
				BackgroundResourceKey = "GridViewItemContainerSelectedMouseOverBorder",
			},
			new BrushColorInfo(ColorType.SortableGridViewColumnArrowBackground, "SortableGridViewColumn arrow background") {
				DefaultBackground = "Gray",
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
				DefaultForeground = "SystemColors.ControlText",
				ForegroundResourceKey = "DockedPaneTitleForeground",
			},
			new BrushColorInfo(ColorType.DecompilerTextViewBorder, "DecompilerTextView border") {
				DefaultBackground = "#FF828790",
				BackgroundResourceKey = "DecompilerTextViewBorder",
			},
			new BrushColorInfo(ColorType.DecompilerTextViewWaitAdornerBackground, "DecompilerTextView wait adorner background") {
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
			new BrushColorInfo(ColorType.TextBoxErrorBorder, "TextBox error border") {
				DefaultBackground = "Red",
				BackgroundResourceKey = "TextBoxErrorBorder",
			},
			new BrushColorInfo(ColorType.TextBoxError, "TextBox error") {
				DefaultBackground = "Pink",
				DefaultForeground = "SystemColors.WindowText",
				BackgroundResourceKey = "TextBoxErrorBackground",
				ForegroundResourceKey = "TextBoxErrorForeground",
			},
			new BrushColorInfo(ColorType.ListArrowBackground, "List arrow background") {
				DefaultBackground = "Black",
				BackgroundResourceKey = "ListArrowBackground",
			},
			new BrushColorInfo(ColorType.TreeViewLineBackground, "TreeView line background") {
				DefaultBackground = "LightGray",
				BackgroundResourceKey = "TreeViewLineBackground",
			},
			new BrushColorInfo(ColorType.IconBar, "IconBar") {
				DefaultBackground = "#E6E7E8",
			},
			new BrushColorInfo(ColorType.IconBarBorder, "IconBar") {
				DefaultBackground = "#CFD0D1",
			},
			new BrushColorInfo(ColorType.NodeAutoLoaded, "TreeView node auto loaded") {
				DefaultForeground = "SteelBlue",
			},
			new BrushColorInfo(ColorType.NodePublic, "TreeView node public") {
				DefaultForeground = "SystemColors.WindowText",
			},
			new BrushColorInfo(ColorType.NodeNotPublic, "TreeView node not public") {
				DefaultForeground = "SystemColors.GrayText",
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
				if (property == null) {
					// HACK: these exist in .NET 4.5+ only but are used by the XAML file.
					if (shortName == "InactiveSelectionHighlight")
						return CreateColor("SystemColors.Highlight");
					if (shortName == "InactiveSelectionHighlightText")
						return CreateColor("SystemColors.HighlightText");
				}
				Debug.Assert(property != null);
				if (property == null)
					return null;
				return new SystemColorHighlightingBrush(property);
			}

			var clr = (System.Windows.Media.Color?)colorConverter.ConvertFromInvariantString(color);
			return clr == null ? null : new SimpleHighlightingBrush(clr.Value);
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
