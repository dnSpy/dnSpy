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
using System.Text;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Debugger.Text.DnSpy {
	/// <summary>
	/// Implements <see cref="IDbgTextWriter"/> and stores all colors and text.
	/// The result can be passed to <see cref="TextClassifierContext.TextClassifierContext(string, string, bool, IReadOnlyCollection{SpanData{object}})"/>
	/// </summary>
	public sealed class DbgTextClassifierTextColorWriter : IDbgTextWriter {
		/// <summary>
		/// Gets the text
		/// </summary>
		public string Text => sb.ToString();

		/// <summary>
		/// Gets the text length
		/// </summary>
		public int Length => sb.Length;

		/// <summary>
		/// Gets the colors
		/// </summary>
		public List<SpanData<object>> Colors => colors;

		readonly List<SpanData<object>> colors;
		readonly StringBuilder sb;

		/// <summary>
		/// Constructor
		/// </summary>
		public DbgTextClassifierTextColorWriter() {
			colors = new List<SpanData<object>>();
			sb = new StringBuilder();
		}

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		public void Write(DbgTextColor color, string text) {
			colors.Add(new SpanData<object>(new Span(sb.Length, text.Length), ColorConverter.ToDnSpyColor(color)));
			sb.Append(text);
		}

		/// <summary>
		/// Clears the text and colors so the instance can be reused
		/// </summary>
		public void Clear() {
			colors.Clear();
			sb.Clear();
		}
	}
}
