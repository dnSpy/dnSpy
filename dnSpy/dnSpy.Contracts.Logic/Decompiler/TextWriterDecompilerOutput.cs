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

using System;
using System.IO;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Implements <see cref="IDecompilerOutput"/> and writes the text to a <see cref="TextWriter"/>
	/// </summary>
	public class TextWriterDecompilerOutput : IDecompilerOutput, IDisposable {
		readonly TextWriter writer;
		readonly Indenter indenter;
		bool addIndent = true;
		int position;

		/// <summary>
		/// Gets the total length of the written text
		/// </summary>
		public virtual int Length => position;

		/// <summary>
		/// This equals <see cref="Length"/> plus any indentation that must be written
		/// before the next text.
		/// </summary>
		public virtual int NextPosition => position + (addIndent ? indenter.String.Length : 0);

		bool IDecompilerOutput.UsesCustomData => false;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="writer">Text writer to use</param>
		/// <param name="indenter">Indenter or null</param>
		public TextWriterDecompilerOutput(TextWriter writer, Indenter indenter = null) {
			this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
			this.indenter = indenter ?? new Indenter(4, 4, true);
		}

		void IDecompilerOutput.AddCustomData<TData>(string id, TData data) { }

		/// <summary>
		/// Increments the indentation level. Nothing is added to the output stream.
		/// </summary>
		public virtual void IncreaseIndent() => indenter.IncreaseIndent();

		/// <summary>
		/// Decrements the indentation level. Nothing is added to the output stream.
		/// </summary>
		public virtual void DecreaseIndent() => indenter.DecreaseIndent();

		/// <summary>
		/// Writes a new line without writing any indentation
		/// </summary>
		public virtual void WriteLine() {
			var nlArray = newLineArray;
			writer.Write(nlArray);
			position += nlArray.Length;
			addIndent = true;
		}
		static readonly char[] newLineArray = Environment.NewLine.ToCharArray();

		void AddIndent() {
			if (!addIndent)
				return;
			addIndent = false;
			var s = indenter.String;
			writer.Write(s);
			position += s.Length;
		}

		void AddText(string text, object color) {
			if (addIndent)
				AddIndent();
			writer.Write(text);
			position += text.Length;
		}

		void AddText(string text, int index, int length, object color) {
			if (addIndent)
				AddIndent();
			if (index == 0 && length == text.Length)
				writer.Write(text);
			else
				writer.Write(text.Substring(index, length));
			position += length;
		}

		/// <summary>
		/// Writes text. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		public virtual void Write(string text) => AddText(text, BoxedTextColor.Text);

		/// <summary>
		/// Writes text and color. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="color">Color, eg. <see cref="BoxedTextColor.Keyword"/></param>
		public virtual void Write(string text, object color) => AddText(text, color);

		/// <summary>
		/// Writes text and color. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="index">Index in <paramref name="text"/></param>
		/// <param name="length">Number of characters to write</param>
		/// <param name="color">Color, eg. <see cref="BoxedTextColor.Keyword"/></param>
		public virtual void Write(string text, int index, int length, object color) => AddText(text, index, length, color);

		/// <summary>
		/// Writes text, color and a reference. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="reference">Reference</param>
		/// <param name="flags">Flags</param>
		/// <param name="color">Color, eg. <see cref="BoxedTextColor.Keyword"/></param>
		public virtual void Write(string text, object reference, DecompilerReferenceFlags flags, object color) => AddText(text, color);

		/// <summary>
		/// Writes text, color and a reference. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="index">Index in <paramref name="text"/></param>
		/// <param name="length">Number of characters to write</param>
		/// <param name="reference">Reference</param>
		/// <param name="flags">Flags</param>
		/// <param name="color">Color, eg. <see cref="BoxedTextColor.Keyword"/></param>
		public virtual void Write(string text, int index, int length, object reference, DecompilerReferenceFlags flags, object color) => AddText(text, index, length, color);

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
