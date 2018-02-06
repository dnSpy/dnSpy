/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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

using System.Collections.Generic;
using dnlib.DotNet;
using dnSpy.Contracts.AsmEditor.Compiler;

namespace dnSpy.AsmEditor.Compiler {
	sealed class AssemblyReferenceResolver : IAssemblyReferenceResolver {
		readonly RawModuleBytesProvider rawModuleBytesProvider;
		readonly IAssemblyResolver assemblyResolver;
		readonly ModuleDef defaultSourceModule;
		readonly bool makeEverythingPublic;
		readonly List<RawModuleBytes> rawModuleBytesList;

		public AssemblyReferenceResolver(RawModuleBytesProvider rawModuleBytesProvider, IAssemblyResolver assemblyResolver, ModuleDef defaultSourceModule, bool makeEverythingPublic) {
			this.rawModuleBytesProvider = rawModuleBytesProvider;
			this.assemblyResolver = assemblyResolver;
			this.defaultSourceModule = defaultSourceModule;
			this.makeEverythingPublic = makeEverythingPublic;
			rawModuleBytesList = new List<RawModuleBytes>();
		}

		CompilerMetadataReference? Save(in (RawModuleBytes rawData, CompilerMetadataReference mdRef) info) {
			if (info.rawData == null)
				return null;
			try {
				rawModuleBytesList.Add(info.rawData);
			}
			catch {
				info.rawData.Dispose();
				throw;
			}
			return info.mdRef;
		}

		public CompilerMetadataReference? Resolve(IAssembly asmRef) {
			ModuleDef sourceModule = null;
			var asm = assemblyResolver.Resolve(asmRef, sourceModule ?? defaultSourceModule);
			if (asm == null)
				return null;

			return Save(CompilerMetadataReferenceCreator.Create(rawModuleBytesProvider, asm.ManifestModule, makeEverythingPublic));
		}

		public CompilerMetadataReference? Create(AssemblyDef asm) =>
			Save(CompilerMetadataReferenceCreator.Create(rawModuleBytesProvider, asm.ManifestModule, makeEverythingPublic));

		public CompilerMetadataReference? Create(ModuleDef module) =>
			Save(CompilerMetadataReferenceCreator.Create(rawModuleBytesProvider, module, makeEverythingPublic));

		public void Dispose() {
			foreach (var rawData in rawModuleBytesList)
				rawData.Dispose();
			rawModuleBytesList.Clear();
		}
	}
}
