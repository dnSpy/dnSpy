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
using System.Diagnostics;
using System.IO;

namespace dnSpy.Decompiler.Shared {
	/// <summary>
	/// Implements <see cref="IDecompilerOutput"/> and writes the text to a <see cref="TextWriter"/>
	/// </summary>
	public sealed class TextWriterDecompilerOutput : IDecompilerOutput, IDisposable {
		readonly TextWriter writer;
		readonly string indentationString;
		int indentation;
		bool addIndent = true;
		int position;

		/// <summary>
		/// Gets the total length of the written text
		/// </summary>
		public int Length => position;

		/// <summary>
		/// This equals <see cref="Length"/> plus any indentation that must be written
		/// before the next text.
		/// </summary>
		public int NextPosition => position + (addIndent ? indentation * indentationString.Length : 0);

		bool IDecompilerOutput.UsesDebugInfo => false;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="writer">Text writer to use</param>
		/// <param name="indentationString">Indentation string</param>
		public TextWriterDecompilerOutput(TextWriter writer, string indentationString = "\t") {
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));
			if (indentationString == null)
				throw new ArgumentNullException(nameof(indentationString));
			this.writer = writer;
			this.indentationString = indentationString;
		}

		void IDecompilerOutput.AddDebugInfo(MethodDebugInfo methodDebugInfo) { }

		/// <summary>
		/// Increments the indentation level. Nothing is added to the output stream.
		/// </summary>
		public void Indent() => indentation++;

		/// <summary>
		/// Decrements the indentation level. Nothing is added to the output stream.
		/// </summary>
		public void Unindent() {
			Debug.Assert(indentation > 0);
			if (indentation > 0)
				indentation--;
		}

		/// <summary>
		/// Writes a new line without writing any indentation
		/// </summary>
		public void WriteLine() {
			var nlArray = newLineArray;
			writer.Write(nlArray);
			position += nlArray.Length;
			addIndent = true;
		}
		static readonly char[] newLineArray = new char[] { '\r', '\n' };

		void AddIndent() {
			if (!addIndent)
				return;
			addIndent = false;
			for (int i = 0; i < indentation; i++)
				writer.Write(indentationString);
			position += indentationString.Length * indentation;
		}

		void AddText(string text, object color) {
			if (addIndent)
				AddIndent();
			writer.Write(text);
			position += text.Length;
		}

		void AddText(string text, int index, int count, object color) {
			if (addIndent)
				AddIndent();
			if (index == 0 && count == text.Length)
				writer.Write(text);
			else
				writer.Write(text.Substring(index, count));
			position += count;
		}

		/// <summary>
		/// Writes text and color. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="color">Color, eg. <see cref="BoxedTextTokenKind.Keyword"/></param>
		public void Write(string text, object color) => AddText(text, color);

		/// <summary>
		/// Writes text and color. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="index">Index in <paramref name="text"/></param>
		/// <param name="count">Number of characters to write</param>
		/// <param name="color">Color, eg. <see cref="BoxedTextTokenKind.Keyword"/></param>
		public void Write(string text, int index, int count, object color) => AddText(text, index, count, color);

		/// <summary>
		/// Writes text, color and a reference. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="reference">Reference</param>
		/// <param name="flags">Flags</param>
		/// <param name="color">Color, eg. <see cref="BoxedTextTokenKind.Keyword"/></param>
		public void Write(string text, object reference, DecompilerReferenceFlags flags, object color) => AddText(text, color);

		/// <summary>
		/// Returns the result from <see cref="TextWriter"/>'s <see cref="object.ToString"/> method
		/// </summary>
		/// <returns></returns>
		public override string ToString() => writer.ToString();

		/// <summary>
		/// Disposes this instance and its underlying <see cref="TextWriter"/>
		/// </summary>
		public void Dispose() => writer.Dispose();
	}
}
