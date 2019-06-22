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
		/// Called before any other method, but since this method is allowed to call <see cref="HexBufferFile.GetStructure(string)"/>,
		/// the other methods could get called before this instance's <see cref="Initialize"/> method has been called.
		/// 
		/// The method returns false if this instance should be removed (eg. the file isn't supported).
		/// 
		/// This method is allowed to call <see cref="HexBufferFile.GetStructure(string)"/> and <see cref="HexBufferFile.GetHeaders{THeaders}"/>
		/// but should make sure that any provider it depends on has already been initialized (eg. add a
		/// <see cref="VSUTIL.OrderAttribute"/> on your <see cref="StructureProviderFactory"/> class)
		/// </summary>
		/// <returns></returns>
		public abstract bool Initialize();

		/// <summary>
		/// Returns a structure at <paramref name="position"/> or null
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract ComplexData? GetStructure(HexPosition position);

		/// <summary>
		/// Returns a structure or null
		/// </summary>
		/// <param name="id">Id, see eg. <see cref="PE.PredefinedPeDataIds"/></param>
		/// <returns></returns>
		public abstract ComplexData? GetStructure(string id);

		/// <summary>
		/// Returns headers or null. This method is called before <see cref="BufferFileHeadersProvider.GetHeaders{THeader}"/>
		/// </summary>
		/// <returns></returns>
		public virtual THeader? GetHeaders<THeader>() where THeader : class, IBufferFileHeaders => null;
	}
}
