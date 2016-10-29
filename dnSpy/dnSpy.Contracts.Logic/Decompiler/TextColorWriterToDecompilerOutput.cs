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
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Converts a <see cref="ITextColorWriter"/> to a <see cref="IDecompilerOutput"/>
	/// </summary>
	public sealed class TextColorWriterToDecompilerOutput : IDecompilerOutput {
		readonly ITextColorWriter output;
		int indent;
		int offset;
		bool addIndent = true;

		/// <summary>
		/// Creates a new <see cref="IDecompilerOutput"/> instance
		/// </summary>
		/// <param name="output">Output to use</param>
		/// <returns></returns>
		public static IDecompilerOutput Create(ITextColorWriter output) =>
			new TextColorWriterToDecompilerOutput(output);

		TextColorWriterToDecompilerOutput(ITextColorWriter output) {
			this.output = output;
			this.indent = 0;
			this.offset = 0;
		}

		int IDecompilerOutput.Length => offset;
		int IDecompilerOutput.NextPosition => offset + (addIndent ? indent : 0);

		bool IDecompilerOutput.UsesCustomData => false;
		void IDecompilerOutput.AddCustomData<TData>(string id, TData data) { }
		void IDecompilerOutput.IncreaseIndent() => indent++;
		void IDecompilerOutput.DecreaseIndent() => indent--;

		void IDecompilerOutput.Write(string text, int index, int length, object color) {
			if (index == 0 && text.Length == length)
				((IDecompilerOutput)this).Write(text, color);
			else
				((IDecompilerOutput)this).Write(text.Substring(index, length), color);
		}

		void IDecompilerOutput.Write(string text, object color) {
			if (addIndent) {
				if (indent != 0)
					output.Write(BoxedTextColor.Text, new string('\t', indent));
				offset += indent;
			}
			output.Write(color, text);
			offset += text.Length;
			addIndent = text.LastIndexOfAny(newLineChars) == text.Length - 1;
		}
		static readonly char[] newLineChars = new char[] { '\r', '\n', '\u0085', '\u2028', '\u2029' };

		void IDecompilerOutput.Write(string text, object reference, DecompilerReferenceFlags flags, object color) =>
			((IDecompilerOutput)this).Write(text, color);
		void IDecompilerOutput.Write(string text, int index, int length, object reference, DecompilerReferenceFlags flags, object color) =>
			((IDecompilerOutput)this).Write(text, index, length, color);
		void IDecompilerOutput.WriteLine() =>
			((IDecompilerOutput)this).Write(Environment.NewLine, BoxedTextColor.Text);
	}
}
