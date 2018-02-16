/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.AsmEditor.Compiler;

namespace dnSpy.AsmEditor.Compiler {
	sealed class AssemblyReferenceResolver : IAssemblyReferenceResolver {
		readonly IRawModuleBytesProvider rawModuleBytesProvider;
		readonly IAssemblyResolver assemblyResolver;
		readonly IAssembly tempAssembly;
		readonly ModuleDef defaultSourceModule;
		readonly TypeDef nonNestedEditedTypeOrNull;
		readonly bool makeEverythingPublic;

		public AssemblyReferenceResolver(IRawModuleBytesProvider rawModuleBytesProvider, IAssemblyResolver assemblyResolver, IAssembly tempAssembly, ModuleDef defaultSourceModule, TypeDef nonNestedEditedTypeOrNull, bool makeEverythingPublic) {
			Debug.Assert(nonNestedEditedTypeOrNull == null || nonNestedEditedTypeOrNull.Module == defaultSourceModule);
			Debug.Assert(nonNestedEditedTypeOrNull?.DeclaringType == null);
			this.rawModuleBytesProvider = rawModuleBytesProvider;
			this.assemblyResolver = assemblyResolver;
			this.tempAssembly = tempAssembly;
			this.defaultSourceModule = defaultSourceModule;
			this.nonNestedEditedTypeOrNull = nonNestedEditedTypeOrNull;
			this.makeEverythingPublic = makeEverythingPublic;
		}

		public CompilerMetadataReference? Resolve(IAssembly asmRef) {
			ModuleDef sourceModule = null;
			var asm = assemblyResolver.Resolve(asmRef, sourceModule ?? defaultSourceModule);
			if (asm == null)
				return null;

			return CompilerMetadataReferenceCreator.Create(rawModuleBytesProvider, tempAssembly, asm.ManifestModule, nonNestedEditedTypeOrNull, makeEverythingPublic);
		}

		public CompilerMetadataReference? Create(AssemblyDef asm) =>
			CompilerMetadataReferenceCreator.Create(rawModuleBytesProvider, tempAssembly, asm.ManifestModule, nonNestedEditedTypeOrNull, makeEverythingPublic);

		public CompilerMetadataReference? Create(ModuleDef module) =>
			CompilerMetadataReferenceCreator.Create(rawModuleBytesProvider, tempAssembly, module, nonNestedEditedTypeOrNull, makeEverythingPublic);
	}
}
