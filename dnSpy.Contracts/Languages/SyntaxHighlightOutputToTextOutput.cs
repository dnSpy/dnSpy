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

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Converts a <see cref="ISyntaxHighlightOutput"/> to a <see cref="ITextOutput"/>
	/// </summary>
	public sealed class SyntaxHighlightOutputToTextOutput : ITextOutput {
		readonly ISyntaxHighlightOutput output;
		int line, col;
		int indent;

		/// <summary>
		/// Creates a new <see cref="ITextOutput"/> instance
		/// </summary>
		/// <param name="output">Output to use</param>
		/// <returns></returns>
		public static ITextOutput Create(ISyntaxHighlightOutput output) {
			return new SyntaxHighlightOutputToTextOutput(output);
		}

		SyntaxHighlightOutputToTextOutput(ISyntaxHighlightOutput output) {
			this.output = output;
			this.line = 1;
			this.col = 1;
			this.indent = 0;
		}

		TextLocation ITextOutput.Location {
			get { return new TextLocation(line, col + indent); }
		}

		void ITextOutput.AddDebugSymbols(MemberMapping methodDebugSymbols) {
		}

		void ITextOutput.Indent() {
			indent++;
		}

		void ITextOutput.Unindent() {
			indent--;
		}

		void ITextOutput.Write(string text, TextTokenType tokenType) {
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

		void ITextOutput.WriteDefinition(string text, object definition, TextTokenType tokenType, bool isLocal) {
			((ITextOutput)this).Write(text, tokenType);
		}

		void ITextOutput.WriteLine() {
			((ITextOutput)this).Write(Environment.NewLine, TextTokenType.Text);
		}

		void ITextOutput.WriteReference(string text, object reference, TextTokenType tokenType, bool isLocal) {
			((ITextOutput)this).Write(text, tokenType);
		}
	}
}
