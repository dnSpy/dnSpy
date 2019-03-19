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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class AdornmentLayerCollection : Canvas {
		readonly IWpfTextView wpfTextView;
		readonly List<AdornmentLayer> adornmentLayers;
		readonly LayerKind layerKind;

		public AdornmentLayerCollection(IWpfTextView wpfTextView, LayerKind layerKind) {
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			this.layerKind = layerKind;
			adornmentLayers = new List<AdornmentLayer>();
			if (layerKind != LayerKind.Normal)
				ClipToBounds = true;
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
		}

		public IAdornmentLayer GetAdornmentLayer(MetadataAndOrder<IAdornmentLayersMetadata> info) {
			var layer = adornmentLayers.FirstOrDefault(a => a.Info.Metadata == info.Metadata);
			if (layer == null)
				layer = Create(info);
			return layer;
		}

		AdornmentLayer Create(MetadataAndOrder<IAdornmentLayersMetadata> info) {
			var layer = new AdornmentLayer(wpfTextView, layerKind, info);
			int index = GetInsertIndex(info);
			adornmentLayers.Insert(index, layer);
			Children.Insert(index, layer);
			return layer;
		}

		int GetInsertIndex(MetadataAndOrder<IAdornmentLayersMetadata> info) {
			for (int i = 0; i < adornmentLayers.Count; i++) {
				if (info.Order < adornmentLayers[i].Info.Order)
					return i;
			}
			return adornmentLayers.Count;
		}

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (Width != wpfTextView.VisualElement.ActualWidth || Height != wpfTextView.VisualElement.ActualHeight) {
				Width = wpfTextView.VisualElement.ActualWidth;
				Height = wpfTextView.VisualElement.ActualHeight;
				if (layerKind == LayerKind.Normal) {
					// Needed when HW acceleration isn't enabled (virtual machine or remote desktop).
					// https://msdn.microsoft.com/en-us/library/system.windows.media.visual.visualscrollableareaclip(v=vs.110).aspx
					// It's ignored if HW acceleration is enabled.
					// This will reduce the number of bytes sent over the network and should speed up the display
					// if it's a slow connection.
					VisualScrollableAreaClip = new Rect(0, 0, Width, Height);
				}
			}

			if (layerKind == LayerKind.Normal) {
				foreach (var layer in adornmentLayers)
					layer.OnLayoutChanged(e);
			}
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
		}

		internal bool IsMouseOverOverlayLayerElement(MouseEventArgs e) {
			foreach (var layer in adornmentLayers) {
				if (layer.IsMouseOverOverlayLayerElement(e))
					return true;
			}
			return false;
		}
	}
}
