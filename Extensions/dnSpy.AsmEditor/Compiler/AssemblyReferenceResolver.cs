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

using dnlib.DotNet;
using dnSpy.Contracts.AsmEditor.Compiler;

namespace dnSpy.AsmEditor.Compiler {
	sealed class AssemblyReferenceResolver : IAssemblyReferenceResolver {
		readonly IAssemblyResolver assemblyResolver;
		readonly ModuleDef defaultSourceModule;
		readonly bool makeEverythingPublic;

		public AssemblyReferenceResolver(IAssemblyResolver assemblyResolver, ModuleDef defaultSourceModule, bool makeEverythingPublic) {
			this.assemblyResolver = assemblyResolver;
			this.defaultSourceModule = defaultSourceModule;
			this.makeEverythingPublic = makeEverythingPublic;
		}

		public CompilerMetadataReference? Resolve(IAssembly asmRef) {
			ModuleDef sourceModule = null;
			var asm = assemblyResolver.Resolve(asmRef, sourceModule ?? defaultSourceModule);
			if (asm == null)
				return null;

			return CompilerMetadataReferenceCreator.Create(asm.ManifestModule, makeEverythingPublic);
		}

		public CompilerMetadataReference? Create(AssemblyDef asm) =>
			CompilerMetadataReferenceCreator.Create(asm.ManifestModule, makeEverythingPublic);

		public CompilerMetadataReference? Create(ModuleDef module) =>
			CompilerMetadataReferenceCreator.Create(module, makeEverythingPublic);
	}
}
