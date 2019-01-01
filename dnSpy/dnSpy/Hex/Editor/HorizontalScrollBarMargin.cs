/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Windows.Controls.Primitives;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using TE = dnSpy.Text.Editor;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(WpfHexViewMarginProvider))]
	[VSTE.MarginContainer(PredefinedHexMarginNames.HorizontalScrollBarContainer)]
	[VSUTIL.Name(PredefinedHexMarginNames.HorizontalScrollBar)]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Interactive)]
	sealed class HorizontalScrollBarMarginProvider : WpfHexViewMarginProvider {
		public override WpfHexViewMargin CreateMargin(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin marginContainer) =>
			new HorizontalScrollBarMargin(wpfHexViewHost);
	}

	sealed class HorizontalScrollBarMargin : WpfHexViewMargin {
		public override bool Enabled => wpfHexViewHost.HexView.Options.IsHorizontalScrollBarEnabled();
		public override double MarginSize => theScrollBar.ActualHeight;
		public override FrameworkElement VisualElement => theScrollBar;

		sealed class TheScrollBar : TE.DsScrollBar {
			readonly HorizontalScrollBarMargin owner;
			public TheScrollBar(HorizontalScrollBarMargin owner) => this.owner = owner;
			protected override void OnScroll(ScrollEventArgs e) => owner.OnScroll(Value);
		}

		readonly TheScrollBar theScrollBar;
		readonly WpfHexViewHost wpfHexViewHost;

		public HorizontalScrollBarMargin(WpfHexViewHost wpfHexViewHost) {
			theScrollBar = new TheScrollBar(this);
			this.wpfHexViewHost = wpfHexViewHost ?? throw new ArgumentNullException(nameof(wpfHexViewHost));
			theScrollBar.IsVisibleChanged += HorizontalScrollBarMargin_IsVisibleChanged;
			wpfHexViewHost.HexView.Options.OptionChanged += Options_OptionChanged;
			theScrollBar.SetResourceReference(FrameworkElement.StyleProperty, typeof(ScrollBar));
			theScrollBar.VerticalAlignment = VerticalAlignment.Top;
			theScrollBar.Orientation = System.Windows.Controls.Orientation.Horizontal;
			theScrollBar.SmallChange = 12.0;
			theScrollBar.Minimum = 0;
			UpdateVisibility();
		}

		void UpdateVisibility() => theScrollBar.Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;

		public override HexViewMargin GetHexViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(PredefinedHexMarginNames.HorizontalScrollBar, marginName) ? this : null;

		void OnScroll(double value) {
			if (!Enabled)
				return;
			wpfHexViewHost.HexView.ViewportLeft = value;
		}

		void Options_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultHexViewHostOptions.HorizontalScrollBarName)
				UpdateVisibility();
		}

		void HorizontalScrollBarMargin_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (theScrollBar.Visibility == Visibility.Visible) {
				RegisterEvents();
				theScrollBar.IsEnabled = true;
				theScrollBar.LargeChange = wpfHexViewHost.HexView.ViewportWidth;
				theScrollBar.ViewportSize = wpfHexViewHost.HexView.ViewportWidth;
				UpdateMaximum();
				theScrollBar.Value = wpfHexViewHost.HexView.ViewportLeft;
			}
			else
				UnregisterEvents();
		}

		void HexView_LayoutChanged(object sender, HexViewLayoutChangedEventArgs e) {
			theScrollBar.LargeChange = wpfHexViewHost.HexView.ViewportWidth;
			theScrollBar.ViewportSize = wpfHexViewHost.HexView.ViewportWidth;
			UpdateMaximum();
			theScrollBar.Value = wpfHexViewHost.HexView.ViewportLeft;
		}

		void UpdateMaximum() => theScrollBar.Maximum = Math.Max(wpfHexViewHost.HexView.ViewportLeft, wpfHexViewHost.HexView.MaxTextRightCoordinate - wpfHexViewHost.HexView.ViewportWidth + WpfHexViewConstants.EXTRA_HORIZONTAL_SCROLLBAR_WIDTH);

		bool hasRegisteredEvents;
		void RegisterEvents() {
			if (hasRegisteredEvents)
				return;
			if (wpfHexViewHost.IsClosed)
				return;
			hasRegisteredEvents = true;
			wpfHexViewHost.HexView.LayoutChanged += HexView_LayoutChanged;
		}

		void UnregisterEvents() {
			hasRegisteredEvents = false;
			wpfHexViewHost.HexView.LayoutChanged -= HexView_LayoutChanged;
		}

		protected override void DisposeCore() {
			theScrollBar.IsVisibleChanged -= HorizontalScrollBarMargin_IsVisibleChanged;
			wpfHexViewHost.HexView.Options.OptionChanged -= Options_OptionChanged;
			UnregisterEvents();
		}
	}
}
