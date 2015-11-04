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
	public struct SerializedDnModule : IEquatable<SerializedDnModule> {
		[Flags]
		enum Flags : byte {
			IsDynamic		= 1,
			IsInMemory		= 2,
		}

		/// <summary>
		/// Gets the full name, identical to the dnlib assembly full name
		/// </summary>
		public string AssemblyFullName {
			get { return asmFullName ?? string.Empty; }
		}

		/// <summary>
		/// Name of module. This is the filename if <see cref="IsInMemory"/> is false, else it's <see cref="ModuleDef.Name"/>
		/// </summary>
		public string ModuleName {
			get { return moduleName ?? string.Empty; }
		}

		/// <summary>
		/// true if it's a dynamic module
		/// </summary>
		public bool IsDynamic {
			get { return (flags & Flags.IsDynamic) != 0; }
		}

		/// <summary>
		/// true if it's an in-memory module and the file doesn't exist on disk
		/// </summary>
		public bool IsInMemory {
			get { return (flags & Flags.IsInMemory) != 0; }
		}

		static readonly StringComparer AssemblyNameComparer = StringComparer.OrdinalIgnoreCase;
		// The module name can contain filenames so case must be ignored
		static readonly StringComparer ModuleNameComparer = StringComparer.OrdinalIgnoreCase;
		readonly string asmFullName;
		readonly string moduleName;
		readonly Flags flags;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmFullName">Assembly full name</param>
		/// <param name="moduleName">Module name</param>
		/// <param name="isDynamic">true if it's a dynamic module</param>
		/// <param name="isInMemory">ture if it's an in-memory module</param>
		public SerializedDnModule(string asmFullName, string moduleName, bool isDynamic, bool isInMemory) {
			this.asmFullName = asmFullName ?? string.Empty;
			this.moduleName = moduleName ?? string.Empty;
			this.flags = 0;
			if (isDynamic)
				this.flags |= Flags.IsDynamic;
			if (isInMemory)
				this.flags |= Flags.IsInMemory;
		}

		public static bool operator ==(SerializedDnModule a, SerializedDnModule b) {
			return a.Equals(b);
		}

		public static bool operator !=(SerializedDnModule a, SerializedDnModule b) {
			return !a.Equals(b);
		}

		public bool Equals(SerializedDnModule other) {
			return AssemblyNameComparer.Equals(AssemblyFullName, other.AssemblyFullName) &&
					ModuleNameComparer.Equals(ModuleName, other.ModuleName) &&
					flags == other.flags;
		}

		public override bool Equals(object obj) {
			var other = obj as SerializedDnModule?;
			if (other != null)
				return Equals(other.Value);
			return false;
		}

		public override int GetHashCode() {
			return AssemblyNameComparer.GetHashCode(AssemblyFullName) ^
				ModuleNameComparer.GetHashCode(ModuleName) ^
				((int)flags << 16);
		}

		public override string ToString() {
			return string.Format("DYN={0} MEM={1} {2} [{3}]", IsDynamic ? 1 : 0, IsInMemory ? 1 : 0, AssemblyFullName, ModuleName);
		}
	}
}
