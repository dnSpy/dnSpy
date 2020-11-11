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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IMarginContextMenuHandlerProvider))]
	[MarginName(PredefinedMarginNames.Glyph)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[TextViewRole(PredefinedDsTextViewRoles.CanHaveGlyphTextMarkerService)]
	sealed class GlyphTextMarkerContextMenuHandlerProvider : IMarginContextMenuHandlerProvider {
		readonly IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl;

		[ImportingConstructor]
		GlyphTextMarkerContextMenuHandlerProvider(IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl) => this.glyphTextMarkerServiceImpl = glyphTextMarkerServiceImpl;

		public IMarginContextMenuHandler? Create(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin margin) =>
			wpfTextViewHost.TextView.Properties.GetOrCreateSingletonProperty(typeof(GlyphTextMarkerServiceMouseProcessor), () => new GlyphTextMarkerServiceMouseProcessor(glyphTextMarkerServiceImpl, wpfTextViewHost, margin));
	}

	[Export(typeof(IGlyphMouseProcessorProvider))]
	[Name(PredefinedDsGlyphMouseProcessorProviders.GlyphTextMarkerService)]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[TextViewRole(PredefinedDsTextViewRoles.CanHaveGlyphTextMarkerService)]
	sealed class GlyphTextMarkerServiceMouseProcessorProvider : IGlyphMouseProcessorProvider {
		readonly IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl;

		[ImportingConstructor]
		GlyphTextMarkerServiceMouseProcessorProvider(IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl) => this.glyphTextMarkerServiceImpl = glyphTextMarkerServiceImpl;

		public IMouseProcessor? GetAssociatedMouseProcessor(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin margin) {
			if (margin.GetTextViewMargin(PredefinedMarginNames.Glyph) == margin)
				return wpfTextViewHost.TextView.Properties.GetOrCreateSingletonProperty(typeof(GlyphTextMarkerServiceMouseProcessor), () => new GlyphTextMarkerServiceMouseProcessor(glyphTextMarkerServiceImpl, wpfTextViewHost, margin));
			return null;
		}
	}

	sealed class GlyphTextMarkerServiceMouseProcessor : MouseProcessorBase, IGlyphTextMarkerListener, IMarginContextMenuHandler, IGlyphTextMarkerSpanProvider {
		readonly GlyphTextViewMarkerService glyphTextViewMarkerService;
		readonly IWpfTextViewHost wpfTextViewHost;
		readonly IWpfTextViewMargin margin;
		readonly IGlyphTextMarkerMouseProcessor[] glyphTextMarkerMouseProcessors;

		public GlyphTextMarkerServiceMouseProcessor(IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl, IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin margin) {
			if (glyphTextMarkerServiceImpl is null)
				throw new ArgumentNullException(nameof(glyphTextMarkerServiceImpl));
			if (wpfTextViewHost is null)
				throw new ArgumentNullException(nameof(wpfTextViewHost));
			glyphTextViewMarkerService = GlyphTextViewMarkerService.GetOrCreate(glyphTextMarkerServiceImpl, wpfTextViewHost.TextView);
			this.wpfTextViewHost = wpfTextViewHost;
			this.margin = margin ?? throw new ArgumentNullException(nameof(margin));
			toolTipDispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal, margin.VisualElement.Dispatcher);
			popup = new Popup { AllowsTransparency = true };

			var list = new List<IGlyphTextMarkerMouseProcessor>();
			foreach (var lazy in glyphTextMarkerServiceImpl.GlyphTextMarkerMouseProcessorProviders) {
				if (lazy.Metadata.TextViewRoles is not null && !wpfTextViewHost.TextView.Roles.ContainsAny(lazy.Metadata.TextViewRoles))
					continue;
				var mouseProcessor = lazy.Value.GetAssociatedMouseProcessor(wpfTextViewHost, margin);
				if (mouseProcessor is not null)
					list.Add(mouseProcessor);
			}
			glyphTextMarkerMouseProcessors = list.ToArray();
			wpfTextViewHost.TextView.Closed += TextView_Closed;
			wpfTextViewHost.TextView.LayoutChanged += TextView_LayoutChanged;
			toolTipDispatcherTimer.Tick += ToolTipDispatcherTimer_Tick;
			popup.Closed += Popup_Closed;
			glyphTextViewMarkerService.AddGlyphTextMarkerListener(this);
		}

		SnapshotSpan IGlyphTextMarkerSpanProvider.GetSpan(IGlyphTextMarker marker) => glyphTextViewMarkerService.GetSpan(marker);

		IWpfTextViewLine? GetLine(MouseEventArgs e) => GetLine(e.GetPosition(margin.VisualElement));
		IWpfTextViewLine? GetLine(Point point) {
			if (point.X < 0 || point.X >= margin.MarginSize)
				return null;
			var line = wpfTextViewHost.TextView.TextViewLines.GetTextViewLineContainingYCoordinate(point.Y + wpfTextViewHost.TextView.ViewportTop);
			var wpfLine = line as IWpfTextViewLine;
			Debug2.Assert((line is not null) == (wpfLine is not null));
			if (wpfLine is null || !wpfLine.IsVisible())
				return null;
			return wpfLine;
		}

		sealed class GlyphTextMarkerHandlerContext : IGlyphTextMarkerHandlerContext {
			public IWpfTextViewHost Host { get; }
			public IWpfTextView TextView => Host.TextView;
			public IWpfTextViewMargin Margin { get; }
			public IWpfTextViewLine Line { get; }
			public IGlyphTextMarkerSpanProvider SpanProvider { get; }
			public GlyphTextMarkerHandlerContext(IWpfTextViewHost host, IWpfTextViewMargin margin, IWpfTextViewLine line, IGlyphTextMarkerSpanProvider spanProvider) {
				Host = host;
				Margin = margin;
				Line = line;
				SpanProvider = spanProvider;
			}
		}

		sealed class GlyphTextMarkerMouseProcessorContext : IGlyphTextMarkerMouseProcessorContext {
			public IWpfTextViewHost Host { get; }
			public IWpfTextView TextView => Host.TextView;
			public IWpfTextViewMargin Margin { get; }
			public IWpfTextViewLine Line { get; }
			public IGlyphTextMarker[] Markers { get; }
			public IGlyphTextMarkerSpanProvider SpanProvider { get; }
			public GlyphTextMarkerMouseProcessorContext(IWpfTextViewHost host, IWpfTextViewMargin margin, IWpfTextViewLine line, IGlyphTextMarker[] markers, IGlyphTextMarkerSpanProvider spanProvider) {
				Host = host;
				Margin = margin;
				Line = line;
				Markers = markers;
				SpanProvider = spanProvider;
			}
		}

		public IEnumerable<GuidObject> GetContextMenuObjects(Point marginRelativePoint) {
			var line = GetLine(marginRelativePoint);
			if (line is null)
				yield break;

			var markers = glyphTextViewMarkerService.GetSortedGlyphTextMarkers(line);

			if (markers.Length > 0) {
				var glyphTextMarkerHandlerContext = new GlyphTextMarkerHandlerContext(wpfTextViewHost, margin, line, this);
				foreach (var marker in markers) {
					foreach (var o in marker.Handler.GetContextMenuObjects(glyphTextMarkerHandlerContext, marker, marginRelativePoint))
						yield return o;
				}
			}

			if (glyphTextMarkerMouseProcessors.Length != 0) {
				var context = new GlyphTextMarkerMouseProcessorContext(wpfTextViewHost, margin, line, markers, this);
				foreach (var processor in glyphTextMarkerMouseProcessors) {
					foreach (var o in processor.GetContextMenuObjects(context, marginRelativePoint))
						yield return o;
				}
			}
		}

		public override void PostprocessMouseDown(MouseButtonEventArgs e) {
			CloseToolTip();
			var line = GetLine(e);
			if (line is null)
				return;

			var markers = glyphTextViewMarkerService.GetSortedGlyphTextMarkers(line);

			IGlyphTextMarkerHandlerContext? glyphTextMarkerHandlerContext = null;
			foreach (var marker in markers) {
				if (marker.Handler.MouseProcessor is null)
					continue;
				if (glyphTextMarkerHandlerContext is null)
					glyphTextMarkerHandlerContext = new GlyphTextMarkerHandlerContext(wpfTextViewHost, margin, line, this);
				marker.Handler.MouseProcessor.OnMouseDown(glyphTextMarkerHandlerContext, marker, e);
				if (e.Handled)
					return;
			}

			if (glyphTextMarkerMouseProcessors.Length != 0) {
				var context = new GlyphTextMarkerMouseProcessorContext(wpfTextViewHost, margin, line, markers, this);
				foreach (var processor in glyphTextMarkerMouseProcessors) {
					processor.OnMouseDown(context, e);
					if (e.Handled)
						return;
				}
			}
		}

		public override void PostprocessMouseUp(MouseButtonEventArgs e) {
			CloseToolTip();
			var line = GetLine(e);
			if (line is null)
				return;

			var markers = glyphTextViewMarkerService.GetSortedGlyphTextMarkers(line);

			IGlyphTextMarkerHandlerContext? glyphTextMarkerHandlerContext = null;
			foreach (var marker in markers) {
				if (marker.Handler.MouseProcessor is null)
					continue;
				if (glyphTextMarkerHandlerContext is null)
					glyphTextMarkerHandlerContext = new GlyphTextMarkerHandlerContext(wpfTextViewHost, margin, line, this);
				marker.Handler.MouseProcessor.OnMouseUp(glyphTextMarkerHandlerContext, marker, e);
				if (e.Handled)
					return;
			}

			if (glyphTextMarkerMouseProcessors.Length != 0) {
				var context = new GlyphTextMarkerMouseProcessorContext(wpfTextViewHost, margin, line, markers, this);
				foreach (var processor in glyphTextMarkerMouseProcessors) {
					processor.OnMouseUp(context, e);
					if (e.Handled)
						return;
				}
			}
		}

		public override void PostprocessMouseLeftButtonDown(MouseButtonEventArgs e) {
			CloseToolTip();
			var line = GetLine(e);
			if (line is null)
				return;

			var markers = glyphTextViewMarkerService.GetSortedGlyphTextMarkers(line);

			IGlyphTextMarkerHandlerContext? glyphTextMarkerHandlerContext = null;
			foreach (var marker in markers) {
				if (marker.Handler.MouseProcessor is null)
					continue;
				if (glyphTextMarkerHandlerContext is null)
					glyphTextMarkerHandlerContext = new GlyphTextMarkerHandlerContext(wpfTextViewHost, margin, line, this);
				marker.Handler.MouseProcessor.OnMouseLeftButtonDown(glyphTextMarkerHandlerContext, marker, e);
				if (e.Handled)
					return;
			}

			if (glyphTextMarkerMouseProcessors.Length != 0) {
				var context = new GlyphTextMarkerMouseProcessorContext(wpfTextViewHost, margin, line, markers, this);
				foreach (var processor in glyphTextMarkerMouseProcessors) {
					processor.OnMouseLeftButtonDown(context, e);
					if (e.Handled)
						return;
				}
			}
		}

		public override void PostprocessMouseLeftButtonUp(MouseButtonEventArgs e) {
			CloseToolTip();
			var line = GetLine(e);
			if (line is null)
				return;

			var markers = glyphTextViewMarkerService.GetSortedGlyphTextMarkers(line);

			IGlyphTextMarkerHandlerContext? glyphTextMarkerHandlerContext = null;
			foreach (var marker in markers) {
				if (marker.Handler.MouseProcessor is null)
					continue;
				if (glyphTextMarkerHandlerContext is null)
					glyphTextMarkerHandlerContext = new GlyphTextMarkerHandlerContext(wpfTextViewHost, margin, line, this);
				marker.Handler.MouseProcessor.OnMouseLeftButtonUp(glyphTextMarkerHandlerContext, marker, e);
				if (e.Handled)
					return;
			}

			if (glyphTextMarkerMouseProcessors.Length != 0) {
				var context = new GlyphTextMarkerMouseProcessorContext(wpfTextViewHost, margin, line, markers, this);
				foreach (var processor in glyphTextMarkerMouseProcessors) {
					processor.OnMouseLeftButtonUp(context, e);
					if (e.Handled)
						return;
				}
			}
		}

		public override void PostprocessMouseRightButtonDown(MouseButtonEventArgs e) {
			CloseToolTip();
			var line = GetLine(e);
			if (line is null)
				return;

			var markers = glyphTextViewMarkerService.GetSortedGlyphTextMarkers(line);

			IGlyphTextMarkerHandlerContext? glyphTextMarkerHandlerContext = null;
			foreach (var marker in markers) {
				if (marker.Handler.MouseProcessor is null)
					continue;
				if (glyphTextMarkerHandlerContext is null)
					glyphTextMarkerHandlerContext = new GlyphTextMarkerHandlerContext(wpfTextViewHost, margin, line, this);
				marker.Handler.MouseProcessor.OnMouseRightButtonDown(glyphTextMarkerHandlerContext, marker, e);
				if (e.Handled)
					return;
			}

			if (glyphTextMarkerMouseProcessors.Length != 0) {
				var context = new GlyphTextMarkerMouseProcessorContext(wpfTextViewHost, margin, line, markers, this);
				foreach (var processor in glyphTextMarkerMouseProcessors) {
					processor.OnMouseRightButtonDown(context, e);
					if (e.Handled)
						return;
				}
			}
		}

		public override void PostprocessMouseRightButtonUp(MouseButtonEventArgs e) {
			CloseToolTip();
			var line = GetLine(e);
			if (line is null)
				return;

			var markers = glyphTextViewMarkerService.GetSortedGlyphTextMarkers(line);

			IGlyphTextMarkerHandlerContext? glyphTextMarkerHandlerContext = null;
			foreach (var marker in markers) {
				if (marker.Handler.MouseProcessor is null)
					continue;
				if (glyphTextMarkerHandlerContext is null)
					glyphTextMarkerHandlerContext = new GlyphTextMarkerHandlerContext(wpfTextViewHost, margin, line, this);
				marker.Handler.MouseProcessor.OnMouseRightButtonUp(glyphTextMarkerHandlerContext, marker, e);
				if (e.Handled)
					return;
			}

			if (glyphTextMarkerMouseProcessors.Length != 0) {
				var context = new GlyphTextMarkerMouseProcessorContext(wpfTextViewHost, margin, line, markers, this);
				foreach (var processor in glyphTextMarkerMouseProcessors) {
					processor.OnMouseRightButtonUp(context, e);
					if (e.Handled)
						return;
				}
			}
		}

		public override void PostprocessMouseMove(MouseEventArgs e) {
			UpdateLine(e);
			var line = GetLine(e);
			if (line is null)
				return;

			var markers = glyphTextViewMarkerService.GetSortedGlyphTextMarkers(line);

			IGlyphTextMarkerHandlerContext? glyphTextMarkerHandlerContext = null;
			foreach (var marker in markers) {
				if (marker.Handler.MouseProcessor is null)
					continue;
				if (glyphTextMarkerHandlerContext is null)
					glyphTextMarkerHandlerContext = new GlyphTextMarkerHandlerContext(wpfTextViewHost, margin, line, this);
				marker.Handler.MouseProcessor.OnMouseMove(glyphTextMarkerHandlerContext, marker, e);
				if (e.Handled)
					return;
			}

			if (glyphTextMarkerMouseProcessors.Length != 0) {
				var context = new GlyphTextMarkerMouseProcessorContext(wpfTextViewHost, margin, line, markers, this);
				foreach (var processor in glyphTextMarkerMouseProcessors) {
					processor.OnMouseMove(context, e);
					if (e.Handled)
						return;
				}
			}
		}

		public override void PostprocessMouseEnter(MouseEventArgs e) {
			if (glyphTextMarkerMouseProcessors.Length != 0) {
				var line = GetLine(e);
				if (line is null)
					return;

				var markers = glyphTextViewMarkerService.GetSortedGlyphTextMarkers(line);
				var context = new GlyphTextMarkerMouseProcessorContext(wpfTextViewHost, margin, line, markers, this);
				foreach (var processor in glyphTextMarkerMouseProcessors) {
					processor.OnMouseEnter(context, e);
					if (e.Handled)
						return;
				}
			}
		}

		public override void PostprocessMouseLeave(MouseEventArgs e) {
			CloseToolTip();
			if (glyphTextMarkerMouseProcessors.Length != 0) {
				var line = GetLine(e);
				if (line is null)
					return;

				var markers = glyphTextViewMarkerService.GetSortedGlyphTextMarkers(line);
				var context = new GlyphTextMarkerMouseProcessorContext(wpfTextViewHost, margin, line, markers, this);
				foreach (var processor in glyphTextMarkerMouseProcessors) {
					processor.OnMouseLeave(context, e);
					if (e.Handled)
						return;
				}
			}
		}

		void UpdateToolTipLine() => UpdateLine(new MouseEventArgs(Mouse.PrimaryDevice, 0));
		void UpdateLine(MouseEventArgs e) {
			var line = GetLine(e);
			if (line is null) {
				CloseToolTip();
				return;
			}

			if (toolTipLine != line) {
				CloseToolTip();
				toolTipLine = line;
				toolTipDispatcherTimer.Interval = TimeSpan.FromMilliseconds(TOOLTIP_WAIT_MILLISECONDS);
				toolTipDispatcherTimer.Start();
			}
		}

		void ClosePopup() {
			popup.IsOpen = false;
			popup.Visibility = Visibility.Collapsed;
			popup.Child = null;
			popupMarker = null;
			popupTopViewLinePosition = -1;
		}

		void CloseToolTip(bool clearLine = true) {
			if (toolTip is not null)
				toolTip.IsOpen = false;
			toolTip = null;
			toolTipMarker = null;
			if (clearLine)
				toolTipLine = null;
			toolTipDispatcherTimer.Stop();
		}
		readonly Popup popup;
		IGlyphTextMarker? popupMarker;
		int popupTopViewLinePosition;
		ToolTip? toolTip;
		IGlyphTextMarker? toolTipMarker;
		IWpfTextViewLine? toolTipLine;
		readonly DispatcherTimer toolTipDispatcherTimer;
		const int TOOLTIP_WAIT_MILLISECONDS = 150;

		void ToolTipDispatcherTimer_Tick(object? sender, EventArgs e) {
			if (wpfTextViewHost.IsClosed || toolTipLine is null || toolTipDispatcherTimer != sender) {
				CloseToolTip();
				return;
			}
			CloseToolTip(false);
			Debug2.Assert(toolTipLine is not null);

			UpdateToolTipContent(toolTipLine);
			UpdatePopupContent(toolTipLine);
		}

		void UpdateToolTipContent(IWpfTextViewLine line) {
			IGlyphTextMarkerHandlerContext? glyphTextMarkerHandlerContext = null;
			foreach (var marker in glyphTextViewMarkerService.GetSortedGlyphTextMarkers(line)) {
				if (glyphTextMarkerHandlerContext is null)
					glyphTextMarkerHandlerContext = new GlyphTextMarkerHandlerContext(wpfTextViewHost, margin, line, this);
				var toolTipInfo = marker.Handler.GetToolTipContent(glyphTextMarkerHandlerContext, marker);
				if (toolTipInfo is not null) {
					Debug2.Assert(toolTip is null);
					toolTipMarker = marker;
					toolTip = new ToolTip();
					PopupHelper.SetScaleTransform(wpfTextViewHost.TextView, toolTip);

					if (toolTipInfo.Content is string toolTipContentString) {
						toolTip.Content = new TextBlock {
							Text = toolTipContentString,
							TextWrapping = TextWrapping.Wrap,
						};
					}
					else
						toolTip.Content = toolTipInfo.Content;

					if (toolTipInfo.Style is Style toolTipStyle)
						toolTip.Style = toolTipStyle;
					else
						toolTip.SetResourceReference(FrameworkElement.StyleProperty, toolTipInfo.Style ?? "GlyphTextMarkerToolTipStyle");

					toolTip.Placement = PlacementMode.Relative;
					toolTip.PlacementTarget = margin.VisualElement;
					toolTip.HorizontalOffset = 0;
					toolTip.VerticalOffset = toolTipLine!.TextBottom - wpfTextViewHost.TextView.ViewportTop + 1;
					toolTip.IsOpen = true;
					return;
				}
			}
		}

		void UpdatePopupContent(IWpfTextViewLine line) {
			IGlyphTextMarkerHandlerContext? glyphTextMarkerHandlerContext = null;
			foreach (var marker in glyphTextViewMarkerService.GetSortedGlyphTextMarkers(line)) {
				if (glyphTextMarkerHandlerContext is null)
					glyphTextMarkerHandlerContext = new GlyphTextMarkerHandlerContext(wpfTextViewHost, margin, line, this);
				var popupContent = marker.Handler.GetPopupContent(glyphTextMarkerHandlerContext, marker);
				if (popupContent is not null) {
					AddPopupContent(line, marker, popupContent);
					return;
				}
			}
		}

		void AddPopupContent(IWpfTextViewLine line, IGlyphTextMarker marker, FrameworkElement popupContent) {
			// We must close it or it refuses to use the new scale transform
			popup.IsOpen = false;
			popupTopViewLinePosition = wpfTextViewHost.TextView.TextViewLines.FirstVisibleLine.Start.Position;
			popupMarker = marker;
			popup.Child = popupContent;
			popup.Placement = PlacementMode.Relative;
			popup.PlacementTarget = margin.VisualElement;
			popup.HorizontalOffset = margin.VisualElement.Width - 1;
			var popupContentHeight = popupContent.Height;
			Debug.Assert(!double.IsNaN(popupContentHeight), "You must initialize the Height property of the popup content!");
			if (double.IsNaN(popupContentHeight))
				popupContentHeight = 0;
			popup.VerticalOffset = line.TextTop - wpfTextViewHost.TextView.ViewportTop + 1 - popupContentHeight;
			popup.Visibility = Visibility.Visible;
			PopupHelper.SetScaleTransform(wpfTextViewHost.TextView, popup);
			popup.IsOpen = true;
		}

		void TextView_LayoutChanged(object? sender, TextViewLayoutChangedEventArgs e) {
			bool refresh = toolTipLine is not null || popup.IsOpen;
			if (popup.IsOpen) {
				if (e.OldSnapshot != e.NewSnapshot)
					ClosePopup();
				if (e.VerticalTranslation)
					ClosePopup();
				if (popupTopViewLinePosition != wpfTextViewHost.TextView.TextViewLines.FirstVisibleLine.Start.Position)
					ClosePopup();
			}
			if (refresh)
				UpdateToolTipLine();
		}

		void IGlyphTextMarkerListener.OnAdded(IEnumerable<IGlyphTextMarkerImpl> markers) { }

		void IGlyphTextMarkerListener.OnRemoved(IEnumerable<IGlyphTextMarkerImpl> markers) {
			bool refresh = false;
			bool closePopup = false;
			bool closeToolTip = false;
			foreach (var marker in markers) {
				if (popupMarker == marker) {
					closePopup = true;
					refresh = true;
				}
				if (toolTipMarker == marker) {
					closeToolTip = true;
					refresh = true;
				}
			}
			if (closePopup)
				ClosePopup();
			if (closeToolTip)
				CloseToolTip();
			if (refresh)
				UpdateToolTipLine();
		}

		// Clean up once it's closed
		void Popup_Closed(object? sender, EventArgs e) => ClosePopup();

		void TextView_Closed(object? sender, EventArgs e) {
			glyphTextViewMarkerService.RemoveGlyphTextMarkerListener(this);
			CloseToolTip();
			ClosePopup();
			wpfTextViewHost.TextView.Closed -= TextView_Closed;
			wpfTextViewHost.TextView.LayoutChanged -= TextView_LayoutChanged;
			toolTipDispatcherTimer.Tick -= ToolTipDispatcherTimer_Tick;
			popup.Closed -= Popup_Closed;
		}
	}
}
