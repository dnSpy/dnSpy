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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Themes;
using dnSpy.Shared.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace dnSpy.Debugger {
	[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforePlugins)]
	sealed class DebuggerColors : IAutoLoaded {
		public static HighlightingColor CodeBreakpointHighlightingColor;
		public static HighlightingColor CodeBreakpointDisabledHighlightingColor;
		public static HighlightingColor StackFrameReturnHighlightingColor;
		public static HighlightingColor StackFrameSelectedHighlightingColor;
		public static HighlightingColor StackFrameCurrentHighlightingColor;

		[ImportingConstructor]
		DebuggerColors(IThemeManager themeManager) {
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			OnThemeUpdated(themeManager);
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			OnThemeUpdated((IThemeManager)sender);
		}

		void OnThemeUpdated(IThemeManager themeManager) {
			var theme = themeManager.Theme;
			CodeBreakpointHighlightingColor = theme.GetTextColor(ColorType.BreakpointStatement).ToHighlightingColor();
			CodeBreakpointDisabledHighlightingColor = theme.GetTextColor(ColorType.DisabledBreakpointStatement).ToHighlightingColor();
			StackFrameCurrentHighlightingColor = theme.GetTextColor(ColorType.CurrentStatement).ToHighlightingColor();
			StackFrameReturnHighlightingColor = theme.GetTextColor(ColorType.ReturnStatement).ToHighlightingColor();
			StackFrameSelectedHighlightingColor = theme.GetTextColor(ColorType.SelectedReturnStatement).ToHighlightingColor();
		}
	}
}
