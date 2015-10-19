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

namespace dnSpy.AsmEditor {
	//TODO: This object is attached to AsmEdHexDocument and DnSpyFile. The UndoCommandManager should
	//		internally store this state so this class (and AsmEdHexDocument) can be removed.
	sealed class UndoObject : IUndoObject {
		public bool IsDirty { get; set; }
		public int SavedCommand { get; set; }
		public object Value { get; set; }

		public UndoObject() {
		}

		public UndoObject(object value) {
			this.Value = value;
		}
	}
}
