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

using System.Collections.Generic;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class StorageStreamVM : HexVM {
		public override string Name { get; }

		public DotNetHeapKind HeapKind { get; }
		public int StreamNumber { get; }
		public UInt32HexField IOffsetVM { get; }
		public UInt32HexField ISizeVM { get; }
		public StringHexField RCNameVM { get; }
		public override IEnumerable<HexField> HexFields => hexFields;
		readonly HexField[] hexFields;

		public StorageStreamVM(HexBuffer buffer, DotNetHeap heap, DotNetStorageStream storageStream, int streamNumber)
			: base(storageStream.Span) {
			Name = storageStream.Name;
			HeapKind = heap.HeapKind;
			StreamNumber = streamNumber;
			IOffsetVM = new UInt32HexField(storageStream.Offset);
			ISizeVM = new UInt32HexField(storageStream.Size);
			RCNameVM = new StringHexField(storageStream.StreamName);

			hexFields = new HexField[] {
				IOffsetVM,
				ISizeVM,
				RCNameVM,
			};
		}
	}
}
