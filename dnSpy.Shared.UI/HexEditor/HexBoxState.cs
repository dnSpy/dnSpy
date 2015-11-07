/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace dnSpy.Shared.UI.HexEditor {
	public sealed class HexBoxState : IEquatable<HexBoxState> {
		public ulong TopOffset;
		public int Column;
		public ulong StartOffset;
		public ulong EndOffset;
		public HexBoxPosition CaretPosition;
		public HexSelection? Selection;

		public HexBoxState() {
		}

		public HexBoxState(HexBoxState other) {
			this.TopOffset = other.TopOffset;
			this.Column = other.Column;
			this.StartOffset = other.StartOffset;
			this.EndOffset = other.EndOffset;
			this.CaretPosition = other.CaretPosition;
			this.Selection = other.Selection;
		}

		public static bool operator ==(HexBoxState a, HexBoxState b) {
			if ((object)a == b)
				return true;
			if ((object)a == null || (object)b == null)
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(HexBoxState a, HexBoxState b) {
			return !(a == b);
		}

		public bool Equals(HexBoxState other) {
			if ((object)other == null)
				return false;
			return TopOffset == other.TopOffset &&
				Column == other.Column &&
				StartOffset == other.StartOffset &&
				EndOffset == other.EndOffset &&
				CaretPosition == other.CaretPosition &&
				Selection == other.Selection;
		}

		public override bool Equals(object obj) {
			return Equals(obj as HexBoxState);
		}

		public override int GetHashCode() {
			return (int)TopOffset ^ (int)(TopOffset >> 32) ^
			Column ^
			(int)StartOffset ^ (int)(StartOffset >> 32) ^
			(int)EndOffset ^ (int)(EndOffset >> 32) ^
			CaretPosition.GetHashCode() ^
			Selection.GetHashCode();
		}
	}
}
