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

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Provides tooltips and references
	/// </summary>
	public abstract class HexFileStructureInfoProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexFileStructureInfoProvider() { }

		/// <summary>
		/// Gets indexes of sub structures or null. The returned array must be sorted. If the array
		/// is empty, every field is a sub structure.
		/// </summary>
		/// <param name="file">File</param>
		/// <param name="structure">Structure</param>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public virtual HexIndexes[] GetSubStructureIndexes(HexBufferFile file, ComplexData structure, HexPosition position) => null;

		/// <summary>
		/// Returns a tooltip or null
		/// </summary>
		/// <param name="file">File</param>
		/// <param name="structure">Structure</param>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public virtual object GetToolTip(HexBufferFile file, ComplexData structure, HexPosition position) => null;

		/// <summary>
		/// Returns a reference or null
		/// </summary>
		/// <param name="file">File</param>
		/// <param name="structure">Structure</param>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public virtual object GetReference(HexBufferFile file, ComplexData structure, HexPosition position) => null;
	}

	/// <summary>
	/// Indexes
	/// </summary>
	public struct HexIndexes {
		/// <summary>
		/// Gets the start index
		/// </summary>
		public int Start { get; }

		/// <summary>
		/// Gets the end index
		/// </summary>
		public int End { get; }

		/// <summary>
		/// true if it's empty
		/// </summary>
		public bool IsEmpty => Start == End;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Start index</param>
		/// <param name="length">Length</param>
		public HexIndexes(int start, int length) {
			if (start < 0)
				throw new ArgumentOutOfRangeException(nameof(start));
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			if (start + length < start)
				throw new ArgumentOutOfRangeException(nameof(length));
			Start = start;
			End = start + length;
		}
	}
}
