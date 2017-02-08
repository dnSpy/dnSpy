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

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Provides tooltips and references
	/// </summary>
	public abstract class HexFileStructureInfoService {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexFileStructureInfoService() { }

		/// <summary>
		/// Gets indexes of sub structures or null. The returned array must be sorted. If the array
		/// is empty, every field is a sub structure.
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract HexIndexes[] GetSubStructureIndexes(HexPosition position);

		/// <summary>
		/// Returns a tooltip or null
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract object GetToolTip(HexPosition position);

		/// <summary>
		/// Returns a reference or null
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract object GetReference(HexPosition position);

		/// <summary>
		/// Returns the span the field value references or null. The span can be empty.
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract HexSpan? GetFieldReferenceSpan(HexPosition position);
	}
}
