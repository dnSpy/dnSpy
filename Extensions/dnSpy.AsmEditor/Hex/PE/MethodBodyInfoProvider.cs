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
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class MethodBodyInfoProvider {
		readonly PEStructureProvider peStructureProvider;
		readonly MetaDataTableVM methodTable;
		readonly MethodBodyPositionAndRid[] methodBodyPositions;
		readonly HexSpan methodBodiesSpan;

		struct MethodBodyPositionAndRid {
			public HexPosition Position { get; }
			public uint Rid { get; }

			public MethodBodyPositionAndRid(HexPosition position, uint rid) {
				Position = position;
				Rid = rid;
			}
		}

		sealed class MethodBodyPositionAndRidComparer : IComparer<MethodBodyPositionAndRid> {
			public static readonly MethodBodyPositionAndRidComparer Instance = new MethodBodyPositionAndRidComparer();
			public int Compare(MethodBodyPositionAndRid x, MethodBodyPositionAndRid y) {
				int c = x.Position.CompareTo(y.Position);
				if (c != 0)
					return c;
				return (int)x.Rid - (int)y.Rid;
			}
		}

		MethodBodyInfoProvider(PEStructureProvider peStructureProvider, MetaDataTableVM methodTable) {
			this.peStructureProvider = peStructureProvider;
			this.methodTable = methodTable;
			methodBodyPositions = CreateMethodBodyPositions();
			methodBodiesSpan = GetMethodBodiesSpan(methodBodyPositions);
		}

		HexSpan GetMethodBodiesSpan(MethodBodyPositionAndRid[] methodBodyPositions) {
			if (methodBodyPositions.Length == 0)
				return default(HexSpan);
			var last = methodBodyPositions[methodBodyPositions.Length - 1];
			var info = TryParseMethodBody(new[] { last.Rid }, last.Position);
			return HexSpan.FromBounds(methodBodyPositions[0].Position, info?.Span.End ?? last.Position);
		}

		MethodBodyPositionAndRid[] CreateMethodBodyPositions() {
			var list = new List<MethodBodyPositionAndRid>((int)methodTable.Rows);
			var recordPos = methodTable.Span.Start;
			var buffer = peStructureProvider.Buffer;
			for (uint rid = 1; rid <= methodTable.Rows; rid++, recordPos += methodTable.TableInfo.RowSize) {
				uint rva = buffer.ReadUInt32(recordPos);
				// This should match the impl in dnlib
				if (rva == 0)
					continue;
				var implAttrs = (MethodImplAttributes)buffer.ReadUInt16(recordPos + 4);
				var codeType = implAttrs & MethodImplAttributes.CodeTypeMask;
				if (codeType != MethodImplAttributes.IL)
					continue;//TODO: Support native methods: MethodImplAttributes.Native

				var methodBodyPosition = peStructureProvider.RvaToBufferPosition(rva);
				list.Add(new MethodBodyPositionAndRid(methodBodyPosition, rid));
			}
			list.Sort(MethodBodyPositionAndRidComparer.Instance);
			return list.ToArray();
		}

		MethodBodyInfo? TryParseMethodBody(IList<uint> rids, HexPosition methodBodyPosition) =>
			new MethodBodyReader(peStructureProvider.Buffer, rids, methodBodyPosition, peStructureProvider.PESpan.End).Read();

		public static MethodBodyInfoProvider TryCreate(PEStructureProvider peStructureProvider) {
			if (peStructureProvider == null)
				throw new ArgumentNullException(nameof(peStructureProvider));
			var tblsStream = peStructureProvider.TablesStream;
			if (tblsStream == null)
				return null;
			var methodTable = tblsStream.MetaDataTables[(int)Table.Method];
			if (methodTable == null)
				return null;

			return new MethodBodyInfoProvider(peStructureProvider, methodTable);
		}

		public MethodBodyInfoAndField? GetMethodBodyInfoAndField(HexPosition position) {
			if (!methodBodiesSpan.Contains(position))
				return null;
			int index = GetStartIndex(position);
			if (index < 0)
				return null;
			var info = methodBodyPositions[index];
			var rids = new List<uint>();
			rids.Add(info.Rid);
			index++;
			while (index < methodBodyPositions.Length && methodBodyPositions[index].Position == info.Position)
				rids.Add(methodBodyPositions[index++].Rid);
			var methodInfo = TryParseMethodBody(rids, info.Position);
			if (methodInfo == null || !methodInfo.Value.Span.Contains(position))
				return null;
			return new MethodBodyInfoAndField(methodInfo.Value, GetFieldInfo(methodInfo.Value, position));
		}

		MethodBodyFieldInfo GetFieldInfo(MethodBodyInfo info, HexPosition position) {
			if (info.HeaderSpan.Contains(position)) {
				if (info.HeaderSpan.Length == 1)
					return new MethodBodyFieldInfo(MethodBodyFieldKind.SmallHeaderCodeSize, info.HeaderSpan);
				int index = (int)(position - info.HeaderSpan.Start).ToUInt64();
				if (index < 2)
					return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeHeaderFlags, HexSpan.FromBounds(info.HeaderSpan.Start, HexPosition.Min(info.HeaderSpan.End, info.HeaderSpan.Start + 2)));
				if (index < 4)
					return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeHeaderMaxStack, HexSpan.FromBounds(info.HeaderSpan.Start + 2, HexPosition.Min(info.HeaderSpan.End, info.HeaderSpan.Start + 4)));
				if (index < 8)
					return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeHeaderCodeSize, HexSpan.FromBounds(info.HeaderSpan.Start + 4, HexPosition.Min(info.HeaderSpan.End, info.HeaderSpan.Start + 8)));
				if (index < 12)
					return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeHeaderLocalVarSigTok, HexSpan.FromBounds(info.HeaderSpan.Start + 8, HexPosition.Min(info.HeaderSpan.End, info.HeaderSpan.Start + 12)));
				return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeHeaderUnknown, HexSpan.FromBounds(info.HeaderSpan.Start + 12, info.HeaderSpan.End));
			}
			else if (info.InstructionsSpan.Contains(position)) {
				//TODO: Return the span of the instruction (including prefixes)
				return new MethodBodyFieldInfo(MethodBodyFieldKind.InstructionBytes, new HexSpan(position, 1));
			}
			else if (info.ExceptionsSpan.Contains(position)) {
				int index = (int)(position - info.ExceptionsSpan.Start).ToUInt64();
				if (info.IsSmallExceptionClauses) {
					if (index < 1)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.SmallExceptionHeaderKind, HexSpan.FromBounds(info.ExceptionsSpan.Start, info.ExceptionsSpan.Start + 1));
					if (index < 2)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.SmallExceptionHeaderDataSize, HexSpan.FromBounds(info.ExceptionsSpan.Start + 1, info.ExceptionsSpan.Start + 2));
					if (index < 4)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.SmallExceptionHeaderPadding, HexSpan.FromBounds(info.ExceptionsSpan.Start + 2, info.ExceptionsSpan.Start + 4));
					int ehFieldIndex = (index - 4) % 12;
					var ehPos = info.ExceptionsSpan.Start + (index - ehFieldIndex);
					if (ehFieldIndex < 2)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.SmallExceptionClauseFlags, HexSpan.FromBounds(ehPos, ehPos + 2));
					if (ehFieldIndex < 4)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.SmallExceptionClauseTryOffset, HexSpan.FromBounds(ehPos + 2, ehPos + 4));
					if (ehFieldIndex < 5)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.SmallExceptionClauseTryLength, HexSpan.FromBounds(ehPos + 4, ehPos + 5));
					if (ehFieldIndex < 7)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.SmallExceptionClauseHandlerOffset, HexSpan.FromBounds(ehPos + 5, ehPos + 7));
					if (ehFieldIndex < 8)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.SmallExceptionClauseHandlerLength, HexSpan.FromBounds(ehPos + 7, ehPos + 8));
					if (ehFieldIndex < 12) {
						var flags = (ExceptionHandlerType)peStructureProvider.Buffer.ReadUInt16(ehPos);
						if (flags == ExceptionHandlerType.Catch)
							return new MethodBodyFieldInfo(MethodBodyFieldKind.SmallExceptionClauseClassToken, HexSpan.FromBounds(ehPos + 8, ehPos + 12));
						if (flags == ExceptionHandlerType.Filter)
							return new MethodBodyFieldInfo(MethodBodyFieldKind.SmallExceptionClauseFilterOffset, HexSpan.FromBounds(ehPos + 8, ehPos + 12));
						return new MethodBodyFieldInfo(MethodBodyFieldKind.SmallExceptionClauseReserved, HexSpan.FromBounds(ehPos + 8, ehPos + 12));
					}
				}
				else {
					if (index < 1)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeExceptionHeaderKind, HexSpan.FromBounds(info.ExceptionsSpan.Start, info.ExceptionsSpan.Start + 1));
					if (index < 4)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeExceptionHeaderDataSize, HexSpan.FromBounds(info.ExceptionsSpan.Start + 1, info.ExceptionsSpan.Start + 4));
					int ehFieldIndex = (index - 4) % 24;
					var ehPos = info.ExceptionsSpan.Start + (index - ehFieldIndex);
					if (ehFieldIndex < 4)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeExceptionClauseFlags, HexSpan.FromBounds(ehPos, ehPos + 4));
					if (ehFieldIndex < 8)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeExceptionClauseTryOffset, HexSpan.FromBounds(ehPos + 4, ehPos + 8));
					if (ehFieldIndex < 12)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeExceptionClauseTryLength, HexSpan.FromBounds(ehPos + 8, ehPos + 12));
					if (ehFieldIndex < 16)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeExceptionClauseHandlerOffset, HexSpan.FromBounds(ehPos + 12, ehPos + 16));
					if (ehFieldIndex < 20)
						return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeExceptionClauseHandlerLength, HexSpan.FromBounds(ehPos + 16, ehPos + 20));
					if (ehFieldIndex < 24) {
						var flags = (ExceptionHandlerType)peStructureProvider.Buffer.ReadUInt16(ehPos);
						if (flags == ExceptionHandlerType.Catch)
							return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeExceptionClauseClassToken, HexSpan.FromBounds(ehPos + 20, ehPos + 24));
						if (flags == ExceptionHandlerType.Filter)
							return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeExceptionClauseFilterOffset, HexSpan.FromBounds(ehPos + 20, ehPos + 24));
						return new MethodBodyFieldInfo(MethodBodyFieldKind.LargeExceptionClauseReserved, HexSpan.FromBounds(ehPos + 20, ehPos + 24));
					}
				}
				return new MethodBodyFieldInfo(MethodBodyFieldKind.ExceptionClausesUnknown, new HexSpan(position, 1));
			}
			return new MethodBodyFieldInfo(MethodBodyFieldKind.Unknown, new HexSpan(position, 1));
		}

		int GetStartIndex(HexPosition position) {
			int index = GetStartIndexCore(position);
			while (index > 0 && methodBodyPositions[index - 1].Position == methodBodyPositions[index].Position)
				index--;
			return index;
		}

		int GetStartIndexCore(HexPosition position) {
			var array = methodBodyPositions;
			int lo = 0, hi = array.Length - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var info = array[index];
				if (position < info.Position)
					hi = index - 1;
				else if (position > info.Position)
					lo = index + 1;
				else
					return index;
			}
			return lo <= array.Length ? lo - 1 : -1;
		}
	}

	enum MethodBodyFieldKind {
		None,
		Unknown,
		SmallHeaderCodeSize,
		LargeHeaderFlags,
		LargeHeaderMaxStack,
		LargeHeaderCodeSize,
		LargeHeaderLocalVarSigTok,
		LargeHeaderUnknown,
		InstructionBytes,
		SmallExceptionHeaderKind,
		SmallExceptionHeaderDataSize,
		SmallExceptionHeaderPadding,
		SmallExceptionClauseFlags,
		SmallExceptionClauseTryOffset,
		SmallExceptionClauseTryLength,
		SmallExceptionClauseHandlerOffset,
		SmallExceptionClauseHandlerLength,
		SmallExceptionClauseClassToken,
		SmallExceptionClauseFilterOffset,
		SmallExceptionClauseReserved,
		LargeExceptionHeaderKind,
		LargeExceptionHeaderDataSize,
		LargeExceptionClauseFlags,
		LargeExceptionClauseTryOffset,
		LargeExceptionClauseTryLength,
		LargeExceptionClauseHandlerOffset,
		LargeExceptionClauseHandlerLength,
		LargeExceptionClauseClassToken,
		LargeExceptionClauseFilterOffset,
		LargeExceptionClauseReserved,
		ExceptionClausesUnknown,
	}

	struct MethodBodyFieldInfo {
		public MethodBodyFieldKind FieldKind { get; }
		public HexSpan Span { get; }
		public MethodBodyFieldInfo(MethodBodyFieldKind fieldKind, HexSpan span) {
			FieldKind = fieldKind;
			Span = span;
		}
	}

	struct MethodBodyInfoAndField {
		public MethodBodyInfo MethodBodyInfo { get; }
		public MethodBodyFieldInfo FieldInfo { get; }

		public MethodBodyInfoAndField(MethodBodyInfo methodBodyInfo, MethodBodyFieldInfo fieldInfo) {
			MethodBodyInfo = methodBodyInfo;
			FieldInfo = fieldInfo;
		}
	}
}
