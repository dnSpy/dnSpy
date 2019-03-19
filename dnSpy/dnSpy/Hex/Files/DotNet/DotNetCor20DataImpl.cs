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
using System.Collections.ObjectModel;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.Hex.Files.DotNet {
	sealed class DotNetCor20DataImpl : DotNetCor20Data {
		public override StructField<UInt32Data> Cb { get; }
		public override StructField<UInt16Data> MajorRuntimeVersion { get; }
		public override StructField<UInt16Data> MinorRuntimeVersion { get; }
		public override StructField<DataDirectoryData> Metadata { get; }
		public override StructField<UInt32FlagsData> Flags { get; }
		public override StructField<UInt32Data> EntryPointTokenOrRVA { get; }
		public override StructField<DataDirectoryData> Resources { get; }
		public override StructField<DataDirectoryData> StrongNameSignature { get; }
		public override StructField<DataDirectoryData> CodeManagerTable { get; }
		public override StructField<DataDirectoryData> VTableFixups { get; }
		public override StructField<DataDirectoryData> ExportAddressTableJumps { get; }
		public override StructField<DataDirectoryData> ManagedNativeHeader { get; }

		protected override BufferField[] Fields { get; }

		static readonly ReadOnlyCollection<FlagInfo> flagsFlagInfos = new ReadOnlyCollection<FlagInfo>(new FlagInfo[] {
			new FlagInfo(0x00000001, "ILONLY"),
			new FlagInfo(0x00000002, "32BITREQUIRED"),
			new FlagInfo(0x00000004, "IL_LIBRARY"),
			new FlagInfo(0x00000008, "STRONGNAMESIGNED"),
			new FlagInfo(0x00000010, "NATIVE_ENTRYPOINT"),
			new FlagInfo(0x00010000, "TRACKDEBUGDATA"),
			new FlagInfo(0x00020000, "32BITPREFERRED"),
		});

		DotNetCor20DataImpl(HexBufferSpan span)
			: base(span) {
			var buffer = span.Buffer;
			var pos = span.Start.Position;
			Cb = new StructField<UInt32Data>("cb", new UInt32Data(buffer, pos));
			MajorRuntimeVersion = new StructField<UInt16Data>("MajorRuntimeVersion", new UInt16Data(buffer, pos + 4));
			MinorRuntimeVersion = new StructField<UInt16Data>("MinorRuntimeVersion", new UInt16Data(buffer, pos + 6));
			Metadata = new StructField<DataDirectoryData>("MetaData", new DataDirectoryData(buffer, pos + 8));
			Flags = new StructField<UInt32FlagsData>("Flags", new UInt32FlagsData(buffer, pos + 0x10, flagsFlagInfos));
			EntryPointTokenOrRVA = new StructField<UInt32Data>("EntryPointTokenOrRVA", new UInt32Data(buffer, pos + 0x14));
			Resources = new StructField<DataDirectoryData>("Resources", new DataDirectoryData(buffer, pos + 0x18));
			StrongNameSignature = new StructField<DataDirectoryData>("StrongNameSignature", new DataDirectoryData(buffer, pos + 0x20));
			CodeManagerTable = new StructField<DataDirectoryData>("CodeManagerTable", new DataDirectoryData(buffer, pos + 0x28));
			VTableFixups = new StructField<DataDirectoryData>("VTableFixups", new DataDirectoryData(buffer, pos + 0x30));
			ExportAddressTableJumps = new StructField<DataDirectoryData>("ExportAddressTableJumps", new DataDirectoryData(buffer, pos + 0x38));
			ManagedNativeHeader = new StructField<DataDirectoryData>("ManagedNativeHeader", new DataDirectoryData(buffer, pos + 0x40));
			Fields = new StructField[] {
				Cb,
				MajorRuntimeVersion,
				MinorRuntimeVersion,
				Metadata,
				Flags,
				EntryPointTokenOrRVA,
				Resources,
				StrongNameSignature,
				CodeManagerTable,
				VTableFixups,
				ExportAddressTableJumps,
				ManagedNativeHeader,
			};
		}

		public static DotNetCor20Data TryCreate(HexBufferFile file, HexPosition position) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (!file.Span.Contains(position) || !file.Span.Contains(position + 0x48 - 1))
				return null;
			return new DotNetCor20DataImpl(new HexBufferSpan(file.Buffer, new HexSpan(position, 0x48)));
		}
	}
}
