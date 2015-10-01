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

namespace dndbg.Engine {
	public struct SerializedDnModuleWithAssembly : IEquatable<SerializedDnModuleWithAssembly> {
		/// <summary>
		/// Gets the assembly name. This is identical to <see cref="DnAssembly.Name"/>, i.e., it's
		/// usually the path to the assembly file on disk, unless it's an in-memory assembly, in
		/// which case it's the name of the assembly.
		/// </summary>
		public string Assembly {
			get { return asmName; }
		}
		readonly string asmName;

		/// <summary>
		/// Gets the module
		/// </summary>
		public SerializedDnModule Module {
			get { return module; }
		}
		/*readonly*/ SerializedDnModule module;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="module">Module</param>
		public SerializedDnModuleWithAssembly(string assembly, SerializedDnModule module) {
			this.asmName = assembly;
			this.module = module;
		}

		public static bool operator ==(SerializedDnModuleWithAssembly a, SerializedDnModuleWithAssembly b) {
			return a.Equals(b);
		}

		public static bool operator !=(SerializedDnModuleWithAssembly a, SerializedDnModuleWithAssembly b) {
			return !a.Equals(b);
		}

		public bool Equals(SerializedDnModuleWithAssembly other) {
			return StringComparer.OrdinalIgnoreCase.Equals(Assembly, other.Assembly) &&
					Module == other.Module;
		}

		public override bool Equals(object obj) {
			return obj is SerializedDnModuleWithAssembly && Equals((SerializedDnModuleWithAssembly)obj);
		}

		public override int GetHashCode() {
			return StringComparer.OrdinalIgnoreCase.GetHashCode(Assembly) ^ Module.GetHashCode();
		}

		public override string ToString() {
			return string.Format("{0} ASM={1}", Module, Assembly);
		}
	}
}
