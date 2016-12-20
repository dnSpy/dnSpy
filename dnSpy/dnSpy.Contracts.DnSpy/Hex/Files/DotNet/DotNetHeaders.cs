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

using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// .NET headers
	/// </summary>
	public abstract class DotNetHeaders : IBufferFileHeaders {
		/// <summary>
		/// Constructor
		/// </summary>
		protected DotNetHeaders() { }

		/// <summary>
		/// Gets the PE headers
		/// </summary>
		public abstract PeHeaders PeHeaders { get; }

		/// <summary>
		/// Gets the COR20 header
		/// </summary>
		public abstract DotNetCor20Data Cor20 { get; }

		/// <summary>
		/// Gets the metadata header
		/// </summary>
		public abstract DotNetMetadataHeaderData MetadataHeader { get; }
	}
}
