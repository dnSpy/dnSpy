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

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// Predefined .NET <see cref="BufferData"/> ids
	/// </summary>
	public static class PredefinedDotNetDataIds {
		/// <summary><see cref="DotNetCor20Data"/></summary>
		public const string Cor20 = nameof(Cor20);

		/// <summary><see cref="DotNetMetadataHeaderData"/></summary>
		public const string MetadataHeader = nameof(MetadataHeader);

		/// <summary><see cref="VirtualArrayData{TData}"/> of bytes (<see cref="ByteData"/>)</summary>
		public const string StrongNameSignature = nameof(StrongNameSignature);

		/// <summary><see cref="DotNetMultiFileResourceHeaderData"/></summary>
		public const string MultiFileResource = nameof(MultiFileResource);
	}
}
