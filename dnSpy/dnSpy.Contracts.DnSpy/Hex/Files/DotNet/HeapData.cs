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

using System;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// #GUID heap record data
	/// </summary>
	public sealed class GuidHeapRecordData : GuidData {
		/// <summary>
		/// Gets the heap
		/// </summary>
		public GUIDHeap Heap { get; }

		/// <summary>
		/// Gets the GUID index (1-based)
		/// </summary>
		public uint Index { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		/// <param name="heap">Owner heap</param>
		/// <param name="index">Guid index (1-based)</param>
		public GuidHeapRecordData(HexBuffer buffer, HexPosition position, GUIDHeap heap, uint index)
			: base(buffer, position) {
			if (heap == null)
				throw new ArgumentNullException(nameof(heap));
			if (index == 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			Heap = heap;
			Index = index;
		}
	}
}
