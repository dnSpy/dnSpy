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
using System.Windows;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class TextViewMouseProcessorCollection {
		readonly IWpfTextView wpfTextView;
		readonly Lazy<IMouseProcessorProvider, IMouseProcessorProviderMetadata>[] mouseProcessorProviders;
		MouseProcessorCollection mouseProcessorCollection;

		public TextViewMouseProcessorCollection(IWpfTextView wpfTextView, Lazy<IMouseProcessorProvider, IMouseProcessorProviderMetadata>[] mouseProcessorProviders) {
			this.wpfTextView = wpfTextView;
			this.mouseProcessorProviders = mouseProcessorProviders;
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.TextDataModel.ContentTypeChanged += TextDataModel_ContentTypeChanged;
			Reinitialize();
		}

		void Reinitialize() {
			mouseProcessorCollection?.Dispose();
			var list = new List<IMouseProcessor>();
			foreach (var provider in mouseProcessorProviders) {
				var mouseProcessor = provider.Value.GetAssociatedProcessor(wpfTextView);
				if (mouseProcessor != null)
					list.Add(mouseProcessor);
			}
			UIElement manipulationElem = null;//TODO:
			mouseProcessorCollection = new MouseProcessorCollection(wpfTextView.VisualElement, manipulationElem, new DefaultTextViewMouseProcessor(wpfTextView), list.ToArray());
		}

		void TextDataModel_ContentTypeChanged(object sender, TextDataModelContentTypeChangedEventArgs e) => Reinitialize();

		void WpfTextView_Closed(object sender, EventArgs e) {
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.TextDataModel.ContentTypeChanged -= TextDataModel_ContentTypeChanged;
			mouseProcessorCollection.Dispose();
		}
	}
}
