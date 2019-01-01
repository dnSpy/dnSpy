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

namespace dnSpy.Contracts.AsmEditor.Compiler {
	/// <summary>
	/// Line location span
	/// </summary>
	public readonly struct LineLocationSpan {
		/// <summary>
		/// Start line position
		/// </summary>
		public LineLocation StartLinePosition { get; }

		/// <summary>
		/// End line position
		/// </summary>
		public LineLocation EndLinePosition { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="startLinePosition">Start line position</param>
		/// <param name="endLinePosition">End line position</param>
		public LineLocationSpan(LineLocation startLinePosition, LineLocation endLinePosition) {
			StartLinePosition = startLinePosition;
			EndLinePosition = endLinePosition;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{StartLinePosition}-{EndLinePosition}";
	}
}
