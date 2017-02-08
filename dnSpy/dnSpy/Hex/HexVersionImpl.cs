/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Hex;

namespace dnSpy.Hex {
	sealed class HexVersionImpl : HexVersion {
		public override HexBuffer Buffer { get; }
		public override int VersionNumber { get; }
		public override int ReiteratedVersionNumber { get; }

		public override NormalizedHexChangeCollection Changes => changes;
		public override HexVersion Next => next;

		NormalizedHexChangeCollection changes;
		HexVersion next;

		public HexVersionImpl(HexBuffer buffer, int versionNumber, int reiteratedVersionNumber) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			Buffer = buffer;
			VersionNumber = versionNumber;
			ReiteratedVersionNumber = reiteratedVersionNumber;
		}

		public HexVersionImpl SetChanges(IList<HexChange> changes, int? reiteratedVersionNumber = null) {
			var normalizedChanges = NormalizedHexChangeCollection.Create(changes);
			if (reiteratedVersionNumber == null)
				reiteratedVersionNumber = changes.Count == 0 ? ReiteratedVersionNumber : VersionNumber + 1;
			var newVersion = new HexVersionImpl(Buffer, VersionNumber + 1, reiteratedVersionNumber.Value);
			this.changes = normalizedChanges;
			next = newVersion;
			return newVersion;
		}

		public override string ToString() => $"V{VersionNumber} (r{ReiteratedVersionNumber})";
	}
}
