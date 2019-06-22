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
using System.Collections.Generic;
using dnlib.DotNet.MD;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	sealed class BlobMDHeap : MDHeap {
		readonly MetadataEditor mdEditor;
		readonly BlobStream blobStream;
		uint currentOffset;
		readonly List<byte[]> newData;

		public string Name => blobStream.Name;
		public bool IsBig => currentOffset > 0xFFFF;
		internal BlobStream BlobStream => blobStream;
		internal List<byte[]> NewData => newData;

		public BlobMDHeap(MetadataEditor mdEditor, BlobStream blobStream) {
			this.mdEditor = mdEditor ?? throw new ArgumentNullException(nameof(mdEditor));
			this.blobStream = blobStream ?? throw new ArgumentNullException(nameof(blobStream));
			currentOffset = blobStream.StreamLength;
			newData = new List<byte[]>();
		}

		public uint Create(byte[]? data) {
			if (currentOffset == 0) {
				newData.Add(new byte[1]);
				currentOffset++;
			}
			if (data is null || data.Length == 0)
				return 0;
			var lengthBytes = GetLengthBytes((uint)data.Length);
			newData.Add(lengthBytes);
			newData.Add(data);
			var res = currentOffset;
			currentOffset += (uint)(lengthBytes.Length + data.Length);
			return res;
		}

		byte[] GetLengthBytes(uint value) {
			if (value <= 0x7F)
				return new byte[1] { (byte)value };

			if (value <= 0x3FFF) {
				return new byte[2] {
					(byte)((value >> 8) | 0x80),
					(byte)value,
				};
			}

			if (value <= 0x1FFFFFFF) {
				return new byte[4] {
					(byte)((value >> 24) | 0xC0),
					(byte)(value >> 16),
					(byte)(value >> 8),
					(byte)value,
				};
			}

			throw new ArgumentOutOfRangeException(nameof(value));
		}

		public override bool MustRewriteHeap() => newData.Count > 0;
		public override bool ExistsInMetadata => !(blobStream.StreamHeader is null);
	}
}
