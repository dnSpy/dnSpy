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
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class USHeapImpl : USHeap, IDotNetHeap {
		public override DotNetMetadataHeaders Metadata => metadata!;
		DotNetMetadataHeaders? metadata;
		USString[]? usStringInfos;

		public USHeapImpl(HexBufferSpan span)
			: base(span) {
		}

		public override ComplexData? GetStructure(HexPosition position) {
			var info = GetStringInfo(position);
			if (info is not null)
				return new USHeapRecordData(Span.Buffer, info.Value.LengthSpan, info.Value.StringSpan, info.Value.TerminalByteSpan, this);

			return null;
		}

		USString? GetStringInfo(HexPosition position) {
			if (!Span.Contains(position))
				return null;
			var index = GetIndex(position);
			if (index < 0)
				return null;
			Debug2.Assert(usStringInfos is not null);
			return usStringInfos[index];
		}

		void Initialize() {
			if (usStringInfos is not null)
				return;
			usStringInfos = CreateUSStringInfos();
		}

		USString[] CreateUSStringInfos() {
			var list = new List<USString>();
			var pos = Span.Span.Start;
			var end = Span.Span.End;
			var buffer = Span.Buffer;
			while (pos < end) {
				var start = pos;
				var len = ReadCompressedUInt32(ref pos) ?? -1;
				if (len < 0) {
					pos++;
					continue;
				}

				if (pos + len > end) {
					pos++;
					continue;
				}

				var byteLen = len & ~1;
				var stringSpan = new HexSpan(pos, (ulong)byteLen);
				var terminalSpan = HexSpan.FromBounds(stringSpan.End, pos + len);
				list.Add(new USString(HexSpan.FromBounds(start, pos), stringSpan, terminalSpan));
				pos = terminalSpan.End;
			}
			return list.ToArray();
		}

		int GetIndex(HexPosition position) {
			var array = usStringInfos;
			if (array is null) {
				Initialize();
				array = usStringInfos;
				Debug2.Assert(array is not null);
			}
			int lo = 0, hi = array.Length - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var span = array[index].FullSpan;
				if (position < span.Start)
					hi = index - 1;
				else if (position >= span.End)
					lo = index + 1;
				else
					return index;
			}
			return -1;
		}

		void IDotNetHeap.SetMetadata(DotNetMetadataHeaders metadata) => this.metadata = metadata;
	}

	readonly struct USString {
		public HexSpan FullSpan => HexSpan.FromBounds(LengthSpan.Start, TerminalByteSpan.End);
		public HexSpan LengthSpan { get; }
		public HexSpan StringSpan { get; }
		public HexSpan TerminalByteSpan { get; }
		public USString(HexSpan lengthSpan, HexSpan stringSpan, HexSpan terminalByteSpan) {
			if (lengthSpan.End != stringSpan.Start)
				throw new ArgumentOutOfRangeException(nameof(stringSpan));
			if (stringSpan.End != terminalByteSpan.Start)
				throw new ArgumentOutOfRangeException(nameof(stringSpan));
			LengthSpan = lengthSpan;
			StringSpan = stringSpan;
			TerminalByteSpan = terminalByteSpan;
		}
	}
}
