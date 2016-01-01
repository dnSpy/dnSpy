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
using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Project options
	/// </summary>
	public class ProjectOptions {
		/// <summary>
		/// Where to save the files
		/// </summary>
		public string Directory { get; set; }

		/// <summary>
		/// Don't reference mscorlib (C# projects only)
		/// </summary>
		public bool DontReferenceStdLib { get; set; }

		/// <summary>
		/// Guid of project, stored in <c>ProjectGuid</c> in the .csproj file
		/// </summary>
		public Guid? ProjectGuid { get; set; }

		/// <summary>
		/// Other project files or null
		/// </summary>
		public List<ProjectFileRef> FileRefs { get; set; }

		/// <summary>
		/// The assembly resolver or null
		/// </summary>
		public IAssemblyResolver AssemblyResolver { get; set; }

		/// <summary>
		/// Resolves an assembly using <see cref="AssemblyResolver"/>
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="sourceModule">Source module or null</param>
		/// <returns></returns>
		public AssemblyDef Resolve(IAssembly assembly, ModuleDef sourceModule) {
			return AssemblyResolver == null ? null : AssemblyResolver.Resolve(assembly, sourceModule);
		}
	}
}
