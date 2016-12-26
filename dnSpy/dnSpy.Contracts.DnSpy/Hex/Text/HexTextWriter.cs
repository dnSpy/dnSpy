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

namespace dnSpy.Contracts.Hex.Text {
	/// <summary>
	/// Writes text
	/// </summary>
	public abstract class HexTextWriter {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexTextWriter() { }

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="tag">Tag, see <see cref="PredefinedClassifiedTextTags"/></param>
		public abstract void Write(string text, string tag);

		/// <summary>
		/// Writes a space
		/// </summary>
		public void WriteSpace() => Write(" ", PredefinedClassifiedTextTags.Text);

		/// <summary>
		/// Writes a new line
		/// </summary>
		public void WriteLine() => Write(Environment.NewLine, PredefinedClassifiedTextTags.Text);
	}
}
