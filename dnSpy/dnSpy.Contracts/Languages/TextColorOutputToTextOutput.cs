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
using System.Text;
using dnSpy.Contracts.TextEditor;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Converts a <see cref="IOutputColorWriter"/> to a <see cref="ITextOutput"/>
	/// </summary>
	public sealed class TextColorOutputToTextOutput : ITextOutput {
		readonly IOutputColorWriter output;
		int line, col;
		int indent;

		/// <summary>
		/// Creates a new <see cref="ITextOutput"/> instance
		/// </summary>
		/// <param name="output">Output to use</param>
		/// <returns></returns>
		public static ITextOutput Create(IOutputColorWriter output) =>
			new TextColorOutputToTextOutput(output);

		TextColorOutputToTextOutput(IOutputColorWriter output) {
			this.output = output;
			this.line = 1;
			this.col = 1;
			this.indent = 0;
		}

		TextPosition ITextOutput.Location => new TextPosition(line, col + indent);

		void ITextOutput.AddDebugSymbols(MemberMapping methodDebugSymbols) { }
		void ITextOutput.Indent() => indent++;
		void ITextOutput.Unindent() => indent--;

		void ITextOutput.Write(string text, int index, int count, object data) {
			if (index == 0 && text.Length == count)
				((ITextOutput)this).Write(text, data);
			((ITextOutput)this).Write(text.Substring(index, count), data);
		}

		void ITextOutput.Write(StringBuilder sb, int index, int count, object data) {
			if (index == 0 && sb.Length == count)
				((ITextOutput)this).Write(sb.ToString(), data);
			((ITextOutput)this).Write(sb.ToString(index, count), data);
		}

		void ITextOutput.Write(string text, object data) {
			if (col == 1 && indent > 0)
				output.Write(BoxedOutputColor.Text, new string('\t', indent));
			output.Write(data, text);
			int index = text.LastIndexOfAny(newLineChars);
			if (index >= 0) {
				line += text.Split(lineFeedChar).Length - 1;	// good enough for our purposes
				col = text.Length - (index + 1) + 1;
				indent = 0;
			}
			else
				col += text.Length;
		}
		static readonly char[] lineFeedChar = new char[] { '\n' };
		static readonly char[] newLineChars = new char[] { '\r', '\n' };

		void ITextOutput.WriteDefinition(string text, object definition, object data, bool isLocal) =>
			((ITextOutput)this).Write(text, data);
		void ITextOutput.WriteLine() =>
			((ITextOutput)this).Write(Environment.NewLine, BoxedOutputColor.Text);
		void ITextOutput.WriteReference(string text, object reference, object data, bool isLocal) =>
			((ITextOutput)this).Write(text, data);
		void ITextOutput.Write(string text, TextTokenKind tokenKind) =>
			((ITextOutput)this).Write(text, tokenKind.Box());
		void ITextOutput.Write(string text, int index, int count, TextTokenKind tokenKind) =>
			((ITextOutput)this).Write(text, index, count, tokenKind.Box());
		void ITextOutput.Write(StringBuilder sb, int index, int count, TextTokenKind tokenKind) =>
			((ITextOutput)this).Write(sb, index, count, tokenKind.Box());
		void ITextOutput.WriteDefinition(string text, object definition, TextTokenKind tokenKind, bool isLocal) =>
			((ITextOutput)this).WriteDefinition(text, definition, tokenKind.Box(), isLocal);
		void ITextOutput.WriteReference(string text, object reference, TextTokenKind tokenKind, bool isLocal) =>
			((ITextOutput)this).WriteReference(text, reference, tokenKind.Box(), isLocal);
	}
}
