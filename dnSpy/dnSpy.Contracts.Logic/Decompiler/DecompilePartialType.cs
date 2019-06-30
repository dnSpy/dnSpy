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
using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Decompiles a partial type
	/// </summary>
	public sealed class DecompilePartialType : DecompileTypeBase {
		/// <summary>
		/// Type to decompile
		/// </summary>
		public TypeDef Type { get; }

		/// <summary>
		/// true to add the 'partial' keyword. It's true by default.
		/// </summary>
		public bool AddPartialKeyword { get; set; }

		/// <summary>
		/// All definitions that must be hidden or shown must be added here
		/// </summary>
		public HashSet<IMemberDef> Definitions { get; }

		/// <summary>
		/// true if members in <see cref="Definitions"/> should be shown, false if they should be removed
		/// </summary>
		public bool ShowDefinitions { get; set; }

		/// <summary>
		/// true to use using declarations, false to use full namespaces (eg. useful when decompiling
		/// WinForms designer files)
		/// </summary>
		public bool UseUsingDeclarations { get; set; }

		/// <summary>
		/// Interfaces to remove from the type
		/// </summary>
		public List<ITypeDefOrRef> InterfacesToRemove { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="ctx">Context</param>
		/// <param name="type">Type</param>
		public DecompilePartialType(IDecompilerOutput output, DecompilationContext ctx, TypeDef type)
			: base(output, ctx) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			AddPartialKeyword = true;
			UseUsingDeclarations = true;
			Definitions = new HashSet<IMemberDef>();
			InterfacesToRemove = new List<ITypeDefOrRef>();
		}
	}
}
