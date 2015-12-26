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
using System.IO;
using dnSpy.AsmEditor.Hex;
using dnSpy.Shared.UI.HexEditor;

namespace dnSpy.AsmEditor.SaveModule {
	sealed class SaveHexOptionsVM : SaveOptionsVM {
		public override SaveOptionsType Type {
			get { return SaveOptionsType.Hex; }
		}

		public override object UndoDocument {
			get { return doc; }
		}

		public HexDocument Document {
			get { return doc; }
		}
		readonly AsmEdHexDocument doc;

		public SaveHexOptionsVM(AsmEdHexDocument doc) {
			this.doc = doc;
			this.FileName = doc.Name ?? string.Empty;
		}

		public SaveHexOptionsVM Clone() {
			return CopyTo(new SaveHexOptionsVM(doc));
		}

		public SaveHexOptionsVM CopyTo(SaveHexOptionsVM other) {
			other.FileName = this.FileName;
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
