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
using dnSpy.Contracts.Hex.Files;

namespace dnSpy.AsmEditor.Hex.PE {
	sealed class HexFieldReference {
		public HexBufferFile File { get; }
		public HexVM Structure { get; }
		public HexField Field { get; }

		public HexFieldReference(HexBufferFile file, HexVM structure, HexField field) {
			File = file ?? throw new ArgumentNullException(nameof(file));
			Structure = structure ?? throw new ArgumentNullException(nameof(structure));
			Field = field ?? throw new ArgumentNullException(nameof(field));
		}
	}
}
