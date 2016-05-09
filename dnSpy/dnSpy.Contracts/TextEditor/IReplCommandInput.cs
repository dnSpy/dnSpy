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

using System.Collections.Generic;

namespace dnSpy.Contracts.TextEditor {
	/// <summary>
	/// REPL command input
	/// </summary>
	public interface IReplCommandInput {
		/// <summary>
		/// Gets the current input
		/// </summary>
		string Input { get; }

		/// <summary>
		/// Gets the changes
		/// </summary>
		ITextChange[] Changes { get; }

		/// <summary>
		/// Add color info. No overlaps, and must be in order.
		/// </summary>
		/// <param name="offset">Offset of text</param>
		/// <param name="length">Length</param>
		/// <param name="color">Color</param>
		void AddColor(int offset, int length, object color);

		/// <summary>
		/// Add color info. No overlaps, and must be in order.
		/// </summary>
		/// <param name="offset">Offset of text</param>
		/// <param name="length">Length</param>
		/// <param name="color">Color</param>
		void AddColor(int offset, int length, OutputColor color);

		/// <summary>
		/// Add color info. No overlaps, and must be in order.
		/// </summary>
		/// <param name="info">Color info</param>
		void AddColor(ColorOffsetInfo info);

		/// <summary>
		/// Add color infos. No overlaps, and must be in order.
		/// </summary>
		/// <param name="infos">Color infos</param>
		void AddColors(IEnumerable<ColorOffsetInfo> infos);
	}
}
