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

using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class LocalOptions {
		public TypeSig Type;
		public string Name;
		public int PdbAttributes;

		public LocalOptions() {
		}

		public LocalOptions(Local local) {
			this.Type = local.Type;
			this.Name = local.Name;
			this.PdbAttributes = local.PdbAttributes;
		}

		public Local CopyTo(Local local) {
			local.Type = this.Type;
			local.Name = this.Name;
			local.PdbAttributes = this.PdbAttributes;
			return local;
		}

		public Local Create() {
			return CopyTo(new Local(null));
		}
	}
}
