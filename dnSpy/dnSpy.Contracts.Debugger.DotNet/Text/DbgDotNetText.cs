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
using System.Text;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Contracts.Debugger.DotNet.Text {
	/// <summary>
	/// Contains text and color
	/// </summary>
	public readonly struct DbgDotNetText {
		/// <summary>
		/// Gets the empty instance
		/// </summary>
		public static readonly DbgDotNetText Empty = new DbgDotNetText(Array.Empty<DbgDotNetTextPart>());

		/// <summary>
		/// Gets all colors and text parts
		/// </summary>
		public DbgDotNetTextPart[] Parts { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Text</param>
		public DbgDotNetText(DbgDotNetTextPart text) => Parts = new[] { text };

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parts">Color and text</param>
		public DbgDotNetText(DbgDotNetTextPart[] parts) => Parts = parts ?? throw new ArgumentNullException(nameof(parts));

		/// <summary>
		/// Writes all text and colors to <paramref name="output"/>
		/// </summary>
		/// <param name="output">Output</param>
		public void WriteTo(IDbgTextWriter output) {
			foreach (var part in Parts)
				output.Write(part.Color, part.Text);
		}

		/// <summary>
		/// Gets all text
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (Parts.Length == 1)
				return Parts[0].Text;
			var sb = new StringBuilder();
			foreach (var part in Parts)
				sb.Append(part.Text);
			return sb.ToString();
		}
	}

	/// <summary>
	/// Color and text
	/// </summary>
	public readonly struct DbgDotNetTextPart {
		/// <summary>
		/// Gets the color
		/// </summary>
		public DbgTextColor Color { get; }

		/// <summary>
		/// Gets the text
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		public DbgDotNetTextPart(DbgTextColor color, string text) {
			Color = color;
			Text = text ?? throw new ArgumentNullException(nameof(text));
		}
	}
}
