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
using System.Windows.Input;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using TE = dnSpy.Text.Editor;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(WpfHexViewMarginProvider))]
	[VSTE.MarginContainer(PredefinedHexMarginNames.VerticalScrollBarContainer)]
	[VSUTIL.Name(PredefinedHexMarginNames.VerticalScrollBar)]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Interactive)]
	sealed class VerticalScrollBarMarginProvider : WpfHexViewMarginProvider {
		readonly HexScrollMapFactoryService scrollMapFactoryService;

		[ImportingConstructor]
		VerticalScrollBarMarginProvider(HexScrollMapFactoryService scrollMapFactoryService) => this.scrollMapFactoryService = scrollMapFactoryService;

		public override WpfHexViewMargin CreateMargin(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin marginContainer) =>
			new VerticalScrollBarMargin(scrollMapFactoryService, wpfHexViewHost);
	}

	sealed class VerticalScrollBarMargin : WpfHexViewMargin {
		public override bool Enabled => wpfHexViewHost.HexView.Options.IsVerticalScrollBarEnabled();
		public override double MarginSize => theScrollBar.ActualWidth;
		public override FrameworkElement VisualElement => theScrollBar;
		HexBufferPoint FirstVisibleLinePoint =>
			wpfHexViewHost.HexView.HexViewLines?.FirstVisibleLine.BufferStart ??
			wpfHexViewHost.HexView.BufferLines.GetBufferPositionFromLineNumber(0);

		sealed class TheScrollBar : TE.DsScrollBar {
			readonly VerticalScrollBarMargin owner;
			public TheScrollBar(VerticalScrollBarMargin owner) {
				this.owner = owner;

				// The default implementation can't handle too large max values. It has code that
				// checks whether OldValue + Inc == OldValue, and that's the case when OldValue is
				// large enough even when Inc != 0.
				Add(ScrollBar.LineUpCommand, ScrollEventType.SmallDecrement);
				Add(ScrollBar.LineDownCommand, ScrollEventType.SmallIncrement);
				Add(ScrollBar.PageUpCommand, ScrollEventType.LargeDecrement);
				Add(ScrollBar.PageDownCommand, ScrollEventType.LargeIncrement);
			}

			void Add(RoutedCommand routedCommand, ScrollEventType @event) =>
				CommandBindings.Add(new CommandBinding(routedCommand,
					(s, e) => owner.OnScroll(new ScrollEventArgs(@event, Value)),
					(s, e) => { e.CanExecute = true; }));

			protected override void OnScroll(ScrollEventArgs e) => owner.OnScroll(e);
		}

		readonly TheScrollBar theScrollBar;
		readonly WpfHexViewHost wpfHexViewHost;
		readonly HexScrollMap scrollMap;

		public VerticalScrollBarMargin(HexScrollMapFactoryService scrollMapFactoryService, WpfHexViewHost wpfHexViewHost) {
			if (scrollMapFactoryService == null)
				throw new ArgumentNullException(nameof(scrollMapFactoryService));
			theScrollBar = new TheScrollBar(this);
			this.wpfHexViewHost = wpfHexViewHost ?? throw new ArgumentNullException(nameof(wpfHexViewHost));
			scrollMap = scrollMapFactoryService.Create(wpfHexViewHost.HexView);
			theScrollBar.IsVisibleChanged += VerticalScrollBarMargin_IsVisibleChanged;
			wpfHexViewHost.HexView.Options.OptionChanged += Options_OptionChanged;
			theScrollBar.SetResourceReference(FrameworkElement.StyleProperty, typeof(ScrollBar));
			theScrollBar.HorizontalAlignment = HorizontalAlignment.Center;
			theScrollBar.Orientation = System.Windows.Controls.Orientation.Vertical;
			theScrollBar.SmallChange = 1;
			UpdateVisibility();
		}

		void UpdateVisibility() => theScrollBar.Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;

		public override HexViewMargin GetHexViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(PredefinedHexMarginNames.VerticalScrollBar, marginName) ? this : null;

		void ScrollMap_MappingChanged(object sender, EventArgs e) => UpdateMinMax();

		void UpdateMinMax() {
			theScrollBar.Minimum = scrollMap.Start;
			theScrollBar.Maximum = scrollMap.End;
		}

		void OnScroll(ScrollEventArgs e) {
			if (!Enabled)
				return;

			switch (e.ScrollEventType) {
			case ScrollEventType.LargeDecrement:
				wpfHexViewHost.HexView.ViewScroller.ScrollViewportVerticallyByPage(VSTE.ScrollDirection.Up);
				break;

			case ScrollEventType.LargeIncrement:
				wpfHexViewHost.HexView.ViewScroller.ScrollViewportVerticallyByPage(VSTE.ScrollDirection.Down);
				break;

			case ScrollEventType.SmallDecrement:
				wpfHexViewHost.HexView.ViewScroller.ScrollViewportVerticallyByLine(VSTE.ScrollDirection.Up);
				break;

			case ScrollEventType.SmallIncrement:
				wpfHexViewHost.HexView.ViewScroller.ScrollViewportVerticallyByLine(VSTE.ScrollDirection.Down);
				break;

			default:
				var bufferPosition = scrollMap.GetBufferPositionAtCoordinate(e.NewValue);
				wpfHexViewHost.HexView.DisplayHexLineContainingBufferPosition(bufferPosition, 0, VSTE.ViewRelativePosition.Top);
				break;
			}
		}

		void Options_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultHexViewHostOptions.VerticalScrollBarName)
				UpdateVisibility();
		}

		void VerticalScrollBarMargin_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (theScrollBar.Visibility == Visibility.Visible) {
				RegisterEvents();
				UpdateMinMax();
				theScrollBar.LargeChange = scrollMap.ThumbSize;
				theScrollBar.ViewportSize = scrollMap.ThumbSize;
				theScrollBar.Value = scrollMap.GetCoordinateAtBufferPosition(FirstVisibleLinePoint);
			}
			else
				UnregisterEvents();
		}

		void HexView_LayoutChanged(object sender, HexViewLayoutChangedEventArgs e) {
			theScrollBar.LargeChange = scrollMap.ThumbSize;
			theScrollBar.ViewportSize = scrollMap.ThumbSize;
			theScrollBar.Value = scrollMap.GetCoordinateAtBufferPosition(FirstVisibleLinePoint);
		}

		bool hasRegisteredEvents;
		void RegisterEvents() {
			if (hasRegisteredEvents)
				return;
			if (wpfHexViewHost.IsClosed)
				return;
			hasRegisteredEvents = true;
			wpfHexViewHost.HexView.LayoutChanged += HexView_LayoutChanged;
			scrollMap.MappingChanged += ScrollMap_MappingChanged;
		}

		void UnregisterEvents() {
			hasRegisteredEvents = false;
			wpfHexViewHost.HexView.LayoutChanged -= HexView_LayoutChanged;
			scrollMap.MappingChanged -= ScrollMap_MappingChanged;
		}

		protected override void DisposeCore() {
			theScrollBar.IsVisibleChanged -= VerticalScrollBarMargin_IsVisibleChanged;
			wpfHexViewHost.HexView.Options.OptionChanged -= Options_OptionChanged;
			UnregisterEvents();
			(scrollMap as IDisposable)?.Dispose();
		}
	}
}
