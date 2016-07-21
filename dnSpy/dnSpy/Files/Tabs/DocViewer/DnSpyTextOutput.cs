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
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Shared;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Files.Tabs.DocViewer {
	sealed class DnSpyTextOutput : IDnSpyTextOutput {
		public bool CanBeCached => canBeCached;

		readonly CachedTextTokenColors cachedTextTokenColors;
		readonly StringBuilder stringBuilder;
		readonly List<MemberMapping> memberMappingsList;
		SpanDataCollectionBuilder<ReferenceInfo> referenceBuilder;
		int line, lineStart;
		int indentation;
		bool canBeCached;
		bool addIndent;
		bool hasCreatedResult;

		TextPosition ITextOutput.Location => new TextPosition(line, stringBuilder.Length - lineStart + GetIndentSize());

		public DnSpyTextOutput() {
			this.cachedTextTokenColors = new CachedTextTokenColors();
			this.stringBuilder = new StringBuilder();
			this.referenceBuilder = SpanDataCollectionBuilder<ReferenceInfo>.CreateBuilder();
			this.memberMappingsList = new List<MemberMapping>();
			this.canBeCached = true;
		}

		public DnSpyTextOutputResult CreateResult() {
			if (hasCreatedResult)
				throw new InvalidOperationException(nameof(CreateResult) + " can only be called once");
			hasCreatedResult = true;
			return new DnSpyTextOutputResult(stringBuilder.ToString(), cachedTextTokenColors, referenceBuilder.Create(), memberMappingsList.ToArray());
		}

		void IDnSpyTextOutput.SetCanNotBeCached() => canBeCached = false;

		public void AddDebugSymbols(MemberMapping memberMapping) => memberMappingsList.Add(memberMapping);

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
			line++;
			lineStart = stringBuilder.Length;
			Debug.Assert(stringBuilder.Length == cachedTextTokenColors.Length);
		}

		int GetIndentSize() => indentation;// Tabs are used

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

		void AddText(string text, object data) {
			if (addIndent)
				AddIndent();
			stringBuilder.Append(text);
			cachedTextTokenColors.Append(data, text);
		}

		void AddText(string text, int index, int count, object data) {
			if (addIndent)
				AddIndent();
			stringBuilder.Append(text, index, count);
			cachedTextTokenColors.Append(data, text, index, count);
		}

		public void Write(string text, TextTokenKind tokenKind) => AddText(text, tokenKind.Box());
		public void Write(string text, object data) => AddText(text, data);
		public void Write(string text, int index, int count, TextTokenKind tokenKind) => AddText(text, index, count, tokenKind.Box());
		public void Write(string text, int index, int count, object data) => AddText(text, index, count, data);

		public void Write(StringBuilder sb, int index, int count, TextTokenKind tokenKind) => Write(sb, index, count, tokenKind.Box());
		public void Write(StringBuilder sb, int index, int count, object data) {
			if (index == 0 && sb.Length == count)
				AddText(sb.ToString(), data);
			else
				AddText(sb.ToString(index, count), data);
		}

		public void WriteDefinition(string text, object definition, TextTokenKind tokenKind, bool isLocal) =>
			WriteDefinition(text, definition, tokenKind.Box(), isLocal);
		public void WriteDefinition(string text, object definition, object data, bool isLocal) {
			if (addIndent)
				AddIndent();
			referenceBuilder.Add(new Span(stringBuilder.Length, text.Length), new ReferenceInfo(definition, isLocal, true));
			AddText(text, data);
		}

		public void WriteReference(string text, object reference, TextTokenKind tokenKind, bool isLocal) =>
			WriteReference(text, reference, tokenKind.Box(), isLocal);
		public void WriteReference(string text, object reference, object data, bool isLocal) {
			if (addIndent)
				AddIndent();
			referenceBuilder.Add(new Span(stringBuilder.Length, text.Length), new ReferenceInfo(reference, isLocal, false));
			AddText(text, data);
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
