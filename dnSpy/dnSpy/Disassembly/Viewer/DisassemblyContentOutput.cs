/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnSpy.Contracts.Disassembly.Viewer;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Disassembly.Viewer {
	sealed class DisassemblyContentOutput {
		readonly List<DisassemblyText> textList;
		SpanDataCollectionBuilder<DisassemblyReferenceInfo> referenceBuilder;
		int currentLength;

		public DisassemblyContentOutput() {
			textList = new List<DisassemblyText>();
			referenceBuilder = SpanDataCollectionBuilder<DisassemblyReferenceInfo>.CreateBuilder();
		}

		void AddText(string text, object color) {
			if (textList.Count == 0 || textList[textList.Count - 1].Color != color)
				textList.Add(new DisassemblyText(color, text));
			else {
				var last = textList[textList.Count - 1];
				textList[textList.Count - 1] = new DisassemblyText(color, last.Text + text);
			}
			currentLength += text.Length;
		}

		public void Write(string text, object color) => AddText(text, color);

		public void Write(string text, object reference, DisassemblyReferenceFlags flags, object color) {
			if (reference == null) {
				AddText(text, color);
				return;
			}
			referenceBuilder.Add(new Span(currentLength, text.Length), new DisassemblyReferenceInfo(reference, flags));
			AddText(text, color);
		}

		public DisassemblyContent Create() {
			Debug.Assert(ToString().Length == currentLength);
			return new DisassemblyContent(textList.ToArray(), referenceBuilder.Create());
		}

		public override string ToString() {
			var sb = new StringBuilder();
			foreach (var text in textList)
				sb.Append(text.Text);
			return sb.ToString();
		}
	}
}
