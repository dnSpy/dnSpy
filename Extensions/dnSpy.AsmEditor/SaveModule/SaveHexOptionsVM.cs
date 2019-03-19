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
using System.IO;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.SaveModule {
	sealed class SaveHexOptionsVM : SaveOptionsVM {
		public override SaveOptionsType Type => SaveOptionsType.Hex;
		public override object UndoDocument => buffer;
		public HexBuffer Buffer => buffer;
		readonly HexBuffer buffer;

		public SaveHexOptionsVM(HexBuffer buffer) {
			this.buffer = buffer;
			FileName = buffer.Name ?? string.Empty;
			OriginalFileName = FileName;
		}

		public SaveHexOptionsVM Clone() => CopyTo(new SaveHexOptionsVM(buffer));

		public SaveHexOptionsVM CopyTo(SaveHexOptionsVM other) {
			other.FileName = FileName;
			other.OriginalFileName = OriginalFileName;
			return other;
		}

		protected override string GetExtension(string filename) {
			try {
				return Path.GetExtension(filename);
			}
			catch (ArgumentException) {
			}
			return string.Empty;
		}
	}
}
