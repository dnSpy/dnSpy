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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class AdornmentLayer : Canvas, IAdornmentLayer {
		public IWpfTextView TextView { get; }
		public MetadataAndOrder<IAdornmentLayersMetadata> Info { get; }
		public bool IsEmpty => adornmentLayerElements.Count == 0;
		public ReadOnlyCollection<IAdornmentLayerElement> Elements => new ReadOnlyCollection<IAdornmentLayerElement>(adornmentLayerElements.ToArray());
		readonly List<AdornmentLayerElement> adornmentLayerElements;
		readonly LayerKind layerKind;

		public AdornmentLayer(IWpfTextView textView, LayerKind layerKind, MetadataAndOrder<IAdornmentLayersMetadata> info) {
			TextView = textView ?? throw new ArgumentNullException(nameof(textView));
			this.layerKind = layerKind;
			Info = info;
			adornmentLayerElements = new List<AdornmentLayerElement>();
		}

		public bool AddAdornment(SnapshotSpan visualSpan, object tag, UIElement adornment) =>
			AddAdornment(AdornmentPositioningBehavior.TextRelative, visualSpan, tag, adornment, null);
		public bool AddAdornment(AdornmentPositioningBehavior behavior, SnapshotSpan? visualSpan, object tag, UIElement adornment, AdornmentRemovedCallback? removedCallback) {
			if (adornment is null)
				throw new ArgumentNullException(nameof(adornment));
			if (visualSpan is null && behavior == AdornmentPositioningBehavior.TextRelative)
				throw new ArgumentNullException(nameof(visualSpan));
			if ((uint)behavior > (uint)AdornmentPositioningBehavior.TextRelative)
				throw new ArgumentOutOfRangeException(nameof(behavior));
			if (layerKind != LayerKind.Normal) {
				if (behavior != AdornmentPositioningBehavior.OwnerControlled)
					throw new ArgumentOutOfRangeException(nameof(behavior), "Special layers must use AdornmentPositioningBehavior.OwnerControlled");
				if (visualSpan is not null)
					throw new ArgumentOutOfRangeException(nameof(visualSpan), "Special layers must use a null visual span");
			}
			bool canAdd = visualSpan is null || TextView.TextViewLines.IntersectsBufferSpan(visualSpan.Value);
			if (canAdd) {
				var layerElem = new AdornmentLayerElement(behavior, visualSpan, tag, adornment, removedCallback);
				layerElem.OnLayoutChanged(TextView.TextSnapshot);
				Children.Add(layerElem.Adornment);
				adornmentLayerElements.Add(layerElem);
			}
			return canAdd;
		}

		public void RemoveAdornment(UIElement adornment) {
			if (adornment is null)
				throw new ArgumentNullException(nameof(adornment));
			for (int i = 0; i < adornmentLayerElements.Count; i++) {
				var elem = adornmentLayerElements[i];
				if (elem.Adornment == adornment) {
					adornmentLayerElements.RemoveAt(i);
					Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
					break;
				}
			}
		}

		internal bool IsMouseOverOverlayLayerElement(MouseEventArgs e) {
			foreach (UIElement? elem in Children) {
				Debug2.Assert(elem is not null);
				if (elem.IsMouseOver)
					return true;
			}
			return false;
		}

		public void RemoveAdornmentsByTag(object? tag) {
			if (tag is null)
				throw new ArgumentNullException(nameof(tag));
			for (int i = adornmentLayerElements.Count - 1; i >= 0; i--) {
				var elem = adornmentLayerElements[i];
				if (tag.Equals(elem.Tag)) {
					adornmentLayerElements.RemoveAt(i);
					Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
				}
			}
		}

		public void RemoveAdornmentsByVisualSpan(SnapshotSpan visualSpan) {
			if (visualSpan.Snapshot is null)
				throw new ArgumentException();
			for (int i = adornmentLayerElements.Count - 1; i >= 0; i--) {
				var elem = adornmentLayerElements[i];
				if (elem.VisualSpan is not null && visualSpan.OverlapsWith(GetOverlapsWithSpan(elem.VisualSpan.Value))) {
					adornmentLayerElements.RemoveAt(i);
					Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
				}
			}
		}

		public void RemoveAllAdornments() {
			foreach (var elem in adornmentLayerElements)
				elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
			if (adornmentLayerElements.Count != 0) {
				adornmentLayerElements.Clear();
				Children.Clear();
			}
		}

		public void RemoveMatchingAdornments(Predicate<IAdornmentLayerElement> match) {
			if (match is null)
				throw new ArgumentNullException(nameof(match));
			for (int i = adornmentLayerElements.Count - 1; i >= 0; i--) {
				var elem = adornmentLayerElements[i];
				if (match(elem)) {
					adornmentLayerElements.RemoveAt(i);
					Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
				}
			}
		}

		public void RemoveMatchingAdornments(SnapshotSpan visualSpan, Predicate<IAdornmentLayerElement> match) {
			if (visualSpan.Snapshot is null)
				throw new ArgumentException();
			if (match is null)
				throw new ArgumentNullException(nameof(match));
			for (int i = adornmentLayerElements.Count - 1; i >= 0; i--) {
				var elem = adornmentLayerElements[i];
				if (elem.VisualSpan is not null && visualSpan.OverlapsWith(GetOverlapsWithSpan(elem.VisualSpan.Value)) && match(elem)) {
					adornmentLayerElements.RemoveAt(i);
					Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
				}
			}
		}

		static SnapshotSpan GetOverlapsWithSpan(SnapshotSpan span) {
			if (span.Length != 0)
				return span;
			if (span.Start.Position == span.Snapshot.Length)
				return span;
			return new SnapshotSpan(span.Start, span.Start + 1);
		}

		internal void OnLayoutChanged(TextViewLayoutChangedEventArgs e) {
			for (int i = adornmentLayerElements.Count - 1; i >= 0; i--) {
				var elem = adornmentLayerElements[i];
				elem.OnLayoutChanged(e.NewSnapshot);

				// All adornments that exist in spans that have been removed or in reformatted lines are always removed.
				if (elem.VisualSpan is not null &&
					(!TextView.TextViewLines.IntersectsBufferSpan(elem.VisualSpan.Value) || GetLine(e.NewOrReformattedLines, GetOverlapsWithSpan(elem.VisualSpan.Value)) is not null)) {
					adornmentLayerElements.RemoveAt(i);
					Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
					continue;
				}

				switch (elem.Behavior) {
				case AdornmentPositioningBehavior.OwnerControlled:
					break;

				case AdornmentPositioningBehavior.ViewportRelative:
					SetTop(elem.Adornment, ToDefault(GetTop(elem.Adornment), 0) + e.NewViewState.ViewportTop - e.OldViewState.ViewportTop);
					SetLeft(elem.Adornment, ToDefault(GetLeft(elem.Adornment), 0) + e.NewViewState.ViewportLeft - e.OldViewState.ViewportLeft);
					break;

				case AdornmentPositioningBehavior.TextRelative:
					Debug2.Assert(elem.VisualSpan is not null);
					var translatedLine = GetLine(e.TranslatedLines, GetOverlapsWithSpan(elem.VisualSpan.Value));
					if (translatedLine is not null) {
						// Only y is updated, x is owner controlled
						SetTop(elem.Adornment, ToDefault(GetTop(elem.Adornment), 0) + translatedLine.DeltaY);
					}
					break;

				default:
					throw new InvalidOperationException();
				}
			}
		}

		// Canvas.Top/Left default to NaN, not 0
		static double ToDefault(double value, double defaultValue) => double.IsNaN(value) ? defaultValue : value;

		static ITextViewLine? GetLine(IList<ITextViewLine> lines, SnapshotSpan span) {
			foreach (var line in lines) {
				if (line.ExtentIncludingLineBreak.OverlapsWith(span))
					return line;
				if (span.End == line.End && line.IsLastDocumentLine())
					return line;
			}
			return null;
		}

		public override string ToString() => $"Layer {Info.Metadata.Name}";
	}
}
