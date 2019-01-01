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

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnSpy.Contracts.Disassembly.Viewer;

namespace dnSpy.Disassembly.Viewer {
	sealed class DisassemblyContentOutput {
		readonly List<DisassemblyText> textList;
		int currentLength;

		public DisassemblyContentOutput() => textList = new List<DisassemblyText>();

		void AddText(string text, object color, object reference, DisassemblyReferenceFlags flags) {
			if (reference == null && textList.Count != 0 && textList[textList.Count - 1].Color == color) {
				var last = textList[textList.Count - 1];
				textList[textList.Count - 1] = new DisassemblyText(color, last.Text + text, reference, flags);
			}
			else 
				textList.Add(new DisassemblyText(color, text, reference, flags));
			currentLength += text.Length;
		}

		public void Write(string text, object color) => AddText(text, color, null, DisassemblyReferenceFlags.None);
		public void Write(string text, object reference, DisassemblyReferenceFlags flags, object color) => AddText(text, color, reference, flags);

		public DisassemblyContent Create(DisassemblyContentKind contentKind) {
			Debug.Assert(ToString().Length == currentLength);
			return new DisassemblyContent(contentKind, textList.ToArray());
		}

		public override string ToString() {
			var sb = new StringBuilder();
			foreach (var text in textList)
				sb.Append(text.Text);
			return sb.ToString();
		}
	}
}
