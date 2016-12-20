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

using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Provides <see cref="ComplexData"/> instances
	/// </summary>
	public abstract class StructureProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected StructureProvider() { }

		/// <summary>
		/// Called before any other method. This method is allowed to call <see cref="HexBufferFile.GetStructure(string)"/>
		/// but should make sure that any provider it depends on has already been initialized (eg. add a
		/// <see cref="VSUTIL.OrderAttribute"/> on your <see cref="StructureProviderFactory"/> class)
		/// </summary>
		public abstract void Initialize();

		/// <summary>
		/// Returns a top level structure at <paramref name="position"/> or null
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract ComplexData GetStructure(HexPosition position);

		/// <summary>
		/// Returns a structure or null
		/// </summary>
		/// <param name="id">Id, see eg. <see cref="PE.PredefinedPeDataIds"/></param>
		/// <returns></returns>
		public abstract ComplexData GetStructure(string id);
	}
}
