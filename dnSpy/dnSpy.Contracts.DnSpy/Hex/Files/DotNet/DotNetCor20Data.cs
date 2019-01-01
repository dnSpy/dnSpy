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
	/// COR20 header
	/// </summary>
	public abstract class DotNetCor20Data : StructureData {
		const string NAME = "IMAGE_COR20_HEADER";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected DotNetCor20Data(HexBufferSpan span)
			: base(NAME, span) {
		}

		/// <summary>IMAGE_COR20_HEADER.cb</summary>
		public abstract StructField<UInt32Data> Cb { get; }
		/// <summary>IMAGE_COR20_HEADER.MajorRuntimeVersion</summary>
		public abstract StructField<UInt16Data> MajorRuntimeVersion { get; }
		/// <summary>IMAGE_COR20_HEADER.MinorRuntimeVersion</summary>
		public abstract StructField<UInt16Data> MinorRuntimeVersion { get; }
		/// <summary>IMAGE_COR20_HEADER.MetaData</summary>
		public abstract StructField<DataDirectoryData> Metadata { get; }
		/// <summary>IMAGE_COR20_HEADER.Flags</summary>
		public abstract StructField<UInt32FlagsData> Flags { get; }
		/// <summary>IMAGE_COR20_HEADER.EntryPointToken / IMAGE_COR20_HEADER.EntryPointRVA</summary>
		public abstract StructField<UInt32Data> EntryPointTokenOrRVA { get; }
		/// <summary>IMAGE_COR20_HEADER.Resources</summary>
		public abstract StructField<DataDirectoryData> Resources { get; }
		/// <summary>IMAGE_COR20_HEADER.StrongNameSignature</summary>
		public abstract StructField<DataDirectoryData> StrongNameSignature { get; }
		/// <summary>IMAGE_COR20_HEADER.CodeManagerTable</summary>
		public abstract StructField<DataDirectoryData> CodeManagerTable { get; }
		/// <summary>IMAGE_COR20_HEADER.VTableFixups</summary>
		public abstract StructField<DataDirectoryData> VTableFixups { get; }
		/// <summary>IMAGE_COR20_HEADER.ExportAddressTableJumps</summary>
		public abstract StructField<DataDirectoryData> ExportAddressTableJumps { get; }
		/// <summary>IMAGE_COR20_HEADER.ManagedNativeHeader</summary>
		public abstract StructField<DataDirectoryData> ManagedNativeHeader { get; }
	}
}
