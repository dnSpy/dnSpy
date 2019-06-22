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
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Debugger.Text.DnSpy {
	/// <summary>
	/// Implements <see cref="ITextColorWriter"/> and writes to a <see cref="IDbgTextWriter"/>
	/// </summary>
	public sealed class DbgTextColorWriter : ITextColorWriter {
		readonly IDbgTextWriter writer;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="writer">Debug text writer</param>
		public DbgTextColorWriter(IDbgTextWriter writer) =>
			this.writer = writer ?? throw new ArgumentNullException(nameof(writer));

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		public void Write(object color, string? text) =>
			writer.Write(ColorConverter.ToDebuggerColor(color), text);

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		public void Write(TextColor color, string? text) =>
			Write(color.Box(), text);
	}
}
