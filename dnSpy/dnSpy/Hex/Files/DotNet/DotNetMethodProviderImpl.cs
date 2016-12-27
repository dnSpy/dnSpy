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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.Hex.Files.DotNet {
	sealed class DotNetMethodProviderImpl : DotNetMethodProvider {
		readonly PeHeaders peHeaders;
		readonly MethodBodyRvaAndRid[] methodBodyRvas;
		readonly HexSpan methodBodiesSpan;
		readonly HexBuffer buffer;
		readonly HexSpan fileSpan;

		struct MethodBodyRvaAndRid {
			// This is an RVA instead of a HexPosition so it's not needed to translate all method
			// bodies' RVAs to HexPositions.
			public uint Rva { get; }
			public uint Rid { get; }

			public MethodBodyRvaAndRid(uint rva, uint rid) {
				Rva = rva;
				Rid = rid;
			}
		}

		sealed class MethodBodyPositionAndRidComparer : IComparer<MethodBodyRvaAndRid> {
			public static readonly MethodBodyPositionAndRidComparer Instance = new MethodBodyPositionAndRidComparer();
			public int Compare(MethodBodyRvaAndRid x, MethodBodyRvaAndRid y) {
				int c = x.Rva.CompareTo(y.Rva);
				if (c != 0)
					return c;
				return (int)x.Rid - (int)y.Rid;
			}
		}

		public DotNetMethodProviderImpl(HexBuffer buffer, HexSpan fileSpan, PeHeaders peHeaders, TablesHeap tablesHeap) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (peHeaders == null)
				throw new ArgumentNullException(nameof(peHeaders));
			if (tablesHeap == null)
				throw new ArgumentNullException(nameof(tablesHeap));
			this.buffer = buffer;
			this.fileSpan = fileSpan;
			this.peHeaders = peHeaders;
			methodBodyRvas = CreateMethodBodyRvas(tablesHeap.MDTables[(int)Table.Method]);
			methodBodiesSpan = GetMethodBodiesSpan(methodBodyRvas);
		}

		HexSpan GetMethodBodiesSpan(MethodBodyRvaAndRid[] methodBodyRvas) {
			if (methodBodyRvas.Length == 0)
				return default(HexSpan);
			int index = methodBodyRvas.Length - 1;
			var last = methodBodyRvas[index];
			var info = ParseMethodBody(index + 1, new[] { last.Rid }, peHeaders.RvaToBufferPosition(last.Rva));
			return HexSpan.FromBounds(peHeaders.RvaToBufferPosition(methodBodyRvas[0].Rva), info.Span.End);
		}

		MethodBodyRvaAndRid[] CreateMethodBodyRvas(MDTable methodTable) {
			var list = new List<MethodBodyRvaAndRid>((int)methodTable.Rows);
			var recordPos = methodTable.Span.Start;
			var buffer = this.buffer;
			for (uint rid = 1; rid <= methodTable.Rows; rid++, recordPos += methodTable.TableInfo.RowSize) {
				uint rva = buffer.ReadUInt32(recordPos);
				// This should match the impl in dnlib
				if (rva == 0)
					continue;
				var implAttrs = buffer.ReadUInt16(recordPos + 4);
				const ushort CodeTypeMask = 3, IL = 0;
				var codeType = implAttrs & CodeTypeMask;
				if (codeType != IL)
					continue;//TODO: Support native methods: MethodImplAttributes.Native = 1
				list.Add(new MethodBodyRvaAndRid(rva, rid));
			}
			list.Sort(MethodBodyPositionAndRidComparer.Instance);
			return list.ToArray();
		}

		MethodBodyInfo ParseMethodBody(int nextMethodIndex, IList<uint> tokens, HexPosition methodBodyPosition) {
			uint maxMethodBodyEndRva;
			HexPosition endPos;
			if (nextMethodIndex >= methodBodyRvas.Length) {
				maxMethodBodyEndRva = (uint)Math.Min(uint.MaxValue, (ulong)methodBodyRvas[methodBodyRvas.Length - 1].Rva + 1);
				endPos = fileSpan.End;
			}
			else {
				maxMethodBodyEndRva = methodBodyRvas[nextMethodIndex].Rva;
				endPos = HexPosition.Min(fileSpan.End, peHeaders.RvaToBufferPosition(maxMethodBodyEndRva));
			}
			if (endPos < methodBodyPosition)
				endPos = methodBodyPosition;
			var info = new MethodBodyReader(buffer, tokens, methodBodyPosition, endPos).Read();
			if (info != null)
				return info.Value;

			// The file could be obfuscated (encrypted methods), assume the method ends at the next method body RVA
			endPos = HexPosition.Min(fileSpan.End, peHeaders.RvaToBufferPosition(maxMethodBodyEndRva));
			if (endPos < methodBodyPosition)
				endPos = methodBodyPosition;
			return new MethodBodyInfo(tokens, HexSpan.FromBounds(methodBodyPosition, endPos), HexSpan.FromBounds(endPos, endPos), default(HexSpan), MethodBodyInfoFlags.Invalid);
		}

		public override bool IsMethodPosition(HexPosition position) => methodBodiesSpan.Contains(position);

		public override DotNetMethodBody GetMethodBody(HexPosition position) {
			if (!IsMethodPosition(position))
				return null;

			int index = GetStartIndex(position);
			if (index < 0)
				return null;
			var info = methodBodyRvas[index];
			var tokens = new List<uint>();
			tokens.Add(0x06000000 + info.Rid);
			index++;
			while (index < methodBodyRvas.Length && methodBodyRvas[index].Rva == info.Rva)
				tokens.Add(0x06000000 + methodBodyRvas[index++].Rid);
			var methodInfo = ParseMethodBody(index, tokens, peHeaders.RvaToBufferPosition(info.Rva));
			if (!methodInfo.Span.Contains(position))
				return null;

			var methodSpan = new HexBufferSpan(buffer, methodInfo.Span);
			var roTokens = new ReadOnlyCollection<uint>(tokens);
			if (methodInfo.IsInvalid)
				return new InvalidMethodBodyImpl(methodSpan, roTokens);
			if (methodInfo.HeaderSpan.Length == 1)
				return new TinyMethodBodyImpl(methodSpan, roTokens);
			return new FatMethodBodyImpl(methodSpan, roTokens, methodInfo.InstructionsSpan, methodInfo.ExceptionsSpan, !methodInfo.IsSmallExceptionClauses);
		}

		int GetStartIndex(HexPosition position) {
			int index = GetStartIndexCore(peHeaders.BufferPositionToRva(position));
			while (index > 0 && methodBodyRvas[index - 1].Rva == methodBodyRvas[index].Rva)
				index--;
			return index;
		}

		int GetStartIndexCore(uint rva) {
			var array = methodBodyRvas;
			int lo = 0, hi = array.Length - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var info = array[index];
				if (rva < info.Rva)
					hi = index - 1;
				else if (rva > info.Rva)
					lo = index + 1;
				else
					return index;
			}
			return lo <= array.Length ? lo - 1 : -1;
		}
	}
}
