/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewMarginProvider))]
	[MarginContainer(PredefinedMarginNames.VerticalScrollBarContainer)]
	[Name(PredefinedMarginNames.VerticalScrollBar)]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	sealed class VerticalScrollBarMarginProvider : IWpfTextViewMarginProvider {
		readonly IScrollMapFactoryService scrollMapFactoryService;

		[ImportingConstructor]
		VerticalScrollBarMarginProvider(IScrollMapFactoryService scrollMapFactoryService) => this.scrollMapFactoryService = scrollMapFactoryService;

		public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) =>
			new VerticalScrollBarMargin(scrollMapFactoryService, wpfTextViewHost);
	}

	sealed class VerticalScrollBarMargin : DsScrollBar, IWpfTextViewMargin {
		public bool Enabled => wpfTextViewHost.TextView.Options.IsVerticalScrollBarEnabled();
		public double MarginSize => ActualWidth;
		public FrameworkElement VisualElement => this;
		SnapshotPoint FirstVisibleLinePoint =>
			wpfTextViewHost.TextView.TextViewLines?.FirstVisibleLine.Start ??
			new SnapshotPoint(wpfTextViewHost.TextView.TextSnapshot, 0);

		readonly IWpfTextViewHost wpfTextViewHost;
		readonly IScrollMap scrollMap;

		public VerticalScrollBarMargin(IScrollMapFactoryService scrollMapFactoryService, IWpfTextViewHost wpfTextViewHost) {
			if (scrollMapFactoryService == null)
				throw new ArgumentNullException(nameof(scrollMapFactoryService));
			this.wpfTextViewHost = wpfTextViewHost ?? throw new ArgumentNullException(nameof(wpfTextViewHost));
			scrollMap = scrollMapFactoryService.Create(wpfTextViewHost.TextView, true);
			IsVisibleChanged += VerticalScrollBarMargin_IsVisibleChanged;
			wpfTextViewHost.TextView.Options.OptionChanged += Options_OptionChanged;
			SetResourceReference(StyleProperty, typeof(ScrollBar));
			HorizontalAlignment = HorizontalAlignment.Center;
			Orientation = System.Windows.Controls.Orientation.Vertical;
			SmallChange = 1;
			UpdateVisibility();
		}

		void UpdateVisibility() => Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;

		public ITextViewMargin GetTextViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(PredefinedMarginNames.VerticalScrollBar, marginName) ? this : null;

		void ScrollMap_MappingChanged(object sender, EventArgs e) => UpdateMinMax();

		void UpdateMinMax() {
			Minimum = scrollMap.Start;
			Maximum = scrollMap.End;
		}

		protected override void OnScroll(ScrollEventArgs e) {
			if (!Enabled)
				return;

			// Special case some commands since the scroll map doesn't know the exact number of visual
			// lines. Without this code, a SmallIncrement command will scroll down one real line (which
			// could be multiple visual lines).
			switch (e.ScrollEventType) {
			case ScrollEventType.LargeDecrement:
				wpfTextViewHost.TextView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Up);
				break;

			case ScrollEventType.LargeIncrement:
				wpfTextViewHost.TextView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Down);
				break;

			case ScrollEventType.SmallDecrement:
				wpfTextViewHost.TextView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Up);
				break;

			case ScrollEventType.SmallIncrement:
				wpfTextViewHost.TextView.ViewScroller.ScrollViewportVerticallyByLine(ScrollDirection.Down);
				break;

			default:
				var bufferPosition = scrollMap.GetBufferPositionAtCoordinate(e.NewValue);
				wpfTextViewHost.TextView.DisplayTextLineContainingBufferPosition(bufferPosition, 0, ViewRelativePosition.Top);
				break;
			}
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewHostOptions.VerticalScrollBarName)
				UpdateVisibility();
		}

		void VerticalScrollBarMargin_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (Visibility == Visibility.Visible) {
				RegisterEvents();
				UpdateMinMax();
				LargeChange = scrollMap.ThumbSize;
				ViewportSize = scrollMap.ThumbSize;
				Value = scrollMap.GetCoordinateAtBufferPosition(FirstVisibleLinePoint);
			}
			else
				UnregisterEvents();
		}

		void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			LargeChange = scrollMap.ThumbSize;
			ViewportSize = scrollMap.ThumbSize;
			Value = scrollMap.GetCoordinateAtBufferPosition(FirstVisibleLinePoint);
		}

		bool hasRegisteredEvents;
		void RegisterEvents() {
			if (hasRegisteredEvents)
				return;
			if (wpfTextViewHost.IsClosed)
				return;
			hasRegisteredEvents = true;
			wpfTextViewHost.TextView.LayoutChanged += TextView_LayoutChanged;
			scrollMap.MappingChanged += ScrollMap_MappingChanged;
		}

		void UnregisterEvents() {
			hasRegisteredEvents = false;
			wpfTextViewHost.TextView.LayoutChanged -= TextView_LayoutChanged;
			scrollMap.MappingChanged -= ScrollMap_MappingChanged;
		}

		public void Dispose() {
			IsVisibleChanged -= VerticalScrollBarMargin_IsVisibleChanged;
			wpfTextViewHost.TextView.Options.OptionChanged -= Options_OptionChanged;
			UnregisterEvents();
			(scrollMap as IDisposable)?.Dispose();
		}
	}
}
