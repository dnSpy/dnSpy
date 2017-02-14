/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// An <see cref="ITextColorWriter"/> using a <see cref="StringBuilder"/>. It ignores
	/// all colors passed to it.
	/// </summary>
	public sealed class StringBuilderTextColorOutput : ITextColorWriter {
		readonly StringBuilder sb;

		/// <summary>
		/// true if nothing has been written
		/// </summary>
		public bool IsEmpty => sb.Length == 0;

		/// <summary>
		/// Gets all the text
		/// </summary>
		public string Text => sb.ToString();

		/// <summary>
		/// Constructor
		/// </summary>
		public StringBuilderTextColorOutput() => sb = new StringBuilder();

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stringBuilder">String builder</param>
		public StringBuilderTextColorOutput(StringBuilder stringBuilder) => sb = stringBuilder ?? throw new ArgumentNullException(nameof(stringBuilder));

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		public void Write(object color, string text) => sb.Append(text);

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		public void Write(TextColor color, string text) => sb.Append(text);

		/// <summary>
		/// Gets all the text
		/// </summary>
		/// <returns></returns>
		public override string ToString() => sb.ToString();
	}
}
