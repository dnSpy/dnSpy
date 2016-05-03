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
using System.Diagnostics;

namespace dnSpy.Shared.HexEditor {
	[DebuggerDisplay("{Offset} {KindPosition} {Kind}")]
	public struct HexBoxPosition : IEquatable<HexBoxPosition> {
		/// <summary>
		/// Offset in stream
		/// </summary>
		public ulong Offset { get; }

		/// <summary>
		/// Kind of position
		/// </summary>
		public HexBoxPositionKind Kind { get; }

		/// <summary>
		/// Character position within <see cref="Kind"/>
		/// </summary>
		public byte KindPosition { get; }

		public const byte INDEX_HEXBYTE_FIRST = 0;
		public const byte INDEX_HEXBYTE_HI = 0;
		public const byte INDEX_HEXBYTE_LO = 1;
		public const byte INDEX_HEXBYTE_LAST = 1;

		public static HexBoxPosition CreateAscii(ulong offset) => new HexBoxPosition(offset, HexBoxPositionKind.Ascii, 0);
		public static HexBoxPosition CreateByte(ulong offset, int kindPos) => new HexBoxPosition(offset, HexBoxPositionKind.HexByte, kindPos);

		public HexBoxPosition(ulong offset, HexBoxPositionKind kind, int kindPos) {
			this.Offset = offset;
			this.Kind = kind;
			this.KindPosition = (byte)kindPos;
		}

		public HexBoxPosition Create(ulong offset) => new HexBoxPosition(offset, Kind, KindPosition);
		public static bool operator ==(HexBoxPosition a, HexBoxPosition b) => a.Equals(b);
		public static bool operator !=(HexBoxPosition a, HexBoxPosition b) => !a.Equals(b);

		public bool Equals(HexBoxPosition other) {
			return Offset == other.Offset &&
				Kind == other.Kind &&
				KindPosition == other.KindPosition;
		}

		public override bool Equals(object obj) => obj is HexBoxPosition && Equals((HexBoxPosition)obj);

		public override int GetHashCode() {
			return (int)(Offset >> 32) ^ (int)Offset ^
				((int)Kind << 30) ^
				(KindPosition << 22);
		}
	}
}
