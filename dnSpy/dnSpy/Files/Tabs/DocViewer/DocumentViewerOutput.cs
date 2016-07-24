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
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Files.Tabs.DocViewer {
	sealed class DocumentViewerOutput : IDocumentViewerOutput {
		public bool CanBeCached => canBeCached;

		readonly CachedTextTokenColors cachedTextTokenColors;
		readonly StringBuilder stringBuilder;
		readonly List<MethodDebugInfo> methodDebugInfos;
		SpanDataCollectionBuilder<ReferenceInfo> referenceBuilder;
		int indentation;
		bool canBeCached;
		bool addIndent = true;
		bool hasCreatedResult;

		int IDecompilerOutput.Length => stringBuilder.Length;
		int IDecompilerOutput.NextPosition => stringBuilder.Length + GetIndentSize();

		public DocumentViewerOutput() {
			this.cachedTextTokenColors = new CachedTextTokenColors();
			this.stringBuilder = new StringBuilder();
			this.referenceBuilder = SpanDataCollectionBuilder<ReferenceInfo>.CreateBuilder();
			this.methodDebugInfos = new List<MethodDebugInfo>();
			this.canBeCached = true;
		}

		public DocumentViewerContent CreateResult() {
			if (hasCreatedResult)
				throw new InvalidOperationException(nameof(CreateResult) + " can only be called once");
			hasCreatedResult = true;
			return new DocumentViewerContent(stringBuilder.ToString(), cachedTextTokenColors, referenceBuilder.Create(), methodDebugInfos.ToArray());
		}

		void IDocumentViewerOutput.DisableCaching() => canBeCached = false;

		bool IDecompilerOutput.UsesDebugInfo => true;
		public void AddDebugInfo(MethodDebugInfo methodDebugInfo) => methodDebugInfos.Add(methodDebugInfo);

		public void Indent() => indentation++;

		public void Unindent() {
			Debug.Assert(indentation > 0);
			if (indentation > 0)
				indentation--;
		}

		public void WriteLine() {
			addIndent = true;
			cachedTextTokenColors.AppendLine();
			stringBuilder.AppendLine();
			Debug.Assert(stringBuilder.Length == cachedTextTokenColors.Length);
		}

		int GetIndentSize() => addIndent ? indentation : 0;// Tabs are used

		void AddIndent() {
			if (!addIndent)
				return;
			addIndent = false;
			int count = indentation;
			while (count > 0) {
				switch (count) {
				case 1:
					AddText("\t", BoxedOutputColor.Text);
					return;
				case 2:
					AddText("\t\t", BoxedOutputColor.Text);
					return;
				case 3:
					AddText("\t\t\t", BoxedOutputColor.Text);
					return;
				case 4:
					AddText("\t\t\t\t", BoxedOutputColor.Text);
					return;
				case 5:
					AddText("\t\t\t\t\t", BoxedOutputColor.Text);
					return;
				case 6:
					AddText("\t\t\t\t\t\t", BoxedOutputColor.Text);
					return;
				default:
					AddText("\t\t\t\t\t\t\t", BoxedOutputColor.Text);
					count -= 7;
					break;
				}
			}
		}

		void AddText(string text, object color) {
			if (addIndent)
				AddIndent();
			stringBuilder.Append(text);
			cachedTextTokenColors.Append(color, text);
		}

		void AddText(string text, int index, int count, object color) {
			if (addIndent)
				AddIndent();
			stringBuilder.Append(text, index, count);
			cachedTextTokenColors.Append(color, text, index, count);
		}

		public void Write(string text, object color) => AddText(text, color);
		public void Write(string text, int index, int count, object color) => AddText(text, index, count, color);

		public void Write(string text, object reference, DecompilerReferenceFlags flags, object color) {
			if (addIndent)
				AddIndent();
			referenceBuilder.Add(new Span(stringBuilder.Length, text.Length), new ReferenceInfo(reference, flags));
			AddText(text, color);
		}

		public void AddUIElement(Func<UIElement> createElement) {
			if (createElement == null)
				throw new ArgumentNullException(nameof(createElement));
			canBeCached = false;
			//TODO:
		}

		public void AddButton(string buttonText, RoutedEventHandler clickHandler) {
			if (buttonText == null)
				throw new ArgumentNullException(nameof(buttonText));
			if (clickHandler == null)
				throw new ArgumentNullException(nameof(clickHandler));
			AddUIElement(() => {
				var button = new Button { Content = buttonText };
				button.SetResourceReference(FrameworkElement.StyleProperty, "TextEditorButton");
				button.Click += clickHandler;
				return button;
			});
		}
	}
}
