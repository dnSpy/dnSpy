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

namespace dnSpy.Contracts.AsmEditor.Compiler {
	/// <summary>
	/// Line location
	/// </summary>
	public struct LineLocation {
		/// <summary>
		/// Line, 1-based
		/// </summary>
		public int Line { get; }

		/// <summary>
		/// Column, 1-based
		/// </summary>
		public int Character { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="line">Line, 1-based</param>
		/// <param name="character">Column, 1-based</param>
		public LineLocation(int line, int character) {
			if (line <= 0)
				throw new ArgumentOutOfRangeException(nameof(line));
			if (character <= 0)
				throw new ArgumentOutOfRangeException(nameof(line));
			Line = line;
			Character = character;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"({Line},{Character})";
	}
}
