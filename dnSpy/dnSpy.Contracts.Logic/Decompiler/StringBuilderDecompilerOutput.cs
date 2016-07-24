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
using System.Text;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Implements <see cref="IDecompilerOutput"/> and writes the text to a <see cref="StringBuilder"/>
	/// </summary>
	public sealed class StringBuilderDecompilerOutput : IDecompilerOutput {
		readonly StringBuilder sb;
		readonly string indentationString;
		int indentation;
		bool addIndent = true;

		/// <summary>
		/// Gets the total length of the written text
		/// </summary>
		public int Length => sb.Length;

		/// <summary>
		/// This equals <see cref="Length"/> plus any indentation that must be written
		/// before the next text.
		/// </summary>
		public int NextPosition => sb.Length + (addIndent ? indentation * indentationString.Length : 0);

		bool IDecompilerOutput.UsesDebugInfo => false;

		/// <summary>
		/// Constructor
		/// </summary>
		public StringBuilderDecompilerOutput() {
			this.sb = new StringBuilder();
			this.indentationString = "\t";
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="indentationString">Indentation string</param>
		public StringBuilderDecompilerOutput(string indentationString) {
			if (indentationString == null)
				throw new ArgumentNullException(nameof(indentationString));
			this.sb = new StringBuilder();
			this.indentationString = indentationString;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stringBuilder">String builder to use. Its <see cref="StringBuilder.Clear"/> method gets called by the constructor</param>
		/// <param name="indentationString">Indentation string</param>
		public StringBuilderDecompilerOutput(StringBuilder stringBuilder, string indentationString = "\t") {
			if (stringBuilder == null)
				throw new ArgumentNullException(nameof(stringBuilder));
			if (indentationString == null)
				throw new ArgumentNullException(nameof(indentationString));
			stringBuilder.Clear();
			this.sb = stringBuilder;
			this.indentationString = indentationString;
		}

		void IDecompilerOutput.AddDebugInfo(MethodDebugInfo methodDebugInfo) { }

		/// <summary>
		/// Increments the indentation level. Nothing is added to the output stream.
		/// </summary>
		public void IncreaseIndent() => indentation++;

		/// <summary>
		/// Decrements the indentation level. Nothing is added to the output stream.
		/// </summary>
		public void DecreaseIndent() {
			Debug.Assert(indentation > 0);
			if (indentation > 0)
				indentation--;
		}

		/// <summary>
		/// Writes a new line without writing any indentation
		/// </summary>
		public void WriteLine() {
			sb.AppendLine();
			addIndent = true;
		}

		void AddIndent() {
			if (!addIndent)
				return;
			addIndent = false;
			for (int i = 0; i < indentation; i++)
				sb.Append(indentationString);
		}

		void AddText(string text, object color) {
			if (addIndent)
				AddIndent();
			sb.Append(text);
		}

		void AddText(string text, int index, int count, object color) {
			if (addIndent)
				AddIndent();
			sb.Append(text, index, count);
		}

		/// <summary>
		/// Writes text and color. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="color">Color, eg. <see cref="BoxedOutputColor.Keyword"/></param>
		public void Write(string text, object color) => AddText(text, color);

		/// <summary>
		/// Writes text and color. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="index">Index in <paramref name="text"/></param>
		/// <param name="count">Number of characters to write</param>
		/// <param name="color">Color, eg. <see cref="BoxedOutputColor.Keyword"/></param>
		public void Write(string text, int index, int count, object color) => AddText(text, index, count, color);

		/// <summary>
		/// Writes text, color and a reference. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="reference">Reference</param>
		/// <param name="flags">Flags</param>
		/// <param name="color">Color, eg. <see cref="BoxedOutputColor.Keyword"/></param>
		public void Write(string text, object reference, DecompilerReferenceFlags flags, object color) {
			if (addIndent)
				AddIndent();
			AddText(text, color);
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
