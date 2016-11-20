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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Hex.MEF;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.Editor {
	sealed class HexAdornmentLayerImpl : HexAdornmentLayer {
		public override FrameworkElement VisualElement => canvas;
		public override WpfHexView HexView { get; }
		public MetadataAndOrder<IAdornmentLayersMetadata> Info { get; }
		public override bool IsEmpty => adornmentLayerElements.Count == 0;
		public override ReadOnlyCollection<HexAdornmentLayerElement> Elements => new ReadOnlyCollection<HexAdornmentLayerElement>(adornmentLayerElements.ToArray());
		readonly List<HexAdornmentLayerElementImpl> adornmentLayerElements;
		readonly HexLayerKind layerKind;
		readonly Canvas canvas;

		public HexAdornmentLayerImpl(WpfHexView hexView, HexLayerKind layerKind, MetadataAndOrder<IAdornmentLayersMetadata> info) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			canvas = new Canvas();
			HexView = hexView;
			this.layerKind = layerKind;
			Info = info;
			this.adornmentLayerElements = new List<HexAdornmentLayerElementImpl>();
		}

		public override bool AddAdornment(VSTE.AdornmentPositioningBehavior behavior, HexBufferSpan? visualSpan, object tag, UIElement adornment, VSTE.AdornmentRemovedCallback removedCallback) {
			if (adornment == null)
				throw new ArgumentNullException(nameof(adornment));
			if (visualSpan != null && visualSpan.Value.IsDefault)
				throw new ArgumentException();
			if (visualSpan == null && behavior == VSTE.AdornmentPositioningBehavior.TextRelative)
				throw new ArgumentNullException(nameof(visualSpan));
			if ((uint)behavior > (uint)VSTE.AdornmentPositioningBehavior.TextRelative)
				throw new ArgumentOutOfRangeException(nameof(behavior));
			if (layerKind != HexLayerKind.Normal) {
				if (behavior != VSTE.AdornmentPositioningBehavior.OwnerControlled)
					throw new ArgumentOutOfRangeException(nameof(behavior), "Special layers must use AdornmentPositioningBehavior.OwnerControlled");
				if (visualSpan != null)
					throw new ArgumentOutOfRangeException(nameof(visualSpan), "Special layers must use a null visual span");
			}
			bool canAdd = visualSpan == null || HexView.HexViewLines.IntersectsBufferSpan(visualSpan.Value);
			if (canAdd) {
				var layerElem = new HexAdornmentLayerElementImpl(behavior, visualSpan, tag, adornment, removedCallback);
				canvas.Children.Add(layerElem.Adornment);
				adornmentLayerElements.Add(layerElem);
			}
			return canAdd;
		}

		public override void RemoveAdornment(UIElement adornment) {
			if (adornment == null)
				throw new ArgumentNullException(nameof(adornment));
			for (int i = 0; i < adornmentLayerElements.Count; i++) {
				var elem = adornmentLayerElements[i];
				if (elem.Adornment == adornment) {
					adornmentLayerElements.RemoveAt(i);
					canvas.Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
					break;
				}
			}
		}

		internal bool IsMouseOverOverlayLayerElement(MouseEventArgs e) {
			foreach (UIElement elem in canvas.Children) {
				if (elem.IsMouseOver)
					return true;
			}
			return false;
		}

		public override void RemoveAdornmentsByTag(object tag) {
			if (tag == null)
				throw new ArgumentNullException(nameof(tag));
			for (int i = adornmentLayerElements.Count - 1; i >= 0; i--) {
				var elem = adornmentLayerElements[i];
				if (tag.Equals(elem.Tag)) {
					adornmentLayerElements.RemoveAt(i);
					canvas.Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
				}
			}
		}

		public override void RemoveMatchingAdornments(Predicate<HexAdornmentLayerElement> match) {
			if (match == null)
				throw new ArgumentNullException(nameof(match));
			for (int i = adornmentLayerElements.Count - 1; i >= 0; i--) {
				var elem = adornmentLayerElements[i];
				if (match(elem)) {
					adornmentLayerElements.RemoveAt(i);
					canvas.Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
				}
			}
		}

		public override void RemoveMatchingAdornments(HexBufferSpan visualSpan, Predicate<HexAdornmentLayerElement> match) {
			if (visualSpan.IsDefault)
				throw new ArgumentException();
			if (match == null)
				throw new ArgumentNullException(nameof(match));
			for (int i = adornmentLayerElements.Count - 1; i >= 0; i--) {
				var elem = adornmentLayerElements[i];
				if (elem.VisualSpan != null && visualSpan.OverlapsWith(GetOverlapsWithSpan(elem.VisualSpan.Value)) && match(elem)) {
					adornmentLayerElements.RemoveAt(i);
					canvas.Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
				}
			}
		}

		static HexBufferSpan GetOverlapsWithSpan(HexBufferSpan span) {
			if (span.Length != 0)
				return span;
			if (span.Start.Position == HexPosition.MaxEndPosition)
				return span;
			return new HexBufferSpan(span.Start, 1);
		}

		public override void RemoveAllAdornments() {
			foreach (var elem in adornmentLayerElements)
				elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
			if (adornmentLayerElements.Count != 0) {
				adornmentLayerElements.Clear();
				canvas.Children.Clear();
			}
		}

		internal void OnLayoutChanged(HexViewLayoutChangedEventArgs e) {
			for (int i = adornmentLayerElements.Count - 1; i >= 0; i--) {
				var elem = adornmentLayerElements[i];

				// All adornments that exist in spans that have been removed or in reformatted lines are always removed.
				if (elem.VisualSpan != null &&
					(!HexView.HexViewLines.IntersectsBufferSpan(elem.VisualSpan.Value) || GetLine(e.NewOrReformattedLines, GetOverlapsWithSpan(elem.VisualSpan.Value)) != null)) {
					adornmentLayerElements.RemoveAt(i);
					canvas.Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
					continue;
				}

				switch (elem.Behavior) {
				case VSTE.AdornmentPositioningBehavior.OwnerControlled:
					break;

				case VSTE.AdornmentPositioningBehavior.ViewportRelative:
					Canvas.SetTop(elem.Adornment, ToDefault(Canvas.GetTop(elem.Adornment), 0) + e.NewViewState.ViewportTop - e.OldViewState.ViewportTop);
					Canvas.SetLeft(elem.Adornment, ToDefault(Canvas.GetLeft(elem.Adornment), 0) + e.NewViewState.ViewportLeft - e.OldViewState.ViewportLeft);
					break;

				case VSTE.AdornmentPositioningBehavior.TextRelative:
					Debug.Assert(elem.VisualSpan != null);
					var translatedLine = GetLine(e.TranslatedLines, GetOverlapsWithSpan(elem.VisualSpan.Value));
					if (translatedLine != null) {
						// Only y is updated, x is owner controlled
						Canvas.SetTop(elem.Adornment, ToDefault(Canvas.GetTop(elem.Adornment), 0) + translatedLine.DeltaY);
					}
					break;

				default:
					throw new InvalidOperationException();
				}
			}
		}

		// Canvas.Top/Left default to NaN, not 0
		static double ToDefault(double value, double defaultValue) => double.IsNaN(value) ? defaultValue : value;

		static HexViewLine GetLine(IList<HexViewLine> lines, HexBufferSpan span) {
			foreach (var line in lines) {
				if (line.BufferSpan.OverlapsWith(span))
					return line;
				if (span.End == line.BufferSpan.End && line.IsLastDocumentLine())
					return line;
			}
			return null;
		}

		public override string ToString() => $"Layer {Info.Metadata.Name}";
	}
}
