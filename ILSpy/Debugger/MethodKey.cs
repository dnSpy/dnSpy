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
using dndbg.Engine;
using dnlib.DotNet;

namespace dnSpy.Debugger {
	public struct MethodKey : IEquatable<MethodKey> {
		readonly uint token;
		/*readonly*/ SerializedDnModule module;

		public SerializedDnModule Module {
			get { return module; }
		}

		public uint Token {
			get { return token; }
		}

		public static MethodKey Create(uint token, SerializedDnModule module) {
			return new MethodKey(token, module);
		}

		public static MethodKey? Create(IMemberRef member) {
			if (member == null)
				return null;
			return Create(member.MDToken.Raw, member.Module);
		}

		public static MethodKey? Create(uint token, IOwnerModule ownerModule) {
			if (ownerModule == null)
				return null;
			return Create(token, ownerModule.Module);
		}

		public static MethodKey? Create(uint token, ModuleDef module) {
			if (module == null || string.IsNullOrEmpty(module.Location))
				return null;
			return new MethodKey(token, module);
		}

		MethodKey(uint token, SerializedDnModule module) {
			this.module = module;
			this.token = token;
		}

		MethodKey(uint token, ModuleDef module) {
			this.token = token;
			if (string.IsNullOrEmpty(module.Location))
				throw new ArgumentException("Module has no path");
			this.module = new SerializedDnModule(Path.GetFullPath(module.Location));
		}

		public static bool operator ==(MethodKey a, MethodKey b) {
			return a.Equals(b);
		}

		public static bool operator !=(MethodKey a, MethodKey b) {
			return !a.Equals(b);
		}

		public bool Equals(MethodKey other) {
			if (token != other.token)
				return false;
			return module == other.module;
		}

		public override bool Equals(object obj) {
			if (!(obj is MethodKey))
				return false;
			return Equals((MethodKey)obj);
		}

		public override int GetHashCode() {
			return (int)token ^ module.GetHashCode();
		}

		public override string ToString() {
			return string.Format("{0:X8} {1}", token, module);
		}
	}
}
