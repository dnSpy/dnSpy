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
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class AdornmentLayer : Canvas, IAdornmentLayer {
		public IWpfTextView TextView { get; }
		public IAdornmentLayerDefinitionMetadata LayerMetadata { get; }

		public bool IsEmpty => adornmentLayerElements.Count == 0;
		public ReadOnlyCollection<IAdornmentLayerElement> Elements => new ReadOnlyCollection<IAdornmentLayerElement>(adornmentLayerElements.ToArray());
		readonly List<AdornmentLayerElement> adornmentLayerElements;

		public AdornmentLayer(IWpfTextView textView, IAdornmentLayerDefinitionMetadata mdLayer) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (mdLayer == null)
				throw new ArgumentNullException(nameof(mdLayer));
			TextView = textView;
			LayerMetadata = mdLayer;
			this.adornmentLayerElements = new List<AdornmentLayerElement>();
		}

		public bool AddAdornment(SnapshotSpan visualSpan, object tag, UIElement adornment) =>
			AddAdornment(AdornmentPositioningBehavior.TextRelative, visualSpan, tag, adornment, null);
		public bool AddAdornment(AdornmentPositioningBehavior behavior, SnapshotSpan? visualSpan, object tag, UIElement adornment, AdornmentRemovedCallback removedCallback) {
			if (adornment == null)
				throw new ArgumentNullException(nameof(adornment));
			if (visualSpan == null && behavior == AdornmentPositioningBehavior.TextRelative)
				throw new ArgumentNullException(nameof(visualSpan));
			bool canAdd = visualSpan == null || TextView.TextViewLines.IntersectsBufferSpan(visualSpan.Value);
			if (canAdd) {
				var layerElem = new AdornmentLayerElement(behavior, visualSpan, tag, adornment, removedCallback);
				Children.Add(layerElem.Adornment);
				adornmentLayerElements.Add(layerElem);
			}
			return canAdd;
		}

		public void RemoveAdornment(UIElement adornment) {
			if (adornment == null)
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

		public void RemoveAdornmentsByTag(object tag) {
			if (tag == null)
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
			if (visualSpan.Snapshot == null)
				throw new ArgumentException();
			for (int i = adornmentLayerElements.Count - 1; i >= 0; i--) {
				var elem = adornmentLayerElements[i];
				if (elem.VisualSpan != null && visualSpan.OverlapsWith(elem.VisualSpan.Value)) {
					adornmentLayerElements.RemoveAt(i);
					Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
				}
			}
		}

		public void RemoveAllAdornments() {
			foreach (var elem in adornmentLayerElements)
				elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
			adornmentLayerElements.Clear();
			Children.Clear();
		}

		public void RemoveMatchingAdornments(Predicate<IAdornmentLayerElement> match) {
			if (match == null)
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
			if (visualSpan.Snapshot == null)
				throw new ArgumentException();
			if (match == null)
				throw new ArgumentNullException(nameof(match));
			for (int i = adornmentLayerElements.Count - 1; i >= 0; i--) {
				var elem = adornmentLayerElements[i];
				if (elem.VisualSpan != null && visualSpan.OverlapsWith(elem.VisualSpan.Value) && match(elem)) {
					adornmentLayerElements.RemoveAt(i);
					Children.RemoveAt(i);
					elem.RemovedCallback?.Invoke(elem.Tag, elem.Adornment);
				}
			}
		}

		public override string ToString() => $"Layer {LayerMetadata.DisplayName} {LayerMetadata.Guid}";
	}
}
