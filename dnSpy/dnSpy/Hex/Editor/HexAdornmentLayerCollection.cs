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
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Hex.MEF;

namespace dnSpy.Hex.Editor {
	sealed class HexAdornmentLayerCollection : Canvas {
		readonly WpfHexView wpfHexView;
		readonly List<HexAdornmentLayerImpl> adornmentLayers;
		readonly HexLayerKind layerKind;

		public HexAdornmentLayerCollection(WpfHexView wpfHexView, HexLayerKind layerKind) {
			this.wpfHexView = wpfHexView ?? throw new ArgumentNullException(nameof(wpfHexView));
			this.layerKind = layerKind;
			adornmentLayers = new List<HexAdornmentLayerImpl>();
			if (layerKind != HexLayerKind.Normal)
				ClipToBounds = true;
			wpfHexView.Closed += WpfHexView_Closed;
			wpfHexView.LayoutChanged += WpfHexView_LayoutChanged;
		}

		public HexAdornmentLayer GetAdornmentLayer(MetadataAndOrder<IAdornmentLayersMetadata> info) {
			var layer = adornmentLayers.FirstOrDefault(a => a.Info.Metadata == info.Metadata);
			if (layer is null)
				layer = Create(info);
			return layer;
		}

		HexAdornmentLayerImpl Create(MetadataAndOrder<IAdornmentLayersMetadata> info) {
			var layer = new HexAdornmentLayerImpl(wpfHexView, layerKind, info);
			int index = GetInsertIndex(info);
			adornmentLayers.Insert(index, layer);
			Children.Insert(index, layer.VisualElement);
			return layer;
		}

		int GetInsertIndex(MetadataAndOrder<IAdornmentLayersMetadata> info) {
			for (int i = 0; i < adornmentLayers.Count; i++) {
				if (info.Order < adornmentLayers[i].Info.Order)
					return i;
			}
			return adornmentLayers.Count;
		}

		void WpfHexView_LayoutChanged(object? sender, HexViewLayoutChangedEventArgs e) {
			if (Width != wpfHexView.VisualElement.ActualWidth || Height != wpfHexView.VisualElement.ActualHeight) {
				Width = wpfHexView.VisualElement.ActualWidth;
				Height = wpfHexView.VisualElement.ActualHeight;
				if (layerKind == HexLayerKind.Normal) {
					// Needed when HW acceleration isn't enabled (virtual machine or remote desktop).
					// https://msdn.microsoft.com/en-us/library/system.windows.media.visual.visualscrollableareaclip(v=vs.110).aspx
					// It's ignored if HW acceleration is enabled.
					// This will reduce the number of bytes sent over the network and should speed up the display
					// if it's a slow connection.
					VisualScrollableAreaClip = new Rect(0, 0, Width, Height);
				}
			}

			if (layerKind == HexLayerKind.Normal) {
				foreach (var layer in adornmentLayers)
					layer.OnLayoutChanged(e);
			}
		}

		void WpfHexView_Closed(object? sender, EventArgs e) {
			wpfHexView.Closed -= WpfHexView_Closed;
			wpfHexView.LayoutChanged -= WpfHexView_LayoutChanged;
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
