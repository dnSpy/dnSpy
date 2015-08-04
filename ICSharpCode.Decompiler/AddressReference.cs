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

namespace ICSharpCode.Decompiler {
	public sealed class AddressReference : IEquatable<AddressReference> {
		public readonly string Filename;
		public readonly bool IsRVA;
		public readonly uint Address;
		public readonly uint Length;

		public AddressReference(string filename, bool isRva, uint addr, uint len) {
			this.Filename = filename;
			this.IsRVA = isRva;
			this.Address = addr;
			this.Length = len;
		}

		public bool Equals(AddressReference other) {
			return other != null &&
				IsRVA == other.IsRVA &&
				Address == other.Address &&
				Length == other.Length &&
				StringComparer.OrdinalIgnoreCase.Equals(Filename, other.Filename);
        }

		public override bool Equals(object obj) {
			return Equals(obj as AddressReference);
		}

		public override int GetHashCode() {
			return (Filename ?? string.Empty).GetHashCode() ^ (IsRVA ? 0 : int.MinValue) ^ (int)Address ^ (int)Length;
		}
	}
}
