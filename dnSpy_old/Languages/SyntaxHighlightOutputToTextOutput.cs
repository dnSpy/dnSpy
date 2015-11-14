/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.Highlighting;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;

namespace dnSpy.LanguagesInternal {
	sealed class SyntaxHighlightOutputToTextOutput : ITextOutput {
		readonly ISyntaxHighlightOutput output;
		int line, col;
		int indent;

		public SyntaxHighlightOutputToTextOutput(ISyntaxHighlightOutput output) {
			this.output = output;
			this.line = 1;
			this.col = 1;
			this.indent = 0;
		}

		public TextLocation Location {
			get { return new TextLocation(line, col + indent); }
		}

		public void AddDebugSymbols(MemberMapping methodDebugSymbols) {
		}

		public void Indent() {
			indent++;
		}

		public void Unindent() {
			indent--;
		}

		public void Write(string text, TextTokenType tokenType) {
			if (col == 1 && indent > 0)
				output.Write(new string('\t', indent), TextTokenType.Text);
			output.Write(text, tokenType);
			int index = text.LastIndexOfAny(newLineChars);
			if (index >= 0) {
				line += text.Split(new char[] { '\n' }).Length - 1;	// good enough for our purposes
				col = text.Length - (index + 1) + 1;
				indent = 0;
			}
			else
				col += text.Length;
		}
		static readonly char[] newLineChars = new char[] { '\r', '\n' };

		public void WriteDefinition(string text, object definition, TextTokenType tokenType, bool isLocal = true) {
			Write(text, tokenType);
		}

		public void WriteLine() {
			Write(Environment.NewLine, TextTokenType.Text);
		}

		public void WriteReference(string text, object reference, TextTokenType tokenType, bool isLocal = false) {
			Write(text, tokenType);
		}
	}
}
