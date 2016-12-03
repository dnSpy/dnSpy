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

using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Hex.Nodes {
	abstract class HexVM : ViewModelBase {
		public abstract string Name { get; }
		public abstract IEnumerable<HexField> HexFields { get; }
		public object Owner { get; }

		protected HexVM(object owner) {
			Debug.Assert(owner != null);
			Owner = owner;
		}

		public virtual void OnBufferChanged(NormalizedHexChangeCollection changes) {
			foreach (var field in HexFields)
				field.OnBufferChanged(changes);
		}
	}
}
