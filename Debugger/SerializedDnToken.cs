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
using dndbg.Engine;
using dnlib.DotNet;

namespace dnSpy.Debugger {
	struct SerializedDnToken : IEquatable<SerializedDnToken> {
		public SerializedDnModule Module {
			get { return module; }
		}
		/*readonly*/ SerializedDnModule module;

		public uint Token {
			get { return token; }
		}
		readonly uint token;

		public SerializedDnToken(SerializedDnModule module, MDToken mdToken)
			: this(module, mdToken.Raw) {
		}

		public SerializedDnToken(SerializedDnModule module, uint token) {
			this.module = module;
			this.token = token;
		}

		public bool Equals(SerializedDnToken other) {
			return token == other.token &&
					module.Equals(other.module);
		}

		public override bool Equals(object obj) {
			var other = obj as SerializedDnToken?;
			if (other != null)
				return Equals(other.Value);
			return false;
		}

		public override int GetHashCode() {
			return module.GetHashCode() ^ (int)token;
		}

		public override string ToString() {
			return string.Format("{0:X8} {1}", token, module);
		}
	}
}
