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
using dnlib.DotNet;

namespace dnSpy.Debugger {
	public struct MethodKey : IEquatable<MethodKey> {
		readonly int token;
		readonly string moduleFullPath;

		public string ModuleFullPath {
			get { return moduleFullPath; }
		}

		public int Token {
			get { return token; }
		}

		public MethodKey(int token, string moduleFullPath) {
			this.token = token;
			this.moduleFullPath = moduleFullPath;
		}

		public static MethodKey? Create(IMemberRef member) {
			if (member == null)
				return null;
			return Create(member.MDToken.ToInt32(), member.Module);
		}

		public static MethodKey? Create(int token, IOwnerModule ownerModule) {
			if (ownerModule == null)
				return null;
			return Create(token, ownerModule.Module);
		}

		public static MethodKey? Create(int token, ModuleDef module) {
			if (module == null || string.IsNullOrEmpty(module.Location))
				return null;
			return new MethodKey(token, module);
		}

		MethodKey(int token, ModuleDef module) {
			this.token = token;
			if (string.IsNullOrEmpty(module.Location))
				throw new ArgumentException("Module has no path");
			this.moduleFullPath = Path.GetFullPath(module.Location);
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
			if (moduleFullPath == other.moduleFullPath)
				return true;
			if (moduleFullPath == null || other.moduleFullPath == null)
				return false;
			return moduleFullPath.Equals(other.moduleFullPath, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj) {
			if (!(obj is MethodKey))
				return false;
			return Equals((MethodKey)obj);
		}

		public override int GetHashCode() {
			return token ^ (moduleFullPath == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(moduleFullPath));
		}

		public override string ToString() {
			return string.Format("{0:X8} {1}", token, moduleFullPath);
		}

		public bool IsSameModule(string moduleFullPath) {
			return StringComparer.OrdinalIgnoreCase.Equals(this.moduleFullPath, moduleFullPath);
		}
	}
}
