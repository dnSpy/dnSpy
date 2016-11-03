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
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed class DocumentViewerOutput : IDocumentViewerOutput {
		readonly CachedTextColorsCollection cachedTextColorsCollection;
		readonly StringBuilder stringBuilder;
		readonly Dictionary<string, object> customDataDict;
		readonly Indenter indenter;
		State state;
		SpanDataCollectionBuilder<ReferenceInfo> referenceBuilder;
		bool canBeCached;
		bool addIndent = true;

		enum State {
			GeneratingContent,
			PostProcessing,
			CustomDataProviders,
			ContentCreated,
		}

		public bool CanBeCached {
			get {
				VerifyNotCreated();
				return canBeCached;
			}
		}

		int IDecompilerOutput.Length {
			get {
				VerifyNotCreated();
				return stringBuilder.Length;
			}
		}

		public int NextPosition {
			get {
				VerifyNotCreated();
				return stringBuilder.Length + (addIndent ? indenter.String.Length : 0);
			}
		}

		internal string GetCachedText() {
			if (cachedText == null)
				throw new InvalidOperationException();
			return cachedText;
		}
		string cachedText;

		internal static DocumentViewerOutput Create() => new DocumentViewerOutput();

		DocumentViewerOutput() {
			this.state = State.GeneratingContent;
			this.cachedTextColorsCollection = new CachedTextColorsCollection();
			this.stringBuilder = new StringBuilder();
			this.referenceBuilder = SpanDataCollectionBuilder<ReferenceInfo>.CreateBuilder();
			this.canBeCached = true;
			this.customDataDict = new Dictionary<string, object>(StringComparer.Ordinal);
			this.indenter = new Indenter(4, 4, true);
		}

		void VerifyGeneratingOrPostProcessing() {
			if (state != State.GeneratingContent && state != State.PostProcessing)
				throw new InvalidOperationException("You can't call this method now");
		}

		void VerifyNotCreated() {
			if (state == State.ContentCreated)
				throw new InvalidOperationException("You can't call this method, content has been created");
		}

		void VerifyState(State expectedState) {
			if (state != expectedState)
				throw new InvalidOperationException("You can't call this method now");
		}

		internal void SetStatePostProcessing() {
			VerifyState(State.GeneratingContent);
			cachedText = stringBuilder.ToString();
			state = State.PostProcessing;
		}

		internal void SetStateCustomDataProviders() {
			VerifyState(State.PostProcessing);
			state = State.CustomDataProviders;
		}

		internal Dictionary<string, object> GetCustomDataDictionary() {
			VerifyState(State.CustomDataProviders);
			return customDataDict;
		}

		internal DocumentViewerContent CreateContent(Dictionary<string, object> dataDict) {
			VerifyState(State.CustomDataProviders);
			state = State.ContentCreated;
			Debug.Assert(cachedText == stringBuilder.ToString());
			return new DocumentViewerContent(cachedText, cachedTextColorsCollection, referenceBuilder.Create(), dataDict);
		}

		void IDocumentViewerOutput.DisableCaching() {
			VerifyGeneratingOrPostProcessing();
			canBeCached = false;
		}

		bool IDecompilerOutput.UsesCustomData {
			get {
				VerifyNotCreated();
				return true;
			}
		}

		public void AddCustomData<TData>(string id, TData data) {
			VerifyGeneratingOrPostProcessing();
			object listObj;
			List<TData> list;
			if (customDataDict.TryGetValue(id, out listObj))
				list = (List<TData>)listObj;
			else
				customDataDict.Add(id, list = new List<TData>());
			list.Add(data);
		}

		public void IncreaseIndent() {
			VerifyState(State.GeneratingContent);
			indenter.IncreaseIndent();
		}

		public void DecreaseIndent() {
			VerifyState(State.GeneratingContent);
			indenter.DecreaseIndent();
		}

		public void WriteLine() {
			VerifyState(State.GeneratingContent);
			addIndent = true;
			cachedTextColorsCollection.Append(BoxedTextColor.Text, Environment.NewLine);
			stringBuilder.Append(Environment.NewLine);
			Debug.Assert(stringBuilder.Length == cachedTextColorsCollection.TextLength);
		}

		void AddIndent() {
			if (!addIndent)
				return;
			addIndent = false;
			AddText(indenter.String, BoxedTextColor.Text);
		}

		void AddText(string text, object color) {
			VerifyState(State.GeneratingContent);
			if (addIndent)
				AddIndent();
			stringBuilder.Append(text);
			cachedTextColorsCollection.Append(color, text);
		}

		void AddText(string text, int index, int length, object color) {
			VerifyState(State.GeneratingContent);
			if (addIndent)
				AddIndent();
			stringBuilder.Append(text, index, length);
			cachedTextColorsCollection.Append(color, text, index, length);
		}

		public void Write(string text, object color) => AddText(text, color);
		public void Write(string text, int index, int length, object color) => AddText(text, index, length, color);

		public void Write(string text, object reference, DecompilerReferenceFlags flags, object color) =>
			Write(text, 0, text.Length, reference, flags, color);

		public void Write(string text, int index, int length, object reference, DecompilerReferenceFlags flags, object color) {
			VerifyState(State.GeneratingContent);
			if (addIndent)
				AddIndent();
			if (reference == null) {
				AddText(text, index, length, color);
				return;
			}
			Debug.Assert(!(reference.GetType().FullName ?? string.Empty).Contains("ICSharpCode"), "Internal decompiler data shouldn't be passed to Write()-ref");
			referenceBuilder.Add(new Span(stringBuilder.Length, length), new ReferenceInfo(reference, flags));
			AddText(text, index, length, color);
		}

		public void AddUIElement(Func<UIElement> createElement) {
			VerifyState(State.GeneratingContent);
			if (createElement == null)
				throw new ArgumentNullException(nameof(createElement));
			if (addIndent)
				AddIndent();
			canBeCached = false;
			AddCustomData(DocumentViewerUIElementConstants.CustomDataId, new DocumentViewerUIElement(NextPosition, createElement));
		}

		public void AddButton(string buttonText, Action clickHandler) {
			VerifyState(State.GeneratingContent);
			if (buttonText == null)
				throw new ArgumentNullException(nameof(buttonText));
			if (clickHandler == null)
				throw new ArgumentNullException(nameof(clickHandler));
			AddUIElement(() => {
				var button = new Button { Content = buttonText };
				button.SetResourceReference(FrameworkElement.StyleProperty, "TextEditorButton");
				button.Click += (s, e) => {
					e.Handled = true;
					clickHandler();
				};
				return button;
			});
		}
	}
}
