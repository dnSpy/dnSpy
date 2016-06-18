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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class AdornmentLayerCollection : Canvas {
		readonly IWpfTextView wpfTextView;
		readonly List<AdornmentLayer> adornmentLayers;

		public AdornmentLayerCollection(IWpfTextView wpfTextView) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			this.wpfTextView = wpfTextView;
			this.adornmentLayers = new List<AdornmentLayer>();
		}

		public IAdornmentLayer GetAdornmentLayer(IAdornmentLayerDefinitionMetadata mdLayer) {
			if (mdLayer == null)
				throw new ArgumentNullException(nameof(mdLayer));
			var layer = adornmentLayers.FirstOrDefault(a => a.LayerMetadata == mdLayer);
			if (layer == null)
				layer = Create(mdLayer);
			return layer;
		}

		AdornmentLayer Create(IAdornmentLayerDefinitionMetadata mdLayer) {
			var layer = new AdornmentLayer(wpfTextView, mdLayer);
			int index = GetInsertIndex(mdLayer);
			adornmentLayers.Insert(index, layer);
			Children.Insert(index, layer);
			layer.Width = ActualWidth;
			layer.Height = ActualHeight;
			return layer;
		}

		int GetInsertIndex(IAdornmentLayerDefinitionMetadata mdLayer) {
			for (int i = 0; i < adornmentLayers.Count; i++) {
				if (mdLayer.Order < adornmentLayers[i].LayerMetadata.Order)
					return i;
			}
			return adornmentLayers.Count;
		}

		public void OnParentSizeChanged(Size newSize) {
			Width = newSize.Width;
			Height = newSize.Height;
			VisualScrollableAreaClip = new Rect(0, 0, newSize.Width, newSize.Height);
		}
	}
}
