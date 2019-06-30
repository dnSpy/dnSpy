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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Text editor position
	/// </summary>
	/// <remarks>It's used by menu handlers only, so it's a class, not a struct (would be boxed otherwise)</remarks>
	public sealed class TextEditorPosition {
		/// <summary>
		/// Gets the position
		/// </summary>
		public int Position { get; }

		/// <summary>
		/// Gets the virtual spaces
		/// </summary>
		public int VirtualSpaces { get; }

		/// <summary>
		/// true if it's in virtual space
		/// </summary>
		public bool IsInVirtualSpace => VirtualSpaces > 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="point">Position</param>
		public TextEditorPosition(VirtualSnapshotPoint point) {
			if (point.Position.Snapshot is null)
				throw new ArgumentException();
			Position = point.Position.Position;
			VirtualSpaces = point.VirtualSpaces;
		}
	}
}
