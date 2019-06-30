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

using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// .NET headers, present if the COR20 header exists in a PE file. The .NET metadata could still be null.
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
		/// Gets the .NET metadata-only headers or null if none
		/// </summary>
		public abstract DotNetMetadataHeaders? MetadataHeaders { get; }

		/// <summary>
		/// Gets the strong name signature or null if none
		/// </summary>
		public abstract VirtualArrayData<ByteData>? StrongNameSignature { get; }

		/// <summary>
		/// Gets the method provider
		/// </summary>
		public abstract DotNetMethodProvider MethodProvider { get; }

		/// <summary>
		/// Gets the .NET resource provider
		/// </summary>
		public abstract DotNetResourceProvider ResourceProvider { get; }
	}
}
