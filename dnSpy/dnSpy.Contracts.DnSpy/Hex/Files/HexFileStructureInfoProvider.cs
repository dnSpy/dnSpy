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
		/// Gets indexes of sub structures or null. The returned array must be sorted, an example
		/// is <c>0, 3, 10</c> if <paramref name="structure"/> contains 42 fields. If the array
		/// is empty, every field is a sub structure.
		/// </summary>
		/// <param name="file">File</param>
		/// <param name="position">Position</param>
		/// <param name="structure">Structure</param>
		/// <returns></returns>
		public virtual int[] GetSubStructureIndexes(HexBufferFile file, HexPosition position, ComplexData structure) => null;

		/// <summary>
		/// Returns a tooltip or null
		/// </summary>
		/// <param name="file">File</param>
		/// <param name="position">Position</param>
		/// <param name="structure">Structure</param>
		/// <returns></returns>
		public virtual object GetToolTip(HexBufferFile file, HexPosition position, ComplexData structure) => null;

		/// <summary>
		/// Returns a reference or null
		/// </summary>
		/// <param name="file">File</param>
		/// <param name="position">Position</param>
		/// <param name="structure">Structure</param>
		/// <returns></returns>
		public virtual object GetReference(HexBufferFile file, HexPosition position, ComplexData structure) => null;
	}
}
