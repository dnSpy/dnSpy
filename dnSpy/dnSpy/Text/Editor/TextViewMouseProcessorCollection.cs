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
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	sealed class TextViewMouseProcessorCollection {
		readonly IWpfTextView wpfTextView;
		readonly Lazy<IMouseProcessorProvider, IOrderableContentTypeAndTextViewRoleMetadata>[] mouseProcessorProviders;
		readonly IEditorOperationsFactoryService editorOperationsFactoryService;
		MouseProcessorCollection mouseProcessorCollection;

		public TextViewMouseProcessorCollection(IWpfTextView wpfTextView, Lazy<IMouseProcessorProvider, IOrderableContentTypeAndTextViewRoleMetadata>[] mouseProcessorProviders, IEditorOperationsFactoryService editorOperationsFactoryService) {
			this.wpfTextView = wpfTextView;
			this.mouseProcessorProviders = Orderer.Order(mouseProcessorProviders).ToArray();
			this.editorOperationsFactoryService = editorOperationsFactoryService;
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.TextDataModel.ContentTypeChanged += TextDataModel_ContentTypeChanged;
			Reinitialize();
		}

		void Reinitialize() {
			mouseProcessorCollection?.Dispose();
			var list = new List<IMouseProcessor>();
			foreach (var provider in mouseProcessorProviders) {
				if (!wpfTextView.Roles.ContainsAny(provider.Metadata.TextViewRoles))
					continue;
				if (!wpfTextView.TextDataModel.ContentType.ContainsAny(provider.Metadata.ContentTypes))
					continue;
				var mouseProcessor = provider.Value.GetAssociatedProcessor(wpfTextView);
				if (mouseProcessor != null)
					list.Add(mouseProcessor);
			}
			UIElement manipulationElem = null;//TODO:
			mouseProcessorCollection = new MouseProcessorCollection(wpfTextView.VisualElement, manipulationElem, new DefaultTextViewMouseProcessor(wpfTextView, editorOperationsFactoryService), list.ToArray());
		}

		void TextDataModel_ContentTypeChanged(object sender, TextDataModelContentTypeChangedEventArgs e) => Reinitialize();

		void WpfTextView_Closed(object sender, EventArgs e) {
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.TextDataModel.ContentTypeChanged -= TextDataModel_ContentTypeChanged;
			mouseProcessorCollection.Dispose();
		}
	}
}
