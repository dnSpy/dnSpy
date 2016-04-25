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

namespace dnSpy.Shared.HexEditor {
	public struct HexSelection : IEquatable<HexSelection> {
		public ulong From {
			get { return from; }
		}
		readonly ulong from;

		public ulong To {
			get { return to; }
		}
		readonly ulong to;

		public ulong StartOffset {
			get { return Math.Min(From, To); }
		}

		public ulong EndOffset {
			get { return Math.Max(From, To); }
		}

		public HexSelection(ulong from, ulong to) {
			this.from = from;
			this.to = to;
		}

		public static bool operator ==(HexSelection a, HexSelection b) {
			return a.Equals(b);
		}

		public static bool operator !=(HexSelection a, HexSelection b) {
			return !a.Equals(b);
		}

		public bool Equals(HexSelection other) {
			return From == other.From &&
				To == other.To;
		}

		public override bool Equals(object obj) {
			return obj is HexSelection && Equals((HexSelection)obj);
		}

		public override int GetHashCode() {
			return (int)From ^ (int)(From >> 32) ^
				(int)To ^ (int)(To >> 32);
		}
	}
}
