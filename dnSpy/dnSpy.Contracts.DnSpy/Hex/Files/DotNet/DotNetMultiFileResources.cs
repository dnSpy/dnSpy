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

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// Present if the file is a .NET multi-file resource file
	/// </summary>
	public abstract class DotNetMultiFileResources : IBufferFileHeaders {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="file">File</param>
		protected DotNetMultiFileResources(HexBufferFile file) => File = file ?? throw new ArgumentNullException(nameof(file));

		/// <summary>
		/// Gets the file
		/// </summary>
		public HexBufferFile File { get; }

		/// <summary>
		/// Position of data section
		/// </summary>
		public abstract HexPosition DataSectionPosition { get; }

		/// <summary>
		/// Gets the header
		/// </summary>
		public abstract DotNetMultiFileResourceHeaderData Header { get; }

		/// <summary>
		/// Returns a structure at <paramref name="position"/> or null
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract ComplexData? GetStructure(HexPosition position);
	}
}
