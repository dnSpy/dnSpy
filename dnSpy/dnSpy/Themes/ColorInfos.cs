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

using System.Windows;
using dnSpy.Contracts.Themes;

namespace dnSpy.Themes {
	static class ColorInfos {
		internal static readonly ColorInfo[] RootColorInfos = new ColorInfo[] {
			new BrushColorInfo(ColorType.SelectedText, "Selected text") {
				DefaultBackground = "#FF3399FF",
			},
			new BrushColorInfo(ColorType.InactiveSelectedText, "Inactive Selected text") {
				DefaultBackground = "#FFBFCDDB",
			},
			new BrushColorInfo(ColorType.HexSelection, "Selected text in hex editor") {
				DefaultBackground = "#663399FF",
			},
			new BrushColorInfo(ColorType.GlyphMargin, "Indicator Margin") {
				BackgroundResourceKey = "GlyphMarginBackground",
				DefaultBackground = "#FFE6E7E8",
			},
			new BrushColorInfo(ColorType.CurrentLine, "Current line") {
				DefaultForeground = "#EAEAF2",
			},
			new BrushColorInfo(ColorType.CurrentLineNoFocus, "Current line (no keyboard focus)") {
				DefaultForeground = "#EEEEEE",
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
			new BrushColorInfo(ColorType.PEHex, "PE Hex") {
				DefaultForeground = "#FF000000",
				ForegroundResourceKey = "PEHexForeground",
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "PEHexBackground"
			},
			new BrushColorInfo(ColorType.PEHexBorder, "PE Hex Border") {
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "PEHexBorder",
			},
			new BrushColorInfo(ColorType.DialogWindow, "Dialog Window") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "DialogWindowForeground",
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "DialogWindowBackground"
			},
			new BrushColorInfo(ColorType.DialogWindowActiveCaption, "Dialog Window Active Caption") {
				DefaultForeground = "#FF525252",
				ForegroundResourceKey = "DialogWindowActiveCaptionText",
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "DialogWindowActiveCaption",
			},
			new BrushColorInfo(ColorType.DialogWindowActiveDebuggingBorder, "Dialog Window Active Debugging Border") {
				DefaultBackground = "#FF9B9FB9",
				BackgroundResourceKey = "DialogWindowActiveDebuggingBorder",
			},
			new BrushColorInfo(ColorType.DialogWindowActiveDefaultBorder, "Dialog Window Active Default Border") {
				DefaultBackground = "#FF9B9FB9",
				BackgroundResourceKey = "DialogWindowActiveDefaultBorder",
			},
			new BrushColorInfo(ColorType.DialogWindowButtonHoverInactive, "Dialog Window Button Hover Inactive") {
				DefaultBackground = "#D8FFFFFF",
				BackgroundResourceKey = "DialogWindowButtonHoverInactive",
			},
			new BrushColorInfo(ColorType.DialogWindowButtonHoverInactiveBorder, "Dialog Window Button Hover Inactive Border") {
				DefaultBackground = "#D8FFFFFF",
				BackgroundResourceKey = "DialogWindowButtonHoverInactiveBorder",
			},
			new BrushColorInfo(ColorType.DialogWindowButtonHoverInactiveGlyph, "Dialog Window Button Hover Inactive Glyph") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "DialogWindowButtonHoverInactiveGlyph",
			},
			new BrushColorInfo(ColorType.DialogWindowButtonInactiveBorder, "Dialog Window Button Inactive Border") {
				DefaultBackground = "#00000000",
				BackgroundResourceKey = "DialogWindowButtonInactiveBorder",
			},
			new BrushColorInfo(ColorType.DialogWindowButtonInactiveGlyph, "Dialog Window Button Inactive Glyph") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "DialogWindowButtonInactiveGlyph",
			},
			new BrushColorInfo(ColorType.DialogWindowInactiveBorder, "Dialog Window Inactive Border") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "DialogWindowInactiveBorder",
			},
			new BrushColorInfo(ColorType.DialogWindowInactiveCaption, "Dialog Window Inactive Caption") {
				DefaultForeground = "#99525252",
				ForegroundResourceKey = "DialogWindowInactiveCaptionText",
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "DialogWindowInactiveCaption",
			},
			new BrushColorInfo(ColorType.EnvironmentBackgroundBrush, "MainWindow background (brush)") {
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "EnvironmentBackgroundBrush",
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
			new BrushColorInfo(ColorType.EnvironmentMainWindowActiveCaption, "MainWindow Active Caption") {
				DefaultForeground = "#FF525252",
				ForegroundResourceKey = "EnvironmentMainWindowActiveCaptionText",
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "EnvironmentMainWindowActiveCaption",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowActiveDebuggingBorder, "MainWindow Active Debugging Border") {
				DefaultBackground = "#FF9B9FB9",
				BackgroundResourceKey = "EnvironmentMainWindowActiveDebuggingBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowActiveDefaultBorder, "MainWindow Active Default Border") {
				DefaultBackground = "#FF9B9FB9",
				BackgroundResourceKey = "EnvironmentMainWindowActiveDefaultBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonActiveBorder, "MainWindow Button Active Border") {
				DefaultBackground = "#00000000",
				BackgroundResourceKey = "EnvironmentMainWindowButtonActiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonActiveGlyph, "MainWindow Button Active Glyph") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "EnvironmentMainWindowButtonActiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonDown, "MainWindow Button Down") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "EnvironmentMainWindowButtonDown",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonDownBorder, "MainWindow Button Down Border") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "EnvironmentMainWindowButtonDownBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonDownGlyph, "MainWindow Button Down Glyph") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentMainWindowButtonDownGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonHoverActive, "MainWindow Button Hover Active") {
				DefaultBackground = "#D8FFFFFF",
				BackgroundResourceKey = "EnvironmentMainWindowButtonHoverActive",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonHoverActiveBorder, "MainWindow Button Hover Active Border") {
				DefaultBackground = "#D8FFFFFF",
				BackgroundResourceKey = "EnvironmentMainWindowButtonHoverActiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonHoverActiveGlyph, "MainWindow Button Hover Active Glyph") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "EnvironmentMainWindowButtonHoverActiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonHoverInactive, "MainWindow Button Hover Inactive") {
				DefaultBackground = "#D8FFFFFF",
				BackgroundResourceKey = "EnvironmentMainWindowButtonHoverInactive",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonHoverInactiveBorder, "MainWindow Button Hover Inactive Border") {
				DefaultBackground = "#D8FFFFFF",
				BackgroundResourceKey = "EnvironmentMainWindowButtonHoverInactiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonHoverInactiveGlyph, "MainWindow Button Hover Inactive Glyph") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "EnvironmentMainWindowButtonHoverInactiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonInactiveBorder, "MainWindow Button Inactive Border") {
				DefaultBackground = "#00000000",
				BackgroundResourceKey = "EnvironmentMainWindowButtonInactiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowButtonInactiveGlyph, "MainWindow Button Inactive Glyph") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "EnvironmentMainWindowButtonInactiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowInactiveBorder, "MainWindow Inactive Border") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "EnvironmentMainWindowInactiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentMainWindowInactiveCaption, "MainWindow Inactive Caption") {
				DefaultForeground = "#99525252",
				ForegroundResourceKey = "EnvironmentMainWindowInactiveCaptionText",
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "EnvironmentMainWindowInactiveCaption",
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
			new BrushColorInfo(ColorType.TopLevelMenuHeaderHoverBorder, "Top Level Menu Header Hover Border") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "TopLevelMenuHeaderHoverBorder",
			},
			new BrushColorInfo(ColorType.TopLevelMenuHeaderHover, "Top Level Menu Header Hover") {
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "TopLevelMenuHeaderHoverBackground",
			},
			new BrushColorInfo(ColorType.MenuItemSeparatorFillTop, "MenuItem Separator fill (top)") {
				DefaultBackground = "#E0E3E6",
				BackgroundResourceKey = "MenuItemSeparatorFillTop",
			},
			new BrushColorInfo(ColorType.MenuItemSeparatorFillBottom, "MenuItem Separator fill (bottom)") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "MenuItemSeparatorFillBottom",
			},
			new BrushColorInfo(ColorType.MenuItemGlyphPanelBorderBrush, "MenuItem glyph panel border brush") {
				DefaultBackground = "#CCCCCC",
				BackgroundResourceKey = "MenuItemGlyphPanelBorderBrush",
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
				DefaultBackground = "#FFA2A4A5",
				BackgroundResourceKey = "MenuItemDisabledGlyphPanelBackground",
			},
			new BrushColorInfo(ColorType.MenuItemDisabledGlyphFill, "MenuItem disabled glyph fill") {
				DefaultBackground = "#848589",
				BackgroundResourceKey = "MenuItemDisabledGlyphFill",
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
			new BrushColorInfo(ColorType.ToolBarButtonChecked, "Toolbar button checked") {
				DefaultForeground = "#FF000000",
				ForegroundResourceKey = "ToolBarButtonCheckedText",
				DefaultBackground = "#FFE6F0FA",
				BackgroundResourceKey = "ToolBarButtonChecked",
			},
			new LinearGradientColorInfo(ColorType.ToolBarOpenHeaderBackground, new Point(0, 1), "Toolbar open header. Color of top level menu item text when the sub menu is open.", 0, 1) {
				ResourceKey = "ToolBarOpenHeaderBackground",
				DefaultForeground = "#F6F6F6",
				DefaultBackground = "#F6F6F6",
			},
			new BrushColorInfo(ColorType.ToolBarIconVerticalBackground, "ToolBar icon vertical background. Makes sure icons look good with this background color.") {
				BackgroundResourceKey = "ToolBarIconVerticalBackground",
				DefaultBackground = "#F6F6F6",
			},
			new LinearGradientColorInfo(ColorType.ToolBarVerticalBackground, new Point(1, 0), "Toolbar vertical header. Color of left vertical part of menu items.", 0, 0.5, 1) {
				ResourceKey = "ToolBarVerticalBackground",
				DefaultForeground = "#F6F6F6",
				DefaultBackground = "#F6F6F6",
				DefaultColor3 = "#F6F6F6",
			},
			new BrushColorInfo(ColorType.ToolBarIconBackground, "ToolBar icon background. Makes sure icons look good with this background color.") {
				BackgroundResourceKey = "ToolBarIconBackground",
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
			new LinearGradientColorInfo(ColorType.EnvironmentCommandBar, new Point(0, 1), "CommandBar", 0, 0.5, 1) {
				ResourceKey = "EnvironmentCommandBar",
				DefaultForeground = "#FFEEEEF2",// Environment.CommandBarGradientBegin
				DefaultBackground = "#FFEEEEF2",// Environment.CommandBarGradientMiddle
				DefaultColor3 = "#FFEEEEF2",// Environment.CommandBarGradientEnd
			},
			new BrushColorInfo(ColorType.EnvironmentCommandBarIcon, "CommandBar (bg for icons)") {
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "EnvironmentCommandBarIcon",
			},
			new BrushColorInfo(ColorType.EnvironmentCommandBarMenuMouseOverSubmenuGlyph, "Submenu opened glyph color") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "EnvironmentCommandBarMenuMouseOverSubmenuGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentCommandBarMenuSeparator, "Grid view item border color") {
				DefaultBackground = "#FFF0F0F0",
				BackgroundResourceKey = "EnvironmentCommandBarMenuSeparator",
			},
			new BrushColorInfo(ColorType.EnvironmentCommandBarCheckBox, "CommandBar CheckBox") {
				DefaultBackground = "#FF717171",
				BackgroundResourceKey = "EnvironmentCommandBarCheckBox",
			},
			new BrushColorInfo(ColorType.EnvironmentCommandBarSelectedIcon, "CommandBar Selected Icon") {
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "EnvironmentCommandBarSelectedIcon",
			},
			new BrushColorInfo(ColorType.EnvironmentCommandBarCheckBoxMouseOver, "CommandBar CheckBox Mouse Over") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "EnvironmentCommandBarCheckBoxMouseOver",
			},
			new BrushColorInfo(ColorType.EnvironmentCommandBarHoverOverSelectedIcon, "CommandBar Hover Over Selected Icon") {
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "EnvironmentCommandBarHoverOverSelectedIcon",
			},
			new BrushColorInfo(ColorType.EnvironmentCommandBarMenuItemMouseOver, "CommandBar MenuItem Mouse Over") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "EnvironmentCommandBarMenuItemMouseOverText",
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "EnvironmentCommandBarMenuItemMouseOver",
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
				DefaultBackground = "#FFACACAC",
				BackgroundResourceKey = "CommonControlsButtonBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsButtonBorderDefault, "CommonControls Button Border Default") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CommonControlsButtonBorderDefault",
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
			new BrushColorInfo(ColorType.CommonControlsButtonDefault, "CommonControls Button Default") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "CommonControlsButtonDefaultText",
				DefaultBackground = "#FFECECF0",
				BackgroundResourceKey = "CommonControlsButtonDefault",
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
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsCheckBoxTextFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxTextHover, "CommonControls CheckBox Text Hover") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsCheckBoxTextHover",
			},
			new BrushColorInfo(ColorType.CommonControlsCheckBoxTextPressed, "CommonControls CheckBox Text Pressed") {
				DefaultBackground = "#FF1E1E1E",
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
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsRadioButtonTextFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonTextHover, "CommonControls RadioButton Text Hover") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsRadioButtonTextHover",
			},
			new BrushColorInfo(ColorType.CommonControlsRadioButtonTextPressed, "CommonControls RadioButton Text Pressed") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CommonControlsRadioButtonTextPressed",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBox, "CommonControls TextBox") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "CommonControlsTextBoxText",
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "CommonControlsTextBoxBackground",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxBorder, "CommonControls TextBox Border") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CommonControlsTextBoxBorder",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxBorderDisabled, "CommonControls TextBox Disabled Border") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CommonControlsTextBoxBorderDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxBorderError, "CommonControls TextBox Error Border") {
				DefaultBackground = "Red",
				BackgroundResourceKey = "CommonControlsTextBoxBorderError",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxBorderFocused, "CommonControls TextBox Focused Border") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CommonControlsTextBoxBorderFocused",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxDisabled, "CommonControls TextBox Disabled") {
				DefaultForeground = "#FFA2A4A5",
				ForegroundResourceKey = "CommonControlsTextBoxTextDisabled",
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "CommonControlsTextBoxBackgroundDisabled",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxError, "CommonControls TextBox Error") {
				DefaultForeground = "#FF000000",
				ForegroundResourceKey = "CommonControlsTextBoxErrorForeground",
				DefaultBackground = "Pink",
				BackgroundResourceKey = "CommonControlsTextBoxErrorBackground",
			},
			new BrushColorInfo(ColorType.CommonControlsTextBoxFocused, "CommonControls TextBox Focused") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "CommonControlsTextBoxTextFocused",
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "CommonControlsTextBoxBackgroundFocused",
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
				DefaultBackground = "#FFE0E0E0",
				BackgroundResourceKey = "ListBoxItem.MouseOver.Background",
			},
			new BrushColorInfo(ColorType.ListBoxItemMouseOverBorder, "ListBoxItem MouseOver Border") {
				DefaultBackground = "#FFD8D8D8",
				BackgroundResourceKey = "ListBoxItem.MouseOver.Border",
			},
			new BrushColorInfo(ColorType.ListBoxItemSelectedInactiveBackground, "ListBoxItem SelectedInactive Background") {
				DefaultBackground = "#FFE0E0E0",
				BackgroundResourceKey = "ListBoxItem.SelectedInactive.Background",
			},
			new BrushColorInfo(ColorType.ListBoxItemSelectedInactiveBorder, "ListBoxItem SelectedInactive Border") {
				DefaultBackground = "#FFD8D8D8",
				BackgroundResourceKey = "ListBoxItem.SelectedInactive.Border",
			},
			new BrushColorInfo(ColorType.ListBoxItemSelectedActiveBackground, "ListBoxItem SelectedActive Background") {
				DefaultBackground = "#FFE0E0E0",
				BackgroundResourceKey = "ListBoxItem.SelectedActive.Background",
			},
			new BrushColorInfo(ColorType.ListBoxItemSelectedActiveBorder, "ListBoxItem SelectedActive Border") {
				DefaultBackground = "#FFD8D8D8",
				BackgroundResourceKey = "ListBoxItem.SelectedActive.Border",
			},
			new BrushColorInfo(ColorType.ContextMenuBackground, "Context menu background") {
				DefaultBackground = "#F6F6F6",
				BackgroundResourceKey = "ContextMenuBackground",
			},
			new BrushColorInfo(ColorType.ContextMenuBorderBrush, "Context menu border brush") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "ContextMenuBorderBrush",
			},
			new BrushColorInfo(ColorType.ContextMenuRectangleFill, "Context menu rectangle fill. It's the vertical rectangle on the left side.") {
				DefaultBackground = "#F6F6F6",
				BackgroundResourceKey = "ContextMenuRectangleFill",
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
				DefaultBackground = "#FFC2C3C9",
				BackgroundResourceKey = "EnvironmentScrollBarThumbBackground",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarThumbDisabled, "ScrollBar thumb disabled") {
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "EnvironmentScrollBarThumbDisabled",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarThumbMouseOverBackground, "ScrollBar thumb mouse over background") {
				DefaultBackground = "#FF686868",
				BackgroundResourceKey = "EnvironmentScrollBarThumbMouseOverBackground",
			},
			new BrushColorInfo(ColorType.EnvironmentScrollBarThumbPressedBackground, "ScrollBar thumb pressed background") {
				DefaultBackground = "#FF5B5B5B",
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
			new BrushColorInfo(ColorType.ScreenTip, "Glyph Margin ToolTip") {
				DefaultForeground = "#FF1E1E1E",// Environment.ScreenTipText
				ForegroundResourceKey = "ScreenTipText",
				DefaultBackground = "#FFFDFBAC",// Environment.ScreenTipBackground
				BackgroundResourceKey = "ScreenTipBackground",
			},
			new BrushColorInfo(ColorType.ScreenTipBorder, "Glyph Margin ToolTip border") {
				DefaultBackground = "#FFFDFBAC",// Environment.ScreenTipBorder
				BackgroundResourceKey = "ScreenTipBorder",
			},
			new BrushColorInfo(ColorType.CompletionToolTip, "Completion ToolTip") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "CompletionToolTipText",
				DefaultBackground = "#FFF6F6F6",
				BackgroundResourceKey = "CompletionToolTipBackground",
			},
			new BrushColorInfo(ColorType.CompletionToolTipBorder, "Completion ToolTip border") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CompletionToolTipBorder",
			},
			new BrushColorInfo(ColorType.QuickInfo, "QuickInfo") {
				DefaultForeground = "#FF1E1E1E",// Environment.ToolTip (fg)
				ForegroundResourceKey = "QuickInfoForeground",
				DefaultBackground = "#FFF6F6F6",// Environment.ToolTip (bg)
				BackgroundResourceKey = "QuickInfoBackground",
			},
			new BrushColorInfo(ColorType.QuickInfoBorder, "QuickInfo border") {
				DefaultBackground = "#FFCCCEDB",// Environment.ToolTipBorder
				BackgroundResourceKey = "QuickInfoBorder",
			},
			new BrushColorInfo(ColorType.SignatureHelp, "SignatureHelp") {
				DefaultForeground = "#FF1E1E1E",// Environment.ToolTip (fg)
				ForegroundResourceKey = "SignatureHelpForeground",
				DefaultBackground = "#FFF6F6F6",// Environment.ToolTip (bg)
				BackgroundResourceKey = "SignatureHelpBackground",
			},
			new BrushColorInfo(ColorType.SignatureHelpBorder, "SignatureHelp border") {
				DefaultBackground = "#FFCCCEDB",// Environment.ToolTipBorder
				BackgroundResourceKey = "SignatureHelpBorder",
			},
			new BrushColorInfo(ColorType.CilButton, "CIL Button") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "CilButtonText",
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "CilButton",
			},
			new BrushColorInfo(ColorType.CilButtonBorder, "CIL Button Border") {
				DefaultBackground = "Transparent",
				BackgroundResourceKey = "CilButtonBorder",
			},
			new BrushColorInfo(ColorType.CilButtonBorderFocused, "CIL Button Border Focused") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CilButtonBorderFocused",
			},
			new BrushColorInfo(ColorType.CilButtonBorderHover, "CIL Button Border Hover") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CilButtonBorderHover",
			},
			new BrushColorInfo(ColorType.CilButtonBorderPressed, "CIL Button Border Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CilButtonBorderPressed",
			},
			new BrushColorInfo(ColorType.CilButtonError, "CIL Button Error") {
				DefaultBackground = "Pink",
				BackgroundResourceKey = "CilButtonErrorBackground",
			},
			new BrushColorInfo(ColorType.CilButtonErrorBorder, "CIL Button Error Border") {
				DefaultBackground = "Red",
				BackgroundResourceKey = "CilButtonErrorBorder",
			},
			new BrushColorInfo(ColorType.CilButtonFocused, "CIL Button Focused") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "CilButtonFocusedText",
				DefaultBackground = "#FFE0E0E0",
				BackgroundResourceKey = "CilButtonFocused",
			},
			new BrushColorInfo(ColorType.CilButtonHover, "CIL Button Hover") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "CilButtonHoverText",
				DefaultBackground = "#FFE0E0E0",
				BackgroundResourceKey = "CilButtonHover",
			},
			new BrushColorInfo(ColorType.CilButtonPressed, "CIL Button Pressed") {
				DefaultForeground = "#FFFFFFFF",
				ForegroundResourceKey = "CilButtonPressedText",
				DefaultBackground = "#FFC0C0C0",
				BackgroundResourceKey = "CilButtonPressed",
			},
			new BrushColorInfo(ColorType.CilCheckBoxBackground, "CIL CheckBox Background") {
				DefaultBackground = "#FFFEFEFE",
				BackgroundResourceKey = "CilCheckBoxBackground",
			},
			new BrushColorInfo(ColorType.CilCheckBoxBackgroundDisabled, "CIL CheckBox Background Disabled") {
				DefaultBackground = "#FFF6F6F6",
				BackgroundResourceKey = "CilCheckBoxBackgroundDisabled",
			},
			new BrushColorInfo(ColorType.CilCheckBoxBackgroundFocused, "CIL CheckBox Background Focused") {
				DefaultBackground = "#FFF3F9FF",
				BackgroundResourceKey = "CilCheckBoxBackgroundFocused",
			},
			new BrushColorInfo(ColorType.CilCheckBoxBackgroundHover, "CIL CheckBox Background Hover") {
				DefaultBackground = "#FFF3F9FF",
				BackgroundResourceKey = "CilCheckBoxBackgroundHover",
			},
			new BrushColorInfo(ColorType.CilCheckBoxBackgroundPressed, "CIL CheckBox Background Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CilCheckBoxBackgroundPressed",
			},
			new BrushColorInfo(ColorType.CilCheckBoxBorder, "CIL CheckBox Border") {
				DefaultBackground = "#FF717171",
				BackgroundResourceKey = "CilCheckBoxBorder",
			},
			new BrushColorInfo(ColorType.CilCheckBoxBorderDisabled, "CIL CheckBox Border Disabled") {
				DefaultBackground = "#FFC6C6C6",
				BackgroundResourceKey = "CilCheckBoxBorderDisabled",
			},
			new BrushColorInfo(ColorType.CilCheckBoxBorderFocused, "CIL CheckBox Border Focused") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CilCheckBoxBorderFocused",
			},
			new BrushColorInfo(ColorType.CilCheckBoxBorderHover, "CIL CheckBox Border Hover") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CilCheckBoxBorderHover",
			},
			new BrushColorInfo(ColorType.CilCheckBoxBorderPressed, "CIL CheckBox Border Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CilCheckBoxBorderPressed",
			},
			new BrushColorInfo(ColorType.CilCheckBoxGlyph, "CIL CheckBox Glyph") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CilCheckBoxGlyph",
			},
			new BrushColorInfo(ColorType.CilCheckBoxGlyphDisabled, "CIL CheckBox Glyph Disabled") {
				DefaultBackground = "#FFA2A4A5",
				BackgroundResourceKey = "CilCheckBoxGlyphDisabled",
			},
			new BrushColorInfo(ColorType.CilCheckBoxGlyphFocused, "CIL CheckBox Glyph Focused") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CilCheckBoxGlyphFocused",
			},
			new BrushColorInfo(ColorType.CilCheckBoxGlyphHover, "CIL CheckBox Glyph Hover") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CilCheckBoxGlyphHover",
			},
			new BrushColorInfo(ColorType.CilCheckBoxGlyphPressed, "CIL CheckBox Glyph Pressed") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "CilCheckBoxGlyphPressed",
			},
			new BrushColorInfo(ColorType.CilCheckBoxText, "CIL CheckBox Text") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "CilCheckBoxText",
			},
			new BrushColorInfo(ColorType.CilCheckBoxTextDisabled, "CIL CheckBox Text Disabled") {
				DefaultBackground = "#FFA2A4A5",
				BackgroundResourceKey = "CilCheckBoxTextDisabled",
			},
			new BrushColorInfo(ColorType.CilCheckBoxTextFocused, "CIL CheckBox Text Focused") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CilCheckBoxTextFocused",
			},
			new BrushColorInfo(ColorType.CilCheckBoxTextHover, "CIL CheckBox Text Hover") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CilCheckBoxTextHover",
			},
			new BrushColorInfo(ColorType.CilCheckBoxTextPressed, "CIL CheckBox Text Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CilCheckBoxTextPressed",
			},
			new BrushColorInfo(ColorType.CilComboBoxBorderFocused, "CIL ComboBox Border Focused") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CilComboBoxBorderFocused",
			},
			new BrushColorInfo(ColorType.CilComboBoxBorderHover, "CIL ComboBox Border Hover") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CilComboBoxBorderHover",
			},
			new BrushColorInfo(ColorType.CilComboBoxBorderPressed, "CIL ComboBox Border Pressed") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "CilComboBoxBorderPressed",
			},
			new BrushColorInfo(ColorType.CilComboBoxError, "CIL ComboBox Error") {
				DefaultBackground = "Pink",
				BackgroundResourceKey = "CilComboBoxErrorBackground",
			},
			new BrushColorInfo(ColorType.CilComboBoxErrorBorder, "CIL ComboBox Error Border") {
				DefaultBackground = "Red",
				BackgroundResourceKey = "CilComboBoxErrorBorder",
			},
			new BrushColorInfo(ColorType.CilComboBoxListBackground, "CIL ComboBox List Background") {
				DefaultBackground = "White",
				BackgroundResourceKey = "CilComboBoxListBackground",
			},
			new BrushColorInfo(ColorType.CilComboBoxListBorder, "CIL ComboBox ListBorder") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CilComboBoxListBorder",
			},
			new BrushColorInfo(ColorType.CilComboBoxListItemBackgroundHover, "CIL ComboBox ListItem Background Hover") {
				DefaultBackground = "#FFF8F8F8",
				BackgroundResourceKey = "CilComboBoxListItemBackgroundHover",
			},
			new BrushColorInfo(ColorType.CilComboBoxListItemBorderHover, "CIL ComboBox ListItem Border Hover") {
				DefaultBackground = "#FFF0F0F0",
				BackgroundResourceKey = "CilComboBoxListItemBorderHover",
			},
			new BrushColorInfo(ColorType.CilComboBoxListItemTextHover, "CIL ComboBox ListItem Text Hover") {
				DefaultBackground = "#FF000000",
				BackgroundResourceKey = "CilComboBoxListItemTextHover",
			},
			new BrushColorInfo(ColorType.CilGridViewBorder, "CIL GridView border") {
				DefaultBackground = "#CCCEDB",
				BackgroundResourceKey = "CilGridViewBorder",
			},
			new BrushColorInfo(ColorType.CilGridViewItemContainerMouseOverHoverBorder, "CIL GridView ItemContainer mouse over hover border") {
				DefaultBackground = "#FFF0F0F0",
				BackgroundResourceKey = "CilGridViewItemContainerMouseOverHoverBorder",
			},
			new BrushColorInfo(ColorType.CilGridViewItemContainerSelectedBorder, "CIL GridView ItemContainer selected border") {
				DefaultBackground = "#FFF0F0F0",
				BackgroundResourceKey = "CilGridViewItemContainerSelectedBorder",
			},
			new BrushColorInfo(ColorType.CilGridViewItemContainerSelectedInactiveBorder, "CIL GridView ItemContainer selected inactive border") {
				DefaultBackground = "#FFF0F0F0",
				BackgroundResourceKey = "CilGridViewItemContainerSelectedInactiveBorder",
			},
			new BrushColorInfo(ColorType.CilGridViewItemContainerSelectedMouseOverBorder, "CIL GridView ItemContainer selected mouse over border brush") {
				DefaultBackground = "#FFE0E0E0",
				BackgroundResourceKey = "CilGridViewItemContainerSelectedMouseOverBorder",
			},
			new BrushColorInfo(ColorType.CilGridViewListItemHoverFill, "CIL GridView ListItem hover fill") {
				DefaultBackground = "#FFF8F8F8",
				BackgroundResourceKey = "CilGridViewListItemHoverFill",
			},
			new BrushColorInfo(ColorType.CilGridViewListItemSelectedFill, "CIL GridView ListItem selected fill") {
				DefaultBackground = "#FFF8F8F8",
				BackgroundResourceKey = "CilGridViewListItemSelectedFill",
			},
			new BrushColorInfo(ColorType.CilGridViewListItemSelectedHoverFill, "CIL GridView ListItem selected hover fill") {
				DefaultBackground = "#FFE8E8E8",
				BackgroundResourceKey = "CilGridViewListItemSelectedHoverFill",
			},
			new BrushColorInfo(ColorType.CilGridViewListItemSelectedInactiveFill, "CIL GridView ListItem selected inactive fill") {
				DefaultBackground = "#FFF8F8F8",
				BackgroundResourceKey = "CilGridViewListItemSelectedInactiveFill",
			},
			new BrushColorInfo(ColorType.CilGridViewListViewItemFocusVisualStroke, "CIL GridView ListViewItem FocusVisual stroke") {
				DefaultBackground = "#FFD0D0D0",
				BackgroundResourceKey = "CilGridViewListViewItemFocusVisualStroke",
			},
			new BrushColorInfo(ColorType.CilListBoxBorder, "CIL ListBox Border") {
				DefaultBackground = "#CCCEDB",
				BackgroundResourceKey = "CilListBoxBorder",
			},
			new BrushColorInfo(ColorType.CilListBoxItemMouseOverBackground, "CIL ListBoxItem MouseOver Background") {
				DefaultBackground = "#FFF8F8F8",
				BackgroundResourceKey = "CilListBoxItem.MouseOver.Background",
			},
			new BrushColorInfo(ColorType.CilListBoxItemMouseOverBorder, "CIL ListBoxItem MouseOver Border") {
				DefaultBackground = "#FFF0F0F0",
				BackgroundResourceKey = "CilListBoxItem.MouseOver.Border",
			},
			new BrushColorInfo(ColorType.CilListBoxItemSelectedActiveBackground, "CIL ListBoxItem SelectedActive Background") {
				DefaultBackground = "#FFF8F8F8",
				BackgroundResourceKey = "CilListBoxItem.SelectedActive.Background",
			},
			new BrushColorInfo(ColorType.CilListBoxItemSelectedActiveBorder, "CIL ListBoxItem SelectedActive Border") {
				DefaultBackground = "#FFF0F0F0",
				BackgroundResourceKey = "CilListBoxItem.SelectedActive.Border",
			},
			new BrushColorInfo(ColorType.CilListBoxItemSelectedInactiveBackground, "CIL ListBoxItem SelectedInactive Background") {
				DefaultBackground = "#FFF8F8F8",
				BackgroundResourceKey = "CilListBoxItem.SelectedInactive.Background",
			},
			new BrushColorInfo(ColorType.CilListBoxItemSelectedInactiveBorder, "CIL ListBoxItem SelectedInactive Border") {
				DefaultBackground = "#FFF0F0F0",
				BackgroundResourceKey = "CilListBoxItem.SelectedInactive.Border",
			},
			new BrushColorInfo(ColorType.CilListViewItem0, "CIL ListViewItem 0") {
				DefaultBackground = "White",
				BackgroundResourceKey = "CilListViewItem0",
			},
			new BrushColorInfo(ColorType.CilListViewItem1, "CIL ListViewItem 1") {
				DefaultBackground = "White",
				BackgroundResourceKey = "CilListViewItem1",
			},
			new BrushColorInfo(ColorType.CilTextBoxDisabled, "CIL TextBox Disabled") {
				DefaultForeground = "#FFA2A4A5",
				ForegroundResourceKey = "CilTextBoxDisabledForeground",
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "CilTextBoxDisabledBackground",
			},
			new BrushColorInfo(ColorType.CilTextBoxDisabledBorder, "CIL TextBox Disabled Border") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "CilTextBoxDisabledBorder",
			},
			new BrushColorInfo(ColorType.CilTextBoxError, "CIL TextBox Error") {
				DefaultForeground = "#FF000000",
				ForegroundResourceKey = "CilTextBoxErrorForeground",
				DefaultBackground = "Pink",
				BackgroundResourceKey = "CilTextBoxErrorBackground",
			},
			new BrushColorInfo(ColorType.CilTextBoxErrorBorder, "CIL TextBox Error Border") {
				DefaultBackground = "Red",
				BackgroundResourceKey = "CilTextBoxErrorBorder",
			},
			new BrushColorInfo(ColorType.CilTextBoxFocusedBorder, "CIL TextBox Focused Border") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CilTextBoxFocusedBorder",
			},
			new BrushColorInfo(ColorType.CilTextBoxMouseOverBorder, "CIL TextBox Mouse Over Border") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CilTextBoxMouseOverBorder",
			},
			new BrushColorInfo(ColorType.CilTextBoxSelection, "CIL TextBox Selection") {
				DefaultBackground = "#FF3399FF",
				BackgroundResourceKey = "CilTextBoxSelection",
			},
			new BrushColorInfo(ColorType.GridViewBackground, "GridView background") {
				DefaultBackground = "#F5F5F5",
				BackgroundResourceKey = "GridViewBackground",
			},
			new BrushColorInfo(ColorType.GridViewBorder, "GridView border") {
				DefaultBackground = "#CCCEDB",
				BackgroundResourceKey = "GridViewBorder",
			},
			new BrushColorInfo(ColorType.HeaderDefault, "Grid Header Default") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "HeaderDefaultText",
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "HeaderDefault",
			},
			new BrushColorInfo(ColorType.HeaderGlyph, "Grid Header Glyph") {
				DefaultBackground = "#FF717171",
				BackgroundResourceKey = "HeaderGlyph",
			},
			new BrushColorInfo(ColorType.HeaderMouseDown, "Grid Header Mouse Down") {
				DefaultForeground = "#FFFFFFFF",
				ForegroundResourceKey = "HeaderMouseDownText",
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "HeaderMouseDown",
			},
			new BrushColorInfo(ColorType.HeaderMouseOver, "Grid Header Mouse Over") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "HeaderMouseOverText",
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "HeaderMouseOver",
			},
			new BrushColorInfo(ColorType.HeaderMouseOverGlyph, "Grid Header Mouse Over Glyph") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "HeaderMouseOverGlyph",
			},
			new BrushColorInfo(ColorType.HeaderSeparatorLine, "Grid Header Separator Line") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "HeaderSeparatorLine",
			},
			new BrushColorInfo(ColorType.GridViewListViewForeground, "GridView ListView foreground") {
				DefaultBackground = "#1E1E1E",
				BackgroundResourceKey = "GridViewListViewForeground",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerMouseOverHoverBorder, "GridView ItemContainer mouse over hover border") {
				DefaultBackground = "#FFD8D8D8",
				BackgroundResourceKey = "GridViewItemContainerMouseOverHoverBorder",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedBorder, "GridView ItemContainer selected border") {
				DefaultBackground = "#FFD8D8D8",
				BackgroundResourceKey = "GridViewItemContainerSelectedBorder",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedInactiveBorder, "GridView ItemContainer selected inactive border") {
				DefaultBackground = "#FFD8D8D8",
				BackgroundResourceKey = "GridViewItemContainerSelectedInactiveBorder",
			},
			new BrushColorInfo(ColorType.GridViewItemContainerSelectedMouseOverBorder, "GridView ItemContainer selected mouse over border brush") {
				DefaultBackground = "#FFD8D8D8",
				BackgroundResourceKey = "GridViewItemContainerSelectedMouseOverBorder",
			},
			new BrushColorInfo(ColorType.GridViewListItemHoverFill, "GridView ListItem hover fill") {
				DefaultBackground = "#FFE0E0E0",
				BackgroundResourceKey = "GridViewListItemHoverFill",
			},
			new BrushColorInfo(ColorType.GridViewListItemSelectedFill, "GridView ListItem selected fill") {
				DefaultBackground = "#FFE0E0E0",
				BackgroundResourceKey = "GridViewListItemSelectedFill",
			},
			new BrushColorInfo(ColorType.GridViewListItemSelectedHoverFill, "GridView ListItem selected hover fill") {
				DefaultBackground = "#FFE0E0E0",
				BackgroundResourceKey = "GridViewListItemSelectedHoverFill",
			},
			new BrushColorInfo(ColorType.GridViewListItemSelectedInactiveFill, "GridView ListItem selected inactive fill") {
				DefaultBackground = "#FFE0E0E0",
				BackgroundResourceKey = "GridViewListItemSelectedInactiveFill",
			},
			new BrushColorInfo(ColorType.GridViewListViewItemFocusVisualStroke, "GridView ListViewItem FocusVisual stroke") {
				DefaultBackground = "#FFD8D8D8",
				BackgroundResourceKey = "GridViewListViewItemFocusVisualStroke",
			},
			new BrushColorInfo(ColorType.DecompilerTextViewWaitAdorner, "DecompilerTextView wait adorner") {
				DefaultForeground = "Black",
				ForegroundResourceKey = "DecompilerTextViewWaitAdornerForeground",
				DefaultBackground = "#C0FFFFFF",
				BackgroundResourceKey = "DecompilerTextViewWaitAdornerBackground",
			},
			new BrushColorInfo(ColorType.ListArrowBackground, "List arrow background") {
				DefaultBackground = "Black",
				BackgroundResourceKey = "ListArrowBackground",
			},
			new BrushColorInfo(ColorType.TreeViewItemMouseOver, "TreeViewItem mouse over") {
				DefaultBackground = "#FFD8D8D8",
				BackgroundResourceKey = "TreeViewItemMouseOverTextBackground",
				DefaultForeground = "Black",
				ForegroundResourceKey = "TreeViewItemMouseOverForeground",
			},
			new BrushColorInfo(ColorType.TreeViewItemSelected, "TreeViewItem Selected") {
				DefaultBackground = "#FFD0D0D0",
				BackgroundResourceKey = "TreeViewItemSelectedBackground",
				DefaultForeground = "Black",
				ForegroundResourceKey = "TreeViewItemSelectedForeground",
			},
			new BrushColorInfo(ColorType.TreeView, "TreeView") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "TreeViewForeground",
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "TreeViewBackground",
			},
			new BrushColorInfo(ColorType.TreeViewBorder, "TreeView border") {
				DefaultBackground = "#CCCEDB",
				BackgroundResourceKey = "TreeViewBorder",
			},
			new BrushColorInfo(ColorType.TreeViewGlyph, "TreeView Glyph") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "TreeViewGlyph",
			},
			new BrushColorInfo(ColorType.TreeViewGlyphMouseOver, "TreeView Glyph Mouse Over") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "TreeViewGlyphMouseOver",
			},
			new BrushColorInfo(ColorType.TVItemAlternationBackground, "TreeViewItem alternation background") {
				DefaultBackground = "WhiteSmoke",
				BackgroundResourceKey = "TVItemAlternationBackground",
			},
			new BrushColorInfo(ColorType.AppSettingsTreeView, "App Settings TreeView") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "AppSettingsTreeViewForeground",
				DefaultBackground = "#FFF8F8F8",
				BackgroundResourceKey = "AppSettingsTreeViewBackground",
			},
			new BrushColorInfo(ColorType.AppSettingsTreeViewBorder, "App Settings TreeView border") {
				DefaultBackground = "#828790",
				BackgroundResourceKey = "AppSettingsTreeViewBorder",
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
			new BrushColorInfo(ColorType.EnvironmentTitleBarActive, "TitleBar Active") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "EnvironmentTitleBarActive",
				DefaultForeground = "#FFFFFFFF",
				ForegroundResourceKey = "EnvironmentTitleBarActiveText",
			},
			new BrushColorInfo(ColorType.EnvironmentTitleBarActiveBorder, "TitleBar Active Border") {
				DefaultBackground = "#FF007ACC",
				BackgroundResourceKey = "EnvironmentTitleBarActiveBorder",
			},
			new LinearGradientColorInfo(ColorType.EnvironmentTitleBarActiveGradient, new Point(0, 1), "TitleBar Active Gradient", 0, 0.5, 0.5, 1) {
				ResourceKey = "EnvironmentTitleBarActiveGradient",
				DefaultForeground = "#FF007ACC",// Environment.TitleBarActiveGradientBegin
				DefaultBackground = "#FF007ACC",// Environment.TitleBarActiveGradientMiddle1
				DefaultColor3 = "#FF007ACC",// Environment.TitleBarActiveGradientMiddle2
				DefaultColor4 = "#FF007ACC",// Environment.TitleBarActiveGradientEnd
			},
			new DrawingBrushColorInfo(ColorType.EnvironmentTitleBarDragHandle, "TitleBar Drag Handle") {
				IsHorizontal = true,
				DefaultBackground = "#FF999999",
				BackgroundResourceKey = "EnvironmentTitleBarDragHandle",
			},
			new DrawingBrushColorInfo(ColorType.EnvironmentTitleBarDragHandleActive, "TitleBar Drag Handle Active") {
				IsHorizontal = true,
				DefaultBackground = "#FF59A8DE",
				BackgroundResourceKey = "EnvironmentTitleBarDragHandleActive",
			},
			new BrushColorInfo(ColorType.EnvironmentTitleBarInactive, "TitleBar Inactive") {
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "EnvironmentTitleBarInactive",
				DefaultForeground = "#FF444444",
				ForegroundResourceKey = "EnvironmentTitleBarInactiveText",
			},
			new BrushColorInfo(ColorType.EnvironmentTitleBarInactiveBorder, "TitleBar Inactive Border") {
				DefaultBackground = "#FFEEEEF2",//Environment.TitleBarInactiveGradientBegin
				BackgroundResourceKey = "EnvironmentTitleBarInactiveBorder",
			},
			new LinearGradientColorInfo(ColorType.EnvironmentTitleBarInactiveGradient, new Point(0, 1), "TitleBar Inactive Gradient", 0, 1) {
				ResourceKey = "EnvironmentTitleBarInactiveGradient",
				DefaultForeground = "#FFEEEEF2",// Environment.TitleBarInactiveGradientBegin
				DefaultBackground = "#FFEEEEF2",// Environment.TitleBarInactiveGradientEnd
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindow, "ToolWindow") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentToolWindow",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowBorder, "ToolWindow Border") {
				DefaultBackground = "#FFCCCEDB",
				BackgroundResourceKey = "EnvironmentToolWindowBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowButtonActiveGlyph, "ToolWindow Button Active Glyph") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentToolWindowButtonActiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowButtonDown, "ToolWindow Button Down") {
				DefaultBackground = "#FF0E6198",
				BackgroundResourceKey = "EnvironmentToolWindowButtonDown",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowButtonDownActiveGlyph, "ToolWindow Button Down Active Glyph") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentToolWindowButtonDownActiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowButtonDownBorder, "ToolWindow Button Down Border") {
				DefaultBackground = "#FF0E6198",
				BackgroundResourceKey = "EnvironmentToolWindowButtonDownBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowButtonHoverActive, "ToolWindow Button Hover Active") {
				DefaultBackground = "#FF52B0EF",
				BackgroundResourceKey = "EnvironmentToolWindowButtonHoverActive",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowButtonHoverActiveBorder, "ToolWindow Button Hover Active Border") {
				DefaultBackground = "#FF52B0EF",
				BackgroundResourceKey = "EnvironmentToolWindowButtonHoverActiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowButtonHoverActiveGlyph, "ToolWindow Button Hover Active Glyph") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentToolWindowButtonHoverActiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowButtonHoverInactive, "ToolWindow Button Hover Inactive") {
				DefaultBackground = "#FFF7F7F9",
				BackgroundResourceKey = "EnvironmentToolWindowButtonHoverInactive",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowButtonHoverInactiveBorder, "ToolWindow Button Hover Inactive Border") {
				DefaultBackground = "#FFF7F7F9",
				BackgroundResourceKey = "EnvironmentToolWindowButtonHoverInactiveBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowButtonHoverInactiveGlyph, "ToolWindow Button Hover Inactive Glyph") {
				DefaultBackground = "#FF717171",
				BackgroundResourceKey = "EnvironmentToolWindowButtonHoverInactiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowButtonInactiveGlyph, "ToolWindow Button Inactive Glyph") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "EnvironmentToolWindowButtonInactiveGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowTabBorder, "Tool Window Tab Border") {
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "EnvironmentToolWindowTabBorder",
			},
			new LinearGradientColorInfo(ColorType.EnvironmentToolWindowTabGradient, new Point(0, 1), "Tool Window Tab Gradient", 0, 1) {
				ResourceKey = "EnvironmentToolWindowTabGradient",
				DefaultForeground = "#FFEEEEF2",// Environment.ToolWindowTabGradientBegin
				DefaultBackground = "#FFEEEEF2",// Environment.ToolWindowTabGradientEnd
			},
			new LinearGradientColorInfo(ColorType.EnvironmentToolWindowTabMouseOverBackgroundGradient, new Point(0, 1), "Tool Window Tab Mouse Over Background Gradient", 0, 1) {
				ResourceKey = "EnvironmentToolWindowTabMouseOverBackgroundGradient",
				DefaultForeground = "#FFC9DEF5",// Environment.ToolWindowTabMouseOverBackgroundBegin
				DefaultBackground = "#FFC9DEF5",// Environment.ToolWindowTabMouseOverBackgroundEnd
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowTabMouseOverBorder, "Tool Window Tab Mouse Over Border") {
				DefaultBackground = "#FFC9DEF5",
				BackgroundResourceKey = "EnvironmentToolWindowTabMouseOverBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowTabMouseOverText, "Tool Window Tab Mouse Over Text") {
				DefaultForeground = "#FF1E1E1E",
				ForegroundResourceKey = "EnvironmentToolWindowTabMouseOverText",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowTabSelectedActiveText, "Tool Window Tab Selected Active Text") {
				DefaultForeground = "#FF0E70C0",
				ForegroundResourceKey = "EnvironmentToolWindowTabSelectedActiveText",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowTabSelectedBorder, "Tool Window Tab Selected Border") {
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "EnvironmentToolWindowTabSelectedBorder",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowTabSelectedTab, "Tool Window Tab Selected Tab") {
				DefaultBackground = "#FFF5F5F5",
				BackgroundResourceKey = "EnvironmentToolWindowTabSelectedTab",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowTabSelectedText, "Tool Window Tab Selected Text") {
				DefaultForeground = "#FF0E70C0",
				ForegroundResourceKey = "EnvironmentToolWindowTabSelectedText",
			},
			new BrushColorInfo(ColorType.EnvironmentToolWindowTabText, "Tool Window Tab Text") {
				DefaultForeground = "#FF444444",
				ForegroundResourceKey = "EnvironmentToolWindowTabText",
			},
			new BrushColorInfo(ColorType.SearchBoxWatermark, "SearchBox Watermark") {
				DefaultForeground = "#FF6D6D6D",
				ForegroundResourceKey = "SearchBoxWatermarkForeground",
			},
			new BrushColorInfo(ColorType.MemoryWindowDisabled, "Memory Window Disabled") {
				DefaultBackground = "#40000000",
				BackgroundResourceKey = "MemoryWindowDisabled",
			},
			new BrushColorInfo(ColorType.TreeViewNode, "TreeView node") {
				DefaultForeground = "#FF000000",
			},
			new BrushColorInfo(ColorType.EnvironmentDropDownGlyph, "Environment DropDownGlyph") {
				DefaultBackground = "#FF717171",
				BackgroundResourceKey = "EnvironmentDropDownGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentDropDownMouseOverGlyph, "Environment DropDownMouseOverGlyph") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "EnvironmentDropDownMouseOverGlyph",
			},
			new BrushColorInfo(ColorType.EnvironmentDropDownMouseDownGlyph, "Environment DropDownMouseDownGlyph") {
				DefaultBackground = "#FFFFFFFF",
				BackgroundResourceKey = "EnvironmentDropDownMouseDownGlyph",
			},
			new LinearGradientColorInfo(ColorType.EnvironmentCommandBarMouseOverBackground, new Point(0, 1), "Environment CommandBarMouseOverBackground", 0, 0.5, 0.5, 1) {
				ResourceKey = "EnvironmentCommandBarMouseOverBackground",
				DefaultForeground = "#FFC9DEF5",// Environment.CommandBarMouseOverBackgroundBegin
				DefaultBackground = "#FFC9DEF5",// Environment.CommandBarMouseOverBackgroundMiddle1
				DefaultColor3 = "#FFC9DEF5",// Environment.CommandBarMouseOverBackgroundMiddle2
				DefaultColor4 = "#FFC9DEF5",// Environment.CommandBarMouseOverBackgroundEnd
			},
			new LinearGradientColorInfo(ColorType.EnvironmentCommandBarMouseDownBackground, new Point(0, 1), "Environment CommandBarMouseDownBackground", 0, 0.5, 1) {
				ResourceKey = "EnvironmentCommandBarMouseDownBackground",
				DefaultForeground = "#FF007ACC",// Environment.CommandBarMouseDownBackgroundBegin
				DefaultBackground = "#FF007ACC",// Environment.CommandBarMouseDownBackgroundMiddle
				DefaultColor3 = "#FF007ACC",// Environment.CommandBarMouseDownBackgroundEnd
			},
			new BrushColorInfo(ColorType.EnvironmentComboBoxDisabledBackground, "Environment ComboBoxDisabledBackground") {
				DefaultBackground = "#FFEEEEF2",
				BackgroundResourceKey = "EnvironmentComboBoxDisabledBackground",
			},
			new BrushColorInfo(ColorType.EnvironmentIconGeneralStroke, "Environment IconGeneralStroke") {
				DefaultBackground = "#00000000",
				BackgroundResourceKey = "EnvironmentIconGeneralStroke",
			},
			new BrushColorInfo(ColorType.EnvironmentIconGeneralFill, "Environment IconGeneralFill") {
				DefaultBackground = "#FF424242",
				BackgroundResourceKey = "EnvironmentIconGeneralFill",
			},
			new BrushColorInfo(ColorType.EnvironmentIconActionFill, "Environment IconActionFill") {
				DefaultBackground = "#FF00529B",
				BackgroundResourceKey = "EnvironmentIconActionFill",
			},
			new BrushColorInfo(ColorType.SearchControlMouseOverDropDownButtonGlyph, "SearchControl MouseOverDropDownButtonGlyph") {
				DefaultBackground = "#FF1E1E1E",
				BackgroundResourceKey = "SearchControlMouseOverDropDownButtonGlyph",
			},
			new BrushColorInfo(ColorType.LineNumber, "Line number"),
			new BrushColorInfo(ColorType.ReplLineNumberInput1, "REPL line number #1 (input)"),
			new BrushColorInfo(ColorType.ReplLineNumberInput2, "REPL line number #2 (input)"),
			new BrushColorInfo(ColorType.ReplLineNumberOutput, "REPL line number (output)"),
			new BrushColorInfo(ColorType.VisibleWhitespace, "Visible whitespace"),
			new BrushColorInfo(ColorType.HighlightedReference, "Highlighted Reference"),
			new BrushColorInfo(ColorType.HighlightedWrittenReference, "Highlighted Written Reference"),
			new BrushColorInfo(ColorType.HighlightedDefinition, "Highlighted Definition"),
			new BrushColorInfo(ColorType.CurrentStatement, "Current statement"),
			new BrushColorInfo(ColorType.CurrentStatementMarker, "Current statement (marker)"),
			new BrushColorInfo(ColorType.CallReturn, "Call return"),
			new BrushColorInfo(ColorType.CallReturnMarker, "Call return (marker)"),
			new BrushColorInfo(ColorType.ActiveStatementMarker, "Active statement (marker)"),
			new BrushColorInfo(ColorType.BreakpointStatement, "Breakpoint statement"),
			new BrushColorInfo(ColorType.BreakpointStatementMarker, "Breakpoint statement (marker)"),
			new BrushColorInfo(ColorType.SelectedBreakpointStatementMarker, "Selected breakpoint statement (marker)"),
			new BrushColorInfo(ColorType.DisabledBreakpointStatementMarker, "Disabled breakpoint statement (marker)"),
			new BrushColorInfo(ColorType.BraceMatching, "Brace matching"),
			new BrushColorInfo(ColorType.LineSeparator, "Line separator"),
			new BrushColorInfo(ColorType.FindMatchHighlightMarker, "Find Match Highlight"),
			new BrushColorInfo(ColorType.StructureVisualizerNamespace, "Structure Visualizer Namespace"),
			new BrushColorInfo(ColorType.StructureVisualizerType, "Structure Visualizer Type"),
			new BrushColorInfo(ColorType.StructureVisualizerModule, "Structure Visualizer Module"),
			new BrushColorInfo(ColorType.StructureVisualizerValueType, "Structure Visualizer ValueType"),
			new BrushColorInfo(ColorType.StructureVisualizerInterface, "Structure Visualizer Interface"),
			new BrushColorInfo(ColorType.StructureVisualizerMethod, "Structure Visualizer Method"),
			new BrushColorInfo(ColorType.StructureVisualizerAccessor, "Structure Visualizer Accessor"),
			new BrushColorInfo(ColorType.StructureVisualizerAnonymousMethod, "Structure Visualizer AnonymousMethod"),
			new BrushColorInfo(ColorType.StructureVisualizerConstructor, "Structure Visualizer Constructor"),
			new BrushColorInfo(ColorType.StructureVisualizerDestructor, "Structure Visualizer Destructor"),
			new BrushColorInfo(ColorType.StructureVisualizerOperator, "Structure Visualizer Operator"),
			new BrushColorInfo(ColorType.StructureVisualizerConditional, "Structure Visualizer Conditional"),
			new BrushColorInfo(ColorType.StructureVisualizerLoop, "Structure Visualizer Loop"),
			new BrushColorInfo(ColorType.StructureVisualizerProperty, "Structure Visualizer Property"),
			new BrushColorInfo(ColorType.StructureVisualizerEvent, "Structure Visualizer Event"),
			new BrushColorInfo(ColorType.StructureVisualizerTry, "Structure Visualizer Try"),
			new BrushColorInfo(ColorType.StructureVisualizerCatch, "Structure Visualizer Catch"),
			new BrushColorInfo(ColorType.StructureVisualizerFilter, "Structure Visualizer Filter"),
			new BrushColorInfo(ColorType.StructureVisualizerFinally, "Structure Visualizer Finally"),
			new BrushColorInfo(ColorType.StructureVisualizerFault, "Structure Visualizer Fault"),
			new BrushColorInfo(ColorType.StructureVisualizerLock, "Structure Visualizer Lock"),
			new BrushColorInfo(ColorType.StructureVisualizerUsing, "Structure Visualizer Using"),
			new BrushColorInfo(ColorType.StructureVisualizerFixed, "Structure Visualizer Fixed"),
			new BrushColorInfo(ColorType.StructureVisualizerSwitch, "Structure Visualizer Switch"),
			new BrushColorInfo(ColorType.StructureVisualizerCase, "Structure Visualizer Case"),
			new BrushColorInfo(ColorType.StructureVisualizerLocalFunction, "Structure Visualizer Local Function"),
			new BrushColorInfo(ColorType.StructureVisualizerOther, "Structure Visualizer Other"),
			new BrushColorInfo(ColorType.CompletionMatchHighlight, "Completion Match Highlight"),
			new BrushColorInfo(ColorType.CompletionSuffix, "Completion Suffix"),
			new BrushColorInfo(ColorType.SignatureHelpDocumentation, "Signature Help Documentation"),
			new BrushColorInfo(ColorType.SignatureHelpCurrentParameter, "Signature Help Current Parameter"),
			new BrushColorInfo(ColorType.SignatureHelpParameter, "Signature Help Parameter"),
			new BrushColorInfo(ColorType.SignatureHelpParameterDocumentation, "Signature Help Parameter Documentation"),
			new BrushColorInfo(ColorType.Url, "URL"),
			new BrushColorInfo(ColorType.HexPeDosHeader, "HexPeDosHeader"),
			new BrushColorInfo(ColorType.HexPeFileHeader, "HexPeFileHeader"),
			new BrushColorInfo(ColorType.HexPeOptionalHeader32, "HexPeOptionalHeader32"),
			new BrushColorInfo(ColorType.HexPeOptionalHeader64, "HexPeOptionalHeader64"),
			new BrushColorInfo(ColorType.HexPeSection, "HexPeSection"),
			new BrushColorInfo(ColorType.HexPeSectionName, "HexPeSectionName"),
			new BrushColorInfo(ColorType.HexCor20Header, "HexCor20Header"),
			new BrushColorInfo(ColorType.HexStorageSignature, "HexStorageSignature"),
			new BrushColorInfo(ColorType.HexStorageHeader, "HexStorageHeader"),
			new BrushColorInfo(ColorType.HexStorageStream, "HexStorageStream"),
			new BrushColorInfo(ColorType.HexStorageStreamName, "HexStorageStreamName"),
			new BrushColorInfo(ColorType.HexStorageStreamNameInvalid, "HexStorageStreamNameInvalid"),
			new BrushColorInfo(ColorType.HexTablesStream, "HexTablesStream"),
			new BrushColorInfo(ColorType.HexTableName, "HexTableName"),
			new BrushColorInfo(ColorType.DocumentListMatchHighlight, "Document List Match Highlight"),
			new BrushColorInfo(ColorType.GacMatchHighlight, "GAC Match Highlight"),
			new BrushColorInfo(ColorType.AppSettingsTreeViewNodeMatchHighlight, "AppSettings TreeView Node Match Highlight"),
			new BrushColorInfo(ColorType.AppSettingsTextMatchHighlight, "AppSettings Text Match Highlight"),
			new BrushColorInfo(ColorType.XmlDocToolTipHeader, "XML doc tooltip"),
			new BrushColorInfo(ColorType.DefaultText, "Default text") {
				DefaultForeground = "Black",
				DefaultBackground = "White",
				Children = new ColorInfo[] {
					new BrushColorInfo(ColorType.Text, "Default text color in text view") {
						Children = new ColorInfo[] {
							new BrushColorInfo(ColorType.Operator, "Operator"),
							new BrushColorInfo(ColorType.Punctuation, "Punctuation"),
							new BrushColorInfo(ColorType.Comment, "Comments"),
							new BrushColorInfo(ColorType.XmlDocCommentAttributeName, "XML Doc Comment AttributeName"),
							new BrushColorInfo(ColorType.XmlDocCommentAttributeQuotes, "XML Doc Comment AttributeQuotes"),
							new BrushColorInfo(ColorType.XmlDocCommentAttributeValue, "XML Doc Comment AttributeValue"),
							new BrushColorInfo(ColorType.XmlDocCommentCDataSection, "XML Doc Comment CData Section"),
							new BrushColorInfo(ColorType.XmlDocCommentComment, "XML Doc Comment Comment"),
							new BrushColorInfo(ColorType.XmlDocCommentDelimiter, "XML Doc Comment Delimiter"),
							new BrushColorInfo(ColorType.XmlDocCommentEntityReference, "XML Doc Comment Entity Reference"),
							new BrushColorInfo(ColorType.XmlDocCommentName, "XML Doc Comment Name"),
							new BrushColorInfo(ColorType.XmlDocCommentProcessingInstruction, "XML Doc Comment Processing Instruction"),
							new BrushColorInfo(ColorType.XmlDocCommentText, "XML Doc Comment Text"),

							new BrushColorInfo(ColorType.XmlLiteralAttributeName, "XML Literal Attribute Name"),
							new BrushColorInfo(ColorType.XmlLiteralAttributeQuotes, "XML Literal Attribute Quotes"),
							new BrushColorInfo(ColorType.XmlLiteralAttributeValue, "XML Literal Attribute Value"),
							new BrushColorInfo(ColorType.XmlLiteralCDataSection, "XML Literal CData Section"),
							new BrushColorInfo(ColorType.XmlLiteralComment, "XML Literal Comment"),
							new BrushColorInfo(ColorType.XmlLiteralDelimiter, "XML Literal Delimiter"),
							new BrushColorInfo(ColorType.XmlLiteralEmbeddedExpression, "XML Literal Embedded Expression"),
							new BrushColorInfo(ColorType.XmlLiteralEntityReference, "XML Literal Entity Reference"),
							new BrushColorInfo(ColorType.XmlLiteralName, "XML Literal Name"),
							new BrushColorInfo(ColorType.XmlLiteralProcessingInstruction, "XML Literal Processing Instruction"),
							new BrushColorInfo(ColorType.XmlLiteralText, "XML Literal Text"),

							new BrushColorInfo(ColorType.XmlAttribute, "XML Attribute Name"),
							new BrushColorInfo(ColorType.XmlAttributeQuotes, "XML Attribute Quotes"),
							new BrushColorInfo(ColorType.XmlAttributeValue, "XML Attribute Value"),
							new BrushColorInfo(ColorType.XmlCDataSection, "XML CData Section"),
							new BrushColorInfo(ColorType.XmlComment, "XML Comment"),
							new BrushColorInfo(ColorType.XmlDelimiter, "XML Delimiter"),
							new BrushColorInfo(ColorType.XmlKeyword, "XML Keyword"),
							new BrushColorInfo(ColorType.XmlName, "XML Name"),
							new BrushColorInfo(ColorType.XmlProcessingInstruction, "XML Processing Instruction"),
							new BrushColorInfo(ColorType.XmlText, "XML Text"),

							new BrushColorInfo(ColorType.XamlAttribute, "XAML Attribute Name"),
							new BrushColorInfo(ColorType.XamlAttributeQuotes, "XAML Attribute Quotes"),
							new BrushColorInfo(ColorType.XamlAttributeValue, "XAML Attribute Value"),
							new BrushColorInfo(ColorType.XamlCDataSection, "XAML CData Section"),
							new BrushColorInfo(ColorType.XamlComment, "XAML Comment"),
							new BrushColorInfo(ColorType.XamlDelimiter, "XAML Delimiter"),
							new BrushColorInfo(ColorType.XamlKeyword, "XAML Keyword"),
							new BrushColorInfo(ColorType.XamlMarkupExtensionClass, "XAML Markup Extension Class"),
							new BrushColorInfo(ColorType.XamlMarkupExtensionParameterName, "XAML Markup Extension Parameter Name"),
							new BrushColorInfo(ColorType.XamlMarkupExtensionParameterValue, "XAML Markup Extension Parameter Value"),
							new BrushColorInfo(ColorType.XamlName, "XAML Name"),
							new BrushColorInfo(ColorType.XamlProcessingInstruction, "XAML Processing Instruction"),
							new BrushColorInfo(ColorType.XamlText, "XAML Text"),

							new BrushColorInfo(ColorType.Number, "Numbers"),
							new BrushColorInfo(ColorType.String, "String"),
							new BrushColorInfo(ColorType.VerbatimString, "Verbatim string"),
							new BrushColorInfo(ColorType.Char, "Char"),
							new BrushColorInfo(ColorType.Keyword, "Keyword"),
							new BrushColorInfo(ColorType.Namespace, "Namespace"),
							new BrushColorInfo(ColorType.Type, "Type"),
							new BrushColorInfo(ColorType.SealedType, "Sealed type"),
							new BrushColorInfo(ColorType.StaticType, "Static type"),
							new BrushColorInfo(ColorType.Delegate, "Delegate"),
							new BrushColorInfo(ColorType.Enum, "Enum"),
							new BrushColorInfo(ColorType.Interface, "Interface"),
							new BrushColorInfo(ColorType.ValueType, "Value type"),
							new BrushColorInfo(ColorType.TypeGenericParameter, "Generic type parameter"),
							new BrushColorInfo(ColorType.MethodGenericParameter, "Generic method parameter"),
							new BrushColorInfo(ColorType.InstanceMethod, "Instance method"),
							new BrushColorInfo(ColorType.StaticMethod, "Static method"),
							new BrushColorInfo(ColorType.ExtensionMethod, "Extension method"),
							new BrushColorInfo(ColorType.InstanceField, "Instance field"),
							new BrushColorInfo(ColorType.EnumField, "Enum field"),
							new BrushColorInfo(ColorType.LiteralField, "Literal field"),
							new BrushColorInfo(ColorType.StaticField, "Static field"),
							new BrushColorInfo(ColorType.InstanceEvent, "Instance event"),
							new BrushColorInfo(ColorType.StaticEvent, "Static event"),
							new BrushColorInfo(ColorType.InstanceProperty, "Instance property"),
							new BrushColorInfo(ColorType.StaticProperty, "Static property"),
							new BrushColorInfo(ColorType.Local, "Local variable"),
							new BrushColorInfo(ColorType.Parameter, "Method parameter"),
							new BrushColorInfo(ColorType.PreprocessorKeyword, "Preprocessor Keyword"),
							new BrushColorInfo(ColorType.PreprocessorText, "Preprocessor Text"),
							new BrushColorInfo(ColorType.Label, "Label"),
							new BrushColorInfo(ColorType.OpCode, "Opcode"),
							new BrushColorInfo(ColorType.ILDirective, "IL directive"),
							new BrushColorInfo(ColorType.ILModule, "IL module"),
							new BrushColorInfo(ColorType.ExcludedCode, "Excluded code"),
							new BrushColorInfo(ColorType.Assembly, "Assembly"),
							new BrushColorInfo(ColorType.AssemblyExe, "Executable Assembly"),
							new BrushColorInfo(ColorType.Module, "Module"),
							new BrushColorInfo(ColorType.DirectoryPart, "Directory part"),
							new BrushColorInfo(ColorType.FileNameNoExtension, "Filename without extension"),
							new BrushColorInfo(ColorType.FileExtension, "File extension"),
							new BrushColorInfo(ColorType.Error, "Error"),
							new BrushColorInfo(ColorType.ToStringEval, "ToString() Eval"),
							new BrushColorInfo(ColorType.ReplPrompt1, "REPL prompt #1"),
							new BrushColorInfo(ColorType.ReplPrompt2, "REPL prompt #2"),
							new BrushColorInfo(ColorType.ReplOutputText, "REPL output text"),
							new BrushColorInfo(ColorType.ReplScriptOutputText, "REPL script output text"),
							new BrushColorInfo(ColorType.Black, ""),
							new BrushColorInfo(ColorType.Blue, ""),
							new BrushColorInfo(ColorType.Cyan, ""),
							new BrushColorInfo(ColorType.DarkBlue, ""),
							new BrushColorInfo(ColorType.DarkCyan, ""),
							new BrushColorInfo(ColorType.DarkGray, ""),
							new BrushColorInfo(ColorType.DarkGreen, ""),
							new BrushColorInfo(ColorType.DarkMagenta, ""),
							new BrushColorInfo(ColorType.DarkRed, ""),
							new BrushColorInfo(ColorType.DarkYellow, ""),
							new BrushColorInfo(ColorType.Gray, ""),
							new BrushColorInfo(ColorType.Green, ""),
							new BrushColorInfo(ColorType.Magenta, ""),
							new BrushColorInfo(ColorType.Red, ""),
							new BrushColorInfo(ColorType.White, ""),
							new BrushColorInfo(ColorType.Yellow, ""),
							new BrushColorInfo(ColorType.InvBlack, ""),
							new BrushColorInfo(ColorType.InvBlue, ""),
							new BrushColorInfo(ColorType.InvCyan, ""),
							new BrushColorInfo(ColorType.InvDarkBlue, ""),
							new BrushColorInfo(ColorType.InvDarkCyan, ""),
							new BrushColorInfo(ColorType.InvDarkGray, ""),
							new BrushColorInfo(ColorType.InvDarkGreen, ""),
							new BrushColorInfo(ColorType.InvDarkMagenta, ""),
							new BrushColorInfo(ColorType.InvDarkRed, ""),
							new BrushColorInfo(ColorType.InvDarkYellow, ""),
							new BrushColorInfo(ColorType.InvGray, ""),
							new BrushColorInfo(ColorType.InvGreen, ""),
							new BrushColorInfo(ColorType.InvMagenta, ""),
							new BrushColorInfo(ColorType.InvRed, ""),
							new BrushColorInfo(ColorType.InvWhite, ""),
							new BrushColorInfo(ColorType.InvYellow, ""),
							new BrushColorInfo(ColorType.DebugLogExceptionHandled, "Debug output handled exception messages"),
							new BrushColorInfo(ColorType.DebugLogExceptionUnhandled, "Debug output unhandled exception messages"),
							new BrushColorInfo(ColorType.DebugLogStepFiltering, "Debug output step filtering messages"),
							new BrushColorInfo(ColorType.DebugLogLoadModule, "Debug output load module messages"),
							new BrushColorInfo(ColorType.DebugLogUnloadModule, "Debug output unload module messages"),
							new BrushColorInfo(ColorType.DebugLogExitProcess, "Debug output process exit messages"),
							new BrushColorInfo(ColorType.DebugLogExitThread, "Debug output thread exit messages"),
							new BrushColorInfo(ColorType.DebugLogProgramOutput, "Debug output program output messages"),
							new BrushColorInfo(ColorType.DebugLogMDA, "Debug output MDA messages"),
							new BrushColorInfo(ColorType.DebugLogTimestamp, "Debug output timestamp"),
							new BrushColorInfo(ColorType.HexText, "Default text color in hex view"),
							new BrushColorInfo(ColorType.HexOffset, "Hex Offset"),
							new BrushColorInfo(ColorType.HexByte0, "Hex Byte Color #0"),
							new BrushColorInfo(ColorType.HexByte1, "Hex Byte Color #1"),
							new BrushColorInfo(ColorType.HexByteError, "Hex Byte Color Error"),
							new BrushColorInfo(ColorType.HexAscii, "Hex ASCII"),
							new BrushColorInfo(ColorType.HexCaret, "Hex Caret"),
							new BrushColorInfo(ColorType.HexInactiveCaret, "Hex Inactive Caret"),
						},
					},
				},
			},
		};
	}
}
