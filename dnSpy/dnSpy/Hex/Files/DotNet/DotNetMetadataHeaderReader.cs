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
using System.Text;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;

namespace dnSpy.Hex.Files.DotNet {
	sealed class DotNetMetadataHeaderReader {
		readonly HexBufferFile file;
		public HexSpan MetadataSpan { get; private set; }
		public HexSpan MetadataHeaderSpan { get; private set; }
		public HexSpan StorageSignatureSpan { get; private set; }
		public HexSpan VersionStringSpan { get; private set; }
		public HexSpan StorageHeaderSpan { get; private set; }
		public int StreamCount { get; private set; }
		public StorageStreamHeader[]? StorageStreamHeaders { get; private set; }

		DotNetMetadataHeaderReader(HexBufferFile file, HexSpan mdSpan) {
			this.file = file ?? throw new ArgumentNullException(nameof(file));
			MetadataSpan = mdSpan;
		}

		public static DotNetMetadataHeaderReader? TryCreate(HexBufferFile file, HexSpan span) {
			if (span.Length < 0x14)
				return null;
			if (!file.Span.Contains(span))
				return null;
			if (file.Buffer.ReadUInt32(span.Start) != 0x424A5342)
				return null;
			var reader = new DotNetMetadataHeaderReader(file, span);
			return reader.Read() ? reader : null;
		}

		bool Read() {
			var pos = MetadataSpan.Start + 0x0C;
			var versionStringLength = file.Buffer.ReadUInt32(pos);
			var endPos = pos + 4 + versionStringLength;
			if (!file.Span.Contains(pos + 4) || !file.Span.Contains(endPos - 1))
				return false;
			VersionStringSpan = HexSpan.FromBounds(pos + 4, endPos);
			StorageSignatureSpan = HexSpan.FromBounds(MetadataSpan.Start, VersionStringSpan.End);
			if (!file.Span.Contains(VersionStringSpan.End + 4))
				return false;
			StorageHeaderSpan = HexSpan.FromBounds(VersionStringSpan.End, VersionStringSpan.End + 4);
			StreamCount = file.Buffer.ReadUInt16(StorageHeaderSpan.End - 2);
			StorageStreamHeaders = new StorageStreamHeader[StreamCount];
			pos = StorageHeaderSpan.End;
			var sb = new StringBuilder();
			var lastStreamOffset = MetadataSpan.Start;
			for (int i = 0; i < StorageStreamHeaders.Length; i++) {
				var stream = ReadStorageStreamHeader(pos, sb);
				if (stream is null)
					return false;
				StorageStreamHeaders[i] = stream.Value;
				pos = stream.Value.Span.End;
				lastStreamOffset = HexPosition.Max(lastStreamOffset, stream.Value.DataSpan.End);
			}
			MetadataHeaderSpan = HexSpan.FromBounds(MetadataSpan.Start, pos);
			MetadataSpan = HexSpan.FromBounds(MetadataSpan.Start, lastStreamOffset);
			return true;
		}

		StorageStreamHeader? ReadStorageStreamHeader(HexPosition position, StringBuilder sb) {
			if (!file.Buffer.Span.Contains(position) || !file.Buffer.Span.Contains(position + 8 - 1))
				return null;
			uint offset = file.Buffer.ReadUInt32(position);
			uint size = file.Buffer.ReadUInt32(position + 4);
			var startPos = MetadataSpan.Start + offset;
			var endPos = startPos + size;
			if (startPos < MetadataSpan.Start || startPos >= MetadataSpan.End || endPos > MetadataSpan.End) {
				startPos = MetadataSpan.Start;
				endPos = MetadataSpan.Start;
			}
			int stringLength = GetStorageStreamStringLength(position + 8, 32);
			if (stringLength < 0)
				return null;
			var end = position + 8 + stringLength;
			if (!file.Span.Contains(end - 1))
				return null;
			return new StorageStreamHeader(GetString(sb, file.Buffer, position + 8, stringLength), HexSpan.FromBounds(position, end), HexSpan.FromBounds(startPos, endPos), stringLength);
		}

		static string GetString(StringBuilder sb, HexBuffer buffer, HexPosition position, int stringLength) {
			sb.Clear();
			for (int i = 0; i < stringLength; i++) {
				var b = buffer.ReadByte(position++);
				if (b == 0)
					break;
				sb.Append((char)b);
			}
			return sb.ToString();
		}

		int GetStorageStreamStringLength(HexPosition position, int maxLen) {
			int i;
			for (i = 0; i < maxLen; i++) {
				if (!file.Span.Contains(position))
					return -1;
				byte b = file.Buffer.ReadByte(position++);
				if (b == 0)
					break;
			}
			return (i + 1 + 3) & ~3;
		}
	}
}
