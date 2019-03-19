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
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewMarginProvider))]
	[MarginContainer(PredefinedMarginNames.HorizontalScrollBarContainer)]
	[Name(PredefinedMarginNames.HorizontalScrollBar)]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	sealed class HorizontalScrollBarMarginProvider : IWpfTextViewMarginProvider {
		public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) =>
			new HorizontalScrollBarMargin(wpfTextViewHost);
	}

	sealed class HorizontalScrollBarMargin : DsScrollBar, IWpfTextViewMargin {
		public bool Enabled => wpfTextViewHost.TextView.Options.IsHorizontalScrollBarEnabled();
		public double MarginSize => ActualHeight;
		public FrameworkElement VisualElement => this;
		bool IsWordWrap => (wpfTextViewHost.TextView.Options.WordWrapStyle() & WordWrapStyles.WordWrap) != 0;

		readonly IWpfTextViewHost wpfTextViewHost;

		public HorizontalScrollBarMargin(IWpfTextViewHost wpfTextViewHost) {
			this.wpfTextViewHost = wpfTextViewHost ?? throw new ArgumentNullException(nameof(wpfTextViewHost));
			IsVisibleChanged += HorizontalScrollBarMargin_IsVisibleChanged;
			wpfTextViewHost.TextView.Options.OptionChanged += Options_OptionChanged;
			SetResourceReference(StyleProperty, typeof(ScrollBar));
			VerticalAlignment = VerticalAlignment.Top;
			Orientation = System.Windows.Controls.Orientation.Horizontal;
			SmallChange = 12.0;
			Minimum = 0;
			UpdateVisibility();
		}

		void UpdateVisibility() => Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;

		public ITextViewMargin GetTextViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(PredefinedMarginNames.HorizontalScrollBar, marginName) ? this : null;

		protected override void OnScroll(ScrollEventArgs e) {
			if (!Enabled)
				return;
			wpfTextViewHost.TextView.ViewportLeft = Value;
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewHostOptions.HorizontalScrollBarName)
				UpdateVisibility();
			else if (!Enabled) {
				// Ignore any other options
			}
			else if (e.OptionId == DefaultTextViewOptions.WordWrapStyleName) {
				IsEnabled = !IsWordWrap;
				UpdateMaximum();
			}
		}

		void HorizontalScrollBarMargin_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (Visibility == Visibility.Visible) {
				RegisterEvents();
				IsEnabled = !IsWordWrap;
				LargeChange = wpfTextViewHost.TextView.ViewportWidth;
				ViewportSize = wpfTextViewHost.TextView.ViewportWidth;
				UpdateMaximum();
				Value = wpfTextViewHost.TextView.ViewportLeft;
			}
			else
				UnregisterEvents();
		}

		void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			LargeChange = wpfTextViewHost.TextView.ViewportWidth;
			ViewportSize = wpfTextViewHost.TextView.ViewportWidth;
			UpdateMaximum();
			Value = wpfTextViewHost.TextView.ViewportLeft;
		}

		void UpdateMaximum() => Maximum = IsWordWrap ? 0 : Math.Max(wpfTextViewHost.TextView.ViewportLeft, wpfTextViewHost.TextView.MaxTextRightCoordinate - wpfTextViewHost.TextView.ViewportWidth + WpfTextViewConstants.EXTRA_HORIZONTAL_SCROLLBAR_WIDTH);

		bool hasRegisteredEvents;
		void RegisterEvents() {
			if (hasRegisteredEvents)
				return;
			if (wpfTextViewHost.IsClosed)
				return;
			hasRegisteredEvents = true;
			wpfTextViewHost.TextView.LayoutChanged += TextView_LayoutChanged;
		}

		void UnregisterEvents() {
			hasRegisteredEvents = false;
			wpfTextViewHost.TextView.LayoutChanged -= TextView_LayoutChanged;
		}

		public void Dispose() {
			IsVisibleChanged -= HorizontalScrollBarMargin_IsVisibleChanged;
			wpfTextViewHost.TextView.Options.OptionChanged -= Options_OptionChanged;
			UnregisterEvents();
		}
	}
}
