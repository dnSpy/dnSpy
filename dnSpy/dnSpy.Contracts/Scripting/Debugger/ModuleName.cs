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

using System;
using System.Diagnostics;
using System.IO;
using dnlib.DotNet;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// A module
	/// </summary>
	public struct ModuleName : IEquatable<ModuleName> {
		[Flags]
		enum Flags : byte {
			IsDynamic		= 0x01,
			IsInMemory		= 0x02,
			NameOnly		= 0x04,

			CompareMask		= IsDynamic | IsInMemory,
		}

		/// <summary>implicit operator</summary>
		/// <param name="moduleFilename">Module filename</param>
		public static implicit operator ModuleName(string moduleFilename) => Create(moduleFilename);

		/// <summary>
		/// Gets the full name, identical to the dnlib assembly full name
		/// </summary>
		public string AssemblyFullName => asmFullName ?? string.Empty;

		/// <summary>
		/// Name of module. This is the filename if <see cref="IsInMemory"/> is false, else it's <see cref="ModuleDef.Name"/>
		/// </summary>
		public string Name => moduleName ?? string.Empty;

		/// <summary>
		/// true if it's a dynamic module
		/// </summary>
		public bool IsDynamic => (flags & Flags.IsDynamic) != 0;

		/// <summary>
		/// true if it's an in-memory module and the file doesn't exist on disk
		/// </summary>
		public bool IsInMemory => (flags & Flags.IsInMemory) != 0;

		/// <summary>
		/// true if <see cref="AssemblyFullName"/> isn't used when comparing this instance against
		/// other instances.
		/// </summary>
		public bool ModuleNameOnly => (flags & Flags.NameOnly) != 0;

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
		/// <param name="isInMemory">true if it's an in-memory module</param>
		/// <param name="nameOnly">true if <paramref name="asmFullName"/> is ignored</param>
		public ModuleName(string asmFullName, string moduleName, bool isDynamic, bool isInMemory, bool nameOnly) {
			Debug.Assert(asmFullName == null || !asmFullName.Contains("\\:"));
			this.asmFullName = asmFullName ?? string.Empty;
			this.moduleName = moduleName ?? string.Empty;
			this.flags = 0;
			if (isDynamic)
				this.flags |= Flags.IsDynamic;
			if (isInMemory)
				this.flags |= Flags.IsInMemory;
			if (nameOnly)
				this.flags |= Flags.NameOnly;
		}

		/// <summary>
		/// Creates a <see cref="ModuleName"/> that was loaded from a file
		/// </summary>
		/// <param name="moduleFilename">Module filename</param>
		/// <returns></returns>
		public static ModuleName Create(string moduleFilename) => new ModuleName(string.Empty, GetFullName(moduleFilename), false, false, true);

		static string GetFullName(string filename) {
			try {
				return Path.GetFullPath(filename);
			}
			catch {
			}
			return filename;
		}

		/// <summary>
		/// Creates a <see cref="ModuleName"/> that was loaded from a file
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		public static ModuleName CreateFromFile(ModuleDef module) => new ModuleName(module.Assembly?.FullName ?? string.Empty, module.Location, false, false, false);

		/// <summary>
		/// Creates an in-memory <see cref="ModuleName"/>
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		public static ModuleName CreateInMemory(ModuleDef module) => new ModuleName(module.Assembly?.FullName ?? string.Empty, module.Name, false, true, false);

		/// <summary>
		/// Creates a <see cref="ModuleName"/>
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="isDynamic">true if it's a dynamic module</param>
		/// <param name="isInMemory">true if it's an in-memory module</param>
		/// <returns></returns>
		public static ModuleName Create(ModuleDef module, bool isDynamic, bool isInMemory) => new ModuleName(module.Assembly?.FullName ?? string.Empty, !isInMemory ? module.Location : module.Name.String, isDynamic, isInMemory, false);

		/// <summary>
		/// Creates a <see cref="ModuleName"/>
		/// </summary>
		/// <param name="asmFullName">Full name of assembly. Must be identical to <see cref="AssemblyDef.FullName"/></param>
		/// <param name="moduleName">Name of module. This is the filename if <paramref name="isInMemory"/>
		/// is false, else it must be identical to <see cref="ModuleDef.Name"/></param>
		/// <param name="isDynamic">true if it's a dynamic module</param>
		/// <param name="isInMemory">true if it's an in-memory module</param>
		/// <param name="moduleNameOnly">true if <paramref name="asmFullName"/> is ignored</param>
		/// <returns></returns>
		public static ModuleName Create(string asmFullName, string moduleName, bool isDynamic, bool isInMemory, bool moduleNameOnly) => new ModuleName(asmFullName, moduleName, isDynamic, isInMemory, false);

		/// <summary>
		/// operator==()
		/// </summary>
		/// <param name="a">a</param>
		/// <param name="b">b</param>
		/// <returns></returns>
		public static bool operator ==(ModuleName a, ModuleName b) => a.Equals(b);

		/// <summary>
		/// operator!=()
		/// </summary>
		/// <param name="a">a</param>
		/// <param name="b">b</param>
		/// <returns></returns>
		public static bool operator !=(ModuleName a, ModuleName b) => !a.Equals(b);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(ModuleName other) {
			return (ModuleNameOnly || other.ModuleNameOnly || AssemblyNameComparer.Equals(AssemblyFullName, other.AssemblyFullName)) &&
					ModuleNameComparer.Equals(Name, other.Name) &&
					(flags & Flags.CompareMask) == (other.flags & Flags.CompareMask);
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object obj) {
			var other = obj as ModuleName?;
			if (other != null)
				return Equals(other.Value);
			return false;
		}

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			// We can't use AssemblyFullName since it's not used if ModuleNameOnly is true
			return ModuleNameComparer.GetHashCode(Name) ^ ((int)(flags & Flags.CompareMask) << 16);
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (ModuleNameOnly)
				return string.Format("DYN={0} MEM={1} [{2}]", IsDynamic ? 1 : 0, IsInMemory ? 1 : 0, Name);
			return string.Format("DYN={0} MEM={1} {2} [{3}]", IsDynamic ? 1 : 0, IsInMemory ? 1 : 0, AssemblyFullName, Name);
		}
	}
}
