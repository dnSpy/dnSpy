/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class KeyProcessorCollection {
		readonly IWpfTextView wpfTextView;
		readonly Lazy<IKeyProcessorProvider, IOrderableContentTypeAndTextViewRoleMetadata>[] keyProcessorProviders;
		KeyProcessor[] keyProcessors;

		public KeyProcessorCollection(IWpfTextView wpfTextView, Lazy<IKeyProcessorProvider, IOrderableContentTypeAndTextViewRoleMetadata>[] keyProcessorProviders) {
			this.wpfTextView = wpfTextView;
			this.keyProcessorProviders = keyProcessorProviders;
			keyProcessors = Array.Empty<KeyProcessor>();
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.TextDataModel.ContentTypeChanged += TextDataModel_ContentTypeChanged;
			wpfTextView.VisualElement.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(VisualElement_KeyDown), true);
			wpfTextView.VisualElement.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler(VisualElement_KeyUp), true);
			wpfTextView.VisualElement.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(VisualElement_PreviewKeyDown), true);
			wpfTextView.VisualElement.AddHandler(UIElement.PreviewKeyUpEvent, new KeyEventHandler(VisualElement_PreviewKeyUp), true);
			wpfTextView.VisualElement.AddHandler(UIElement.TextInputEvent, new TextCompositionEventHandler(VisualElement_TextInput), true);
			wpfTextView.VisualElement.AddHandler(UIElement.PreviewTextInputEvent, new TextCompositionEventHandler(VisualElement_PreviewTextInput), true);
			wpfTextView.VisualElement.AddHandler(TextCompositionManager.TextInputStartEvent, new TextCompositionEventHandler(VisualElement_TextInputStart), true);
			wpfTextView.VisualElement.AddHandler(TextCompositionManager.PreviewTextInputStartEvent, new TextCompositionEventHandler(VisualElement_PreviewTextInputStart), true);
			wpfTextView.VisualElement.AddHandler(TextCompositionManager.TextInputUpdateEvent, new TextCompositionEventHandler(VisualElement_TextInputUpdate), true);
			wpfTextView.VisualElement.AddHandler(TextCompositionManager.PreviewTextInputUpdateEvent, new TextCompositionEventHandler(VisualElement_PreviewTextInputUpdate), true);
			Reinitialize();
		}

		void VisualElement_KeyDown(object sender, KeyEventArgs e) {
			foreach (var keyProcessor in keyProcessors) {
				if (keyProcessor.IsInterestedInHandledEvents || !e.Handled)
					keyProcessor.KeyDown(e);
			}
		}

		void VisualElement_KeyUp(object sender, KeyEventArgs e) {
			var array = keyProcessors;
			foreach (var keyProcessor in array) {
				if (keyProcessor.IsInterestedInHandledEvents || !e.Handled)
					keyProcessor.KeyUp(e);
			}
		}

		void VisualElement_PreviewKeyDown(object sender, KeyEventArgs e) {
			var array = keyProcessors;
			foreach (var keyProcessor in array) {
				if (keyProcessor.IsInterestedInHandledEvents || !e.Handled)
					keyProcessor.PreviewKeyDown(e);
			}
		}

		void VisualElement_PreviewKeyUp(object sender, KeyEventArgs e) {
			var array = keyProcessors;
			foreach (var keyProcessor in array) {
				if (keyProcessor.IsInterestedInHandledEvents || !e.Handled)
					keyProcessor.PreviewKeyUp(e);
			}
		}

		void VisualElement_TextInput(object sender, TextCompositionEventArgs e) {
			var array = keyProcessors;
			foreach (var keyProcessor in array) {
				if (keyProcessor.IsInterestedInHandledEvents || !e.Handled)
					keyProcessor.TextInput(e);
			}
		}

		void VisualElement_PreviewTextInput(object sender, TextCompositionEventArgs e) {
			var array = keyProcessors;
			foreach (var keyProcessor in array) {
				if (keyProcessor.IsInterestedInHandledEvents || !e.Handled)
					keyProcessor.PreviewTextInput(e);
			}
		}

		void VisualElement_TextInputStart(object sender, TextCompositionEventArgs e) {
			var array = keyProcessors;
			foreach (var keyProcessor in array) {
				if (keyProcessor.IsInterestedInHandledEvents || !e.Handled)
					keyProcessor.TextInputStart(e);
			}
		}

		void VisualElement_PreviewTextInputStart(object sender, TextCompositionEventArgs e) {
			var array = keyProcessors;
			foreach (var keyProcessor in array) {
				if (keyProcessor.IsInterestedInHandledEvents || !e.Handled)
					keyProcessor.PreviewTextInputStart(e);
			}
		}

		void VisualElement_TextInputUpdate(object sender, TextCompositionEventArgs e) {
			var array = keyProcessors;
			foreach (var keyProcessor in array) {
				if (keyProcessor.IsInterestedInHandledEvents || !e.Handled)
					keyProcessor.TextInputUpdate(e);
			}
		}

		void VisualElement_PreviewTextInputUpdate(object sender, TextCompositionEventArgs e) {
			var array = keyProcessors;
			foreach (var keyProcessor in array) {
				if (keyProcessor.IsInterestedInHandledEvents || !e.Handled)
					keyProcessor.PreviewTextInputUpdate(e);
			}
		}

		void CleanUp() {
			var array = keyProcessors;
			keyProcessors = Array.Empty<KeyProcessor>();
			foreach (var k in array)
				(k as IDisposable)?.Dispose();
		}

		void Reinitialize() {
			CleanUp();
			var list = new List<KeyProcessor>();
			foreach (var provider in keyProcessorProviders) {
				if (!wpfTextView.Roles.ContainsAny(provider.Metadata.TextViewRoles))
					continue;
				if (!wpfTextView.TextDataModel.ContentType.IsOfAnyType(provider.Metadata.ContentTypes))
					continue;
				var keyProcessor = provider.Value.GetAssociatedProcessor(wpfTextView);
				if (keyProcessor != null)
					list.Add(keyProcessor);
			}
			keyProcessors = list.ToArray();
		}

		void TextDataModel_ContentTypeChanged(object sender, TextDataModelContentTypeChangedEventArgs e) => Reinitialize();

		void WpfTextView_Closed(object sender, EventArgs e) {
			CleanUp();
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.TextDataModel.ContentTypeChanged -= TextDataModel_ContentTypeChanged;
			wpfTextView.VisualElement.RemoveHandler(UIElement.KeyDownEvent, new KeyEventHandler(VisualElement_KeyDown));
			wpfTextView.VisualElement.RemoveHandler(UIElement.KeyUpEvent, new KeyEventHandler(VisualElement_KeyUp));
			wpfTextView.VisualElement.RemoveHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(VisualElement_PreviewKeyDown));
			wpfTextView.VisualElement.RemoveHandler(UIElement.PreviewKeyUpEvent, new KeyEventHandler(VisualElement_PreviewKeyUp));
			wpfTextView.VisualElement.RemoveHandler(UIElement.TextInputEvent, new TextCompositionEventHandler(VisualElement_TextInput));
			wpfTextView.VisualElement.RemoveHandler(UIElement.PreviewTextInputEvent, new TextCompositionEventHandler(VisualElement_PreviewTextInput));
			wpfTextView.VisualElement.RemoveHandler(TextCompositionManager.TextInputStartEvent, new TextCompositionEventHandler(VisualElement_TextInputStart));
			wpfTextView.VisualElement.RemoveHandler(TextCompositionManager.PreviewTextInputStartEvent, new TextCompositionEventHandler(VisualElement_PreviewTextInputStart));
			wpfTextView.VisualElement.RemoveHandler(TextCompositionManager.TextInputUpdateEvent, new TextCompositionEventHandler(VisualElement_TextInputUpdate));
			wpfTextView.VisualElement.RemoveHandler(TextCompositionManager.PreviewTextInputUpdateEvent, new TextCompositionEventHandler(VisualElement_PreviewTextInputUpdate));
		}
	}
}
