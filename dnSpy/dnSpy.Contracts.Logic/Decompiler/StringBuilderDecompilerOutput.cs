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
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Implements <see cref="IDecompilerOutput"/> and writes the text to a <see cref="StringBuilder"/>
	/// </summary>
	public class StringBuilderDecompilerOutput : IDecompilerOutput {
		readonly StringBuilder sb;
		readonly Indenter indenter;
		bool addIndent = true;

		/// <summary>
		/// Gets the total length of the written text
		/// </summary>
		public virtual int Length => sb.Length;

		/// <summary>
		/// This equals <see cref="Length"/> plus any indentation that must be written
		/// before the next text.
		/// </summary>
		public virtual int NextPosition => sb.Length + (addIndent ? indenter.String.Length : 0);

		bool IDecompilerOutput.UsesCustomData => false;

		/// <summary>
		/// Constructor
		/// </summary>
		public StringBuilderDecompilerOutput() {
			sb = new StringBuilder();
			indenter = new Indenter(4, 4, true);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="indenter">Indenter</param>
		public StringBuilderDecompilerOutput(Indenter indenter) {
			if (indenter == null)
				throw new ArgumentNullException(nameof(indenter));
			sb = new StringBuilder();
			this.indenter = indenter;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stringBuilder">String builder to use. Its <see cref="StringBuilder.Clear"/> method gets called by the constructor</param>
		/// <param name="indenter">Indenter or null</param>
		public StringBuilderDecompilerOutput(StringBuilder stringBuilder, Indenter indenter = null) {
			if (stringBuilder == null)
				throw new ArgumentNullException(nameof(stringBuilder));
			stringBuilder.Clear();
			sb = stringBuilder;
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
			sb.AppendLine();
			addIndent = true;
		}

		void AddIndent() {
			if (!addIndent)
				return;
			addIndent = false;
			sb.Append(indenter.String);
		}

		void AddText(string text, object color) {
			if (addIndent)
				AddIndent();
			sb.Append(text);
		}

		void AddText(string text, int index, int length, object color) {
			if (addIndent)
				AddIndent();
			sb.Append(text, index, length);
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
		public virtual void Write(string text, object reference, DecompilerReferenceFlags flags, object color) {
			if (addIndent)
				AddIndent();
			AddText(text, color);
		}

		/// <summary>
		/// Writes text, color and a reference. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="index">Index in <paramref name="text"/></param>
		/// <param name="length">Number of characters to write</param>
		/// <param name="reference">Reference</param>
		/// <param name="flags">Flags</param>
		/// <param name="color">Color, eg. <see cref="BoxedTextColor.Keyword"/></param>
		public virtual void Write(string text, int index, int length, object reference, DecompilerReferenceFlags flags, object color) {
			if (addIndent)
				AddIndent();
			AddText(text, index, length, color);
		}

		/// <summary>
		/// Gets the text
		/// </summary>
		/// <returns></returns>
		public string GetText() => sb.ToString();

		/// <summary>
		/// Gets the text
		/// </summary>
		/// <returns></returns>
		public override string ToString() => sb.ToString();
	}
}
