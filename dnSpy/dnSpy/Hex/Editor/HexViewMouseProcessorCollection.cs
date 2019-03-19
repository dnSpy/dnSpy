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
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Operations;
using dnSpy.Hex.MEF;

namespace dnSpy.Hex.Editor {
	sealed class HexViewMouseProcessorCollection {
		readonly WpfHexView wpfHexView;
		readonly WpfHexViewImpl wpfHexViewImpl;
		readonly HexEditorOperationsFactoryService editorOperationsFactoryService;
		readonly Lazy<HexMouseProcessorProvider, IOrderableTextViewRoleMetadata>[] mouseProcessorProviders;
		readonly Func<MouseEventArgs, bool> allowEventDelegate;
		HexMouseProcessorCollection mouseProcessorCollection;

		public HexViewMouseProcessorCollection(WpfHexView wpfHexView, HexEditorOperationsFactoryService editorOperationsFactoryService, Lazy<HexMouseProcessorProvider, IOrderableTextViewRoleMetadata>[] mouseProcessorProviders) {
			this.wpfHexView = wpfHexView;
			wpfHexViewImpl = wpfHexView as WpfHexViewImpl;
			this.editorOperationsFactoryService = editorOperationsFactoryService;
			this.mouseProcessorProviders = mouseProcessorProviders;
			allowEventDelegate = AllowMouseEvent;
			wpfHexView.Closed += WpfHexView_Closed;
			Reinitialize();
		}

		bool AllowMouseEvent(MouseEventArgs e) {
			if (wpfHexViewImpl != null && wpfHexViewImpl.IsMouseOverOverlayLayerElement(e)) {
				e.Handled = true;
				return false;
			}
			return true;
		}

		void Reinitialize() {
			mouseProcessorCollection?.Dispose();
			var list = new List<HexMouseProcessor>();
			foreach (var provider in mouseProcessorProviders) {
				if (!wpfHexView.Roles.ContainsAny(provider.Metadata.TextViewRoles))
					continue;
				var mouseProcessor = provider.Value.GetAssociatedProcessor(wpfHexView);
				if (mouseProcessor != null)
					list.Add(mouseProcessor);
			}
			UIElement manipulationElem = null;//TODO:
			mouseProcessorCollection = new HexMouseProcessorCollection(wpfHexView.VisualElement, manipulationElem, new DefaultHexViewMouseProcessor(wpfHexView, editorOperationsFactoryService), list.ToArray(), allowEventDelegate);
		}

		void WpfHexView_Closed(object sender, EventArgs e) {
			wpfHexView.Closed -= WpfHexView_Closed;
			mouseProcessorCollection.Dispose();
		}
	}
}
