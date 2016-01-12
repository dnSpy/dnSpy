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
using dnlib.DotNet;
using ICSharpCode.Decompiler;

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Decompiles AssemblyInfo.{cs,vb}
	/// </summary>
	public sealed class DecompileAssemblyInfo : DecompileTypeBase {
		/// <summary>
		/// Gets the module
		/// </summary>
		public ModuleDef Module { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Type</param>
		/// <param name="output">Output</param>
		/// <param name="options">Options</param>
		public DecompileAssemblyInfo(ModuleDef module, ITextOutput output, DecompilationOptions options)
			: base(output, options) {
			if (module == null)
				throw new ArgumentNullException();
			this.Module = module;
		}
	}
}
