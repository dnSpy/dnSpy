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

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// .NET metadata header
	/// </summary>
	public abstract class DotNetMetadataHeaderData : StructureData {
		const string NAME = "MetadataHeader";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected DotNetMetadataHeaderData(HexBufferSpan span)
			: base(NAME, span) {
		}

		/// <summary>STORAGESIGNATURE.lSignature</summary>
		public abstract StructField<UInt32Data> Signature { get; }
		/// <summary>STORAGESIGNATURE.iMajorVer</summary>
		public abstract StructField<UInt16Data> MajorVersion { get; }
		/// <summary>STORAGESIGNATURE.iMinorVer</summary>
		public abstract StructField<UInt16Data> MinorVersion { get; }
		/// <summary>STORAGESIGNATURE.iExtraData</summary>
		public abstract StructField<UInt32Data> ExtraData { get; }
		/// <summary>STORAGESIGNATURE.iVersionString</summary>
		public abstract StructField<UInt32Data> VersionStringCount { get; }
		/// <summary>STORAGESIGNATURE.VersionString</summary>
		public abstract StructField<StringData> VersionString { get; }

		/// <summary>STORAGEHEADER.fFlags</summary>
		public abstract StructField<ByteFlagsData> Flags { get; }
		/// <summary>STORAGEHEADER.pad</summary>
		public abstract StructField<ByteData> Pad { get; }
		/// <summary>STORAGEHEADER.iStreams</summary>
		public abstract StructField<UInt16Data> StreamCount { get; }

		/// <summary>Streams following STORAGEHEADER.iStreams</summary>
		public abstract StructField<VariableLengthArrayData<DotNetStorageStream>> Streams { get; }
	}
}
