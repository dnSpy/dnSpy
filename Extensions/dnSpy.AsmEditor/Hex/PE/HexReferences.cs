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
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.PE {
	abstract class HexReference {
		public HexBuffer Buffer { get; }

		protected HexReference(HexBuffer buffer) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			Buffer = buffer;
		}

		public abstract override bool Equals(object obj);
		public abstract override int GetHashCode();
	}

	sealed class HexFieldReference : HexReference {
		public HexField Field { get; }

		public HexFieldReference(HexBuffer buffer, HexField field)
			: base(buffer) {
			if (field == null)
				throw new ArgumentNullException(nameof(field));
			Field = field;
		}

		public override bool Equals(object obj) {
			var other = obj as HexFieldReference;
			return other != null &&
				Buffer == other.Buffer &&
				Field == other.Field;
		}

		public override int GetHashCode() => Buffer.GetHashCode() ^ Field.GetHashCode();
	}

	sealed class HexMethodReference : HexReference {
		public MDToken Token { get; }
		public uint? Offset { get; }

		public HexMethodReference(HexBuffer buffer, uint rid, uint? offset)
			: base(buffer) {
			if (rid == 0)
				throw new ArgumentOutOfRangeException(nameof(rid));
			Token = new MDToken(Table.Method, rid);
			Offset = offset;
		}

		public override bool Equals(object obj) {
			var other = obj as HexMethodReference;
			return other != null &&
				Buffer == other.Buffer &&
				Token == other.Token &&
				Offset == other.Offset;
		}

		public override int GetHashCode() => Buffer.GetHashCode() ^ Token.GetHashCode() ^ (int)(Offset ?? 0);
	}
}
