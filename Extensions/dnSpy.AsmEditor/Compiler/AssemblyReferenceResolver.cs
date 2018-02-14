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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.AsmEditor.Compiler;

namespace dnSpy.AsmEditor.Compiler {
	sealed class AssemblyReferenceResolver : IAssemblyReferenceResolver {
		readonly RawModuleBytesProvider rawModuleBytesProvider;
		readonly IAssemblyResolver assemblyResolver;
		readonly IAssembly tempAssembly;
		readonly ModuleDef defaultSourceModule;
		readonly TypeDef nonNestedEditedTypeOrNull;
		readonly bool makeEverythingPublic;
		readonly List<RawModuleBytes> rawModuleBytesList;

		public AssemblyReferenceResolver(RawModuleBytesProvider rawModuleBytesProvider, IAssemblyResolver assemblyResolver, IAssembly tempAssembly, ModuleDef defaultSourceModule, TypeDef nonNestedEditedTypeOrNull, bool makeEverythingPublic) {
			Debug.Assert(nonNestedEditedTypeOrNull == null || nonNestedEditedTypeOrNull.Module == defaultSourceModule);
			Debug.Assert(nonNestedEditedTypeOrNull?.DeclaringType == null);
			this.rawModuleBytesProvider = rawModuleBytesProvider;
			this.assemblyResolver = assemblyResolver;
			this.tempAssembly = tempAssembly;
			this.defaultSourceModule = defaultSourceModule;
			this.nonNestedEditedTypeOrNull = nonNestedEditedTypeOrNull;
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

			return Save(CreateRef(asm.ManifestModule));
		}

		public CompilerMetadataReference? Create(AssemblyDef asm) => Save(CreateRef(asm.ManifestModule));
		public CompilerMetadataReference? Create(ModuleDef module) => Save(CreateRef(module));

		unsafe (RawModuleBytes rawData, CompilerMetadataReference mdRef) CreateRef(ModuleDef module) {
			var info = rawModuleBytesProvider.GetRawModuleBytes(module);
			var moduleData = info.rawData;
			if (moduleData == null)
				return default;
			bool error = true;
			try {
				// Only file layout is supported by CompilerMetadataReference
				if (!info.isFileLayout)
					return default;

				var patcher = new ModulePatcher(moduleData, info.isFileLayout, tempAssembly, nonNestedEditedTypeOrNull, makeEverythingPublic);
				if (!patcher.Patch(module, out var newModuleData))
					return default;
				if (moduleData != newModuleData) {
					moduleData.Dispose();
					moduleData = newModuleData;
				}

				var asmRef = module.Assembly.ToAssemblyRef();
				CompilerMetadataReference mdRef;
				if (module.IsManifestModule)
					mdRef = CompilerMetadataReference.CreateAssemblyReference(moduleData.Pointer, moduleData.Size, asmRef, module.Location);
				else
					mdRef = CompilerMetadataReference.CreateModuleReference(moduleData.Pointer, moduleData.Size, asmRef, module.Location);
				error = false;
				return (rawData: moduleData, mdRef);
			}
			finally {
				if (error)
					moduleData.Dispose();
			}
		}

		public void Dispose() {
			foreach (var rawData in rawModuleBytesList)
				rawData.Dispose();
			rawModuleBytesList.Clear();
		}
	}
}
