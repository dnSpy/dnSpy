/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Type scope kind
	/// </summary>
	public enum DmdTypeScopeKind {
		/// <summary>
		/// It's not a TypeDef or TypeRef so it doesn't have a type scope
		/// </summary>
		Invalid,

		/// <summary>
		/// Same module as the reference
		/// </summary>
		Module,

		/// <summary>
		/// A reference to another module in the same assembly
		/// </summary>
		ModuleRef,

		/// <summary>
		/// A reference to another assembly
		/// </summary>
		AssemblyRef,
	}

	/// <summary>
	/// A <see cref="DmdType"/> scope
	/// </summary>
	public readonly struct DmdTypeScope {
		/// <summary>
		/// An instance whose <see cref="Kind"/> equals <see cref="DmdTypeScopeKind.Invalid"/>
		/// </summary>
		public static readonly DmdTypeScope Invalid = new DmdTypeScope(DmdTypeScopeKind.Invalid);

		/// <summary>
		/// Gets the kind
		/// </summary>
		public DmdTypeScopeKind Kind { get; }

		/// <summary>
		/// Gets the data: <see cref="DmdModule"/>, <see cref="string"/> (<see cref="DmdTypeScopeKind.ModuleRef"/>), <see cref="IDmdAssemblyName"/>
		/// </summary>
		public object Data { get; }

		/// <summary>
		/// Used if it's a module reference. This is the assembly name (<see cref="IDmdAssemblyName"/>)
		/// </summary>
		public object Data2 { get; }

		DmdTypeScope(DmdTypeScopeKind kind) {
			if (kind != DmdTypeScopeKind.Invalid)
				throw new InvalidOperationException();
			Kind = kind;
			Data = null;
			Data2 = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		public DmdTypeScope(DmdModule module) {
			Kind = DmdTypeScopeKind.Module;
			Data = module ?? throw new ArgumentNullException(nameof(module));
			Data2 = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="moduleName">Module name</param>
		public DmdTypeScope(IDmdAssemblyName assembly, string moduleName) {
			Kind = DmdTypeScopeKind.ModuleRef;
			Data = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
			Data2 = assembly ?? throw new ArgumentNullException(nameof(assembly));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assemblyRef">Assembly reference</param>
		public DmdTypeScope(IDmdAssemblyName assemblyRef) {
			Kind = DmdTypeScopeKind.AssemblyRef;
			Data = assemblyRef ?? throw new ArgumentNullException(nameof(assemblyRef));
			Data2 = null;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			switch (Kind) {
			case DmdTypeScopeKind.Invalid:		return "<invalid>";
			case DmdTypeScopeKind.Module:		return "Module: " + Data.ToString();
			case DmdTypeScopeKind.ModuleRef:	return "ModuleRef: " + Data.ToString() + ": " + Data2.ToString();
			case DmdTypeScopeKind.AssemblyRef:	return "Assembly: " + Data.ToString();
			default:							throw new InvalidOperationException();
			}
		}
	}
}
