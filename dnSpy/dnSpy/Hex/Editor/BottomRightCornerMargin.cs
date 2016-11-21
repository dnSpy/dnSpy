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
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(WpfHexViewMarginProvider))]
	[VSTE.MarginContainer(PredefinedHexMarginNames.BottomRightCorner)]
	[VSUTIL.Name(BottomRightCornerMargin.NAME)]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Interactive)]
	[VSTE.GridCellLength(1.0), VSTE.GridUnitType(GridUnitType.Star)]
	sealed class BottomRightCornerMarginProvider : WpfHexViewMarginProvider {
		public override WpfHexViewMargin CreateMargin(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin marginContainer) =>
			new BottomRightCornerMargin(wpfHexViewHost);
	}

	sealed class BottomRightCornerMargin : WpfHexViewMargin {
		public const string NAME = "BottomRightCornerMargin";

		public override bool Enabled => wpfHexViewHost.HexView.Options.IsHorizontalScrollBarEnabled();
		public override double MarginSize => canvas.ActualHeight;
		public override FrameworkElement VisualElement => canvas;

		readonly Canvas canvas;
		readonly WpfHexViewHost wpfHexViewHost;

		public BottomRightCornerMargin(WpfHexViewHost wpfHexViewHost) {
			if (wpfHexViewHost == null)
				throw new ArgumentNullException(nameof(wpfHexViewHost));
			canvas = new Canvas();
			this.wpfHexViewHost = wpfHexViewHost;
			wpfHexViewHost.HexView.Options.OptionChanged += Options_OptionChanged;
			canvas.SetResourceReference(Control.BackgroundProperty, "EnvironmentScrollBarBackground");
			UpdateVisibility();
		}

		void UpdateVisibility() => canvas.Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;

		public override HexViewMargin GetHexViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(NAME, marginName) ? this : null;

		void Options_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultHexViewHostOptions.HorizontalScrollBarName)
				UpdateVisibility();
		}

		protected override void DisposeCore() => wpfHexViewHost.HexView.Options.OptionChanged -= Options_OptionChanged;
	}
}
