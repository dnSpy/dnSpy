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
using dnlib.DotNet;

namespace dnSpy.Files {
	public struct SerializedDnSpyModule : IEquatable<SerializedDnSpyModule> {
        [Flags]
		enum Flags : byte {
			IsDynamic		= 1,
			IsInMemory		= 2,
		}

		/// <summary>
		/// Gets the full name, identical to the dnlib assembly full name
		/// </summary>
		public string AssemblyFullName {
			get { return asmFullName; }
		}

		/// <summary>
		/// Name of module. This is the filename if <see cref="IsInMemory"/> is false
		/// </summary>
		public string ModuleName {
			get { return moduleName; }
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

		SerializedDnSpyModule(string asmFullName, string moduleName, bool isDynamic, bool isInMemory) {
			this.asmFullName = asmFullName ?? string.Empty;
			this.moduleName = moduleName ?? string.Empty;
			this.flags = 0;
			if (isDynamic)
				this.flags |= Flags.IsDynamic;
			if (isInMemory)
				this.flags |= Flags.IsInMemory;
		}

		public static SerializedDnSpyModule CreateFromFile(ModuleDef module) {
			var asm = module.Assembly;
			return new SerializedDnSpyModule(asm == null ? string.Empty : asm.FullName, module.Location, false, false);
		}

		public static SerializedDnSpyModule CreateInMemory(ModuleDef module) {
			var asm = module.Assembly;
			return new SerializedDnSpyModule(asm == null ? string.Empty : asm.FullName, module.Name, false, true);
		}

		public static SerializedDnSpyModule CreateDynamic(ModuleDef module, bool isInMemory) {
			var asm = module.Assembly;
			return new SerializedDnSpyModule(asm == null ? string.Empty : asm.FullName, module.Name, true, isInMemory);
		}

		public static SerializedDnSpyModule Create(string asmFullName, string moduleName, bool isDynamic, bool isInMemory) {
			return new SerializedDnSpyModule(asmFullName, moduleName, isDynamic, isInMemory);
		}

		public bool Equals(SerializedDnSpyModule other) {
			return AssemblyNameComparer.Equals(asmFullName ?? string.Empty, other.asmFullName ?? string.Empty) &&
					ModuleNameComparer.Equals(moduleName ?? string.Empty, other.moduleName ?? string.Empty) &&
					flags == other.flags;
		}

		public override bool Equals(object obj) {
			var other = obj as SerializedDnSpyModule?;
			if (other != null)
				return Equals(other.Value);
			return false;
		}

		public override int GetHashCode() {
			return AssemblyNameComparer.GetHashCode(asmFullName ?? string.Empty) ^
				ModuleNameComparer.GetHashCode(moduleName ?? string.Empty) ^
				((int)flags << 16);
		}

		public override string ToString() {
			return string.Format("DYN={0} MEM={1} {2} [{3}]", IsDynamic ? 1 : 0, IsInMemory ? 1 : 0, asmFullName ?? string.Empty, moduleName ?? string.Empty);
		}
	}
}
