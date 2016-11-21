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
using System.Windows.Input;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Hex.MEF;

namespace dnSpy.Hex.Editor {
	sealed class HexKeyProcessorCollection {
		readonly WpfHexView wpfHexView;
		readonly Lazy<HexKeyProcessorProvider, IOrderableTextViewRoleMetadata>[] keyProcessorProviders;
		HexKeyProcessor[] keyProcessors;

		public HexKeyProcessorCollection(WpfHexView wpfHexView, Lazy<HexKeyProcessorProvider, IOrderableTextViewRoleMetadata>[] keyProcessorProviders) {
			this.wpfHexView = wpfHexView;
			this.keyProcessorProviders = keyProcessorProviders;
			this.keyProcessors = Array.Empty<HexKeyProcessor>();
			wpfHexView.Closed += WpfHexView_Closed;
			wpfHexView.VisualElement.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(VisualElement_KeyDown), true);
			wpfHexView.VisualElement.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler(VisualElement_KeyUp), true);
			wpfHexView.VisualElement.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(VisualElement_PreviewKeyDown), true);
			wpfHexView.VisualElement.AddHandler(UIElement.PreviewKeyUpEvent, new KeyEventHandler(VisualElement_PreviewKeyUp), true);
			wpfHexView.VisualElement.AddHandler(UIElement.TextInputEvent, new TextCompositionEventHandler(VisualElement_TextInput), true);
			wpfHexView.VisualElement.AddHandler(UIElement.PreviewTextInputEvent, new TextCompositionEventHandler(VisualElement_PreviewTextInput), true);
			wpfHexView.VisualElement.AddHandler(TextCompositionManager.TextInputStartEvent, new TextCompositionEventHandler(VisualElement_TextInputStart), true);
			wpfHexView.VisualElement.AddHandler(TextCompositionManager.PreviewTextInputStartEvent, new TextCompositionEventHandler(VisualElement_PreviewTextInputStart), true);
			wpfHexView.VisualElement.AddHandler(TextCompositionManager.TextInputUpdateEvent, new TextCompositionEventHandler(VisualElement_TextInputUpdate), true);
			wpfHexView.VisualElement.AddHandler(TextCompositionManager.PreviewTextInputUpdateEvent, new TextCompositionEventHandler(VisualElement_PreviewTextInputUpdate), true);
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
			keyProcessors = Array.Empty<HexKeyProcessor>();
			foreach (var k in array)
				(k as IDisposable)?.Dispose();
		}

		void Reinitialize() {
			CleanUp();
			var list = new List<HexKeyProcessor>();
			foreach (var provider in keyProcessorProviders) {
				if (!wpfHexView.Roles.ContainsAny(provider.Metadata.TextViewRoles))
					continue;
				var keyProcessor = provider.Value.GetAssociatedProcessor(wpfHexView);
				if (keyProcessor != null)
					list.Add(keyProcessor);
			}
			keyProcessors = list.ToArray();
		}

		void WpfHexView_Closed(object sender, EventArgs e) {
			CleanUp();
			wpfHexView.Closed -= WpfHexView_Closed;
			wpfHexView.VisualElement.RemoveHandler(UIElement.KeyDownEvent, new KeyEventHandler(VisualElement_KeyDown));
			wpfHexView.VisualElement.RemoveHandler(UIElement.KeyUpEvent, new KeyEventHandler(VisualElement_KeyUp));
			wpfHexView.VisualElement.RemoveHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(VisualElement_PreviewKeyDown));
			wpfHexView.VisualElement.RemoveHandler(UIElement.PreviewKeyUpEvent, new KeyEventHandler(VisualElement_PreviewKeyUp));
			wpfHexView.VisualElement.RemoveHandler(UIElement.TextInputEvent, new TextCompositionEventHandler(VisualElement_TextInput));
			wpfHexView.VisualElement.RemoveHandler(UIElement.PreviewTextInputEvent, new TextCompositionEventHandler(VisualElement_PreviewTextInput));
			wpfHexView.VisualElement.RemoveHandler(TextCompositionManager.TextInputStartEvent, new TextCompositionEventHandler(VisualElement_TextInputStart));
			wpfHexView.VisualElement.RemoveHandler(TextCompositionManager.PreviewTextInputStartEvent, new TextCompositionEventHandler(VisualElement_PreviewTextInputStart));
			wpfHexView.VisualElement.RemoveHandler(TextCompositionManager.TextInputUpdateEvent, new TextCompositionEventHandler(VisualElement_TextInputUpdate));
			wpfHexView.VisualElement.RemoveHandler(TextCompositionManager.PreviewTextInputUpdateEvent, new TextCompositionEventHandler(VisualElement_PreviewTextInputUpdate));
		}
	}
}
