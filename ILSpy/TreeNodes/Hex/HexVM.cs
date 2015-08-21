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

using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.AsmEditor;

namespace dnSpy.TreeNodes.Hex {
	abstract class HexVM : ViewModelBase {
		public abstract string Name { get; }
		public abstract IEnumerable<HexField> HexFields { get; }

		public object Owner {
			get { return owner; }
		}
		readonly object owner;

		protected HexVM(object owner) {
			Debug.Assert(owner != null);
			this.owner = owner;
		}

		public virtual void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			foreach (var field in HexFields)
				field.OnDocumentModified(modifiedStart, modifiedEnd);
		}
	}
}
