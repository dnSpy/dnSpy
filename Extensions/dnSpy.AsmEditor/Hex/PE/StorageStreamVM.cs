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

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet.MD;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class StorageStreamVM : HexVM {
		public override string Name => "STORAGESTREAM";

		public StorageStreamType StorageStreamType { get; }
		public int StreamNumber { get; }
		public UInt32HexField IOffsetVM { get; }
		public UInt32HexField ISizeVM { get; }
		public StringHexField RCNameVM { get; }
		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields;

		public StorageStreamVM(HexBuffer buffer, DotNetStream knownStream, int streamNumber, HexPosition startOffset, int stringLen)
			: base(new HexSpan(startOffset, (ulong)(8 + stringLen))) {
			StorageStreamType = GetStorageStreamType(knownStream);
			StreamNumber = streamNumber;
			IOffsetVM = new UInt32HexField(buffer, Name, "iOffset", startOffset + 0);
			ISizeVM = new UInt32HexField(buffer, Name, "iSize", startOffset + 4);
			RCNameVM = new StringHexField(buffer, Name, "rcName", startOffset + 8, Encoding.ASCII, stringLen);

			hexFields = new HexField[] {
				IOffsetVM,
				ISizeVM,
				RCNameVM,
			};
		}

		static StorageStreamType GetStorageStreamType(DotNetStream stream) {
			if (stream == null)
				return StorageStreamType.None;
			if (stream is StringsStream)
				return StorageStreamType.Strings;
			if (stream is USStream)
				return StorageStreamType.US;
			if (stream is BlobStream)
				return StorageStreamType.Blob;
			if (stream is GuidStream)
				return StorageStreamType.Guid;
			if (stream is TablesStream)
				return StorageStreamType.Tables;
			if (stream.Name == "#Pdb")
				return StorageStreamType.Pdb;
			if (stream.Name == "#!")
				return StorageStreamType.HotHeap;
			Debug.Fail(string.Format("Shouldn't be here when stream is a known stream type: {0}", stream.GetType()));
			return StorageStreamType.None;
		}
	}
}
