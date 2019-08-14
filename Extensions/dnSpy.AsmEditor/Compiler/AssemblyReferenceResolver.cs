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

using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.AsmEditor.Compiler;

namespace dnSpy.AsmEditor.Compiler {
	sealed class AssemblyReferenceResolver : IAssemblyReferenceResolver {
		readonly RawModuleBytesProvider rawModuleBytesProvider;
		readonly IAssemblyResolver assemblyResolver;
		readonly IAssembly tempAssembly;
		readonly ModuleDef editedModule;
		readonly TypeDef? nonNestedEditedType;
		readonly List<(RawModuleBytes rawData, CompilerMetadataReference mdRef)> rawModuleBytesList;

		public AssemblyReferenceResolver(RawModuleBytesProvider rawModuleBytesProvider, IAssemblyResolver assemblyResolver, IAssembly tempAssembly, ModuleDef editedModule, TypeDef? nonNestedEditedType) {
			Debug2.Assert(nonNestedEditedType is null || nonNestedEditedType.Module == editedModule);
			Debug2.Assert(nonNestedEditedType?.DeclaringType is null);
			this.rawModuleBytesProvider = rawModuleBytesProvider;
			this.assemblyResolver = assemblyResolver;
			this.tempAssembly = tempAssembly;
			this.editedModule = editedModule;
			this.nonNestedEditedType = nonNestedEditedType;
			rawModuleBytesList = new List<(RawModuleBytes rawData, CompilerMetadataReference mdRef)>();
		}

		internal (RawModuleBytes rawData, CompilerMetadataReference mdRef)[] GetReferences() => rawModuleBytesList.ToArray();

		CompilerMetadataReference? Save((RawModuleBytes rawData, CompilerMetadataReference mdRef) info) {
			if (info.rawData is null)
				return null;
			try {
				rawModuleBytesList.Add(info);
			}
			catch {
				info.rawData.Dispose();
				throw;
			}
			return info.mdRef;
		}

		public CompilerMetadataReference? Resolve(IAssembly asmRef) {
			ModuleDef? sourceModule = null;
			var asm = assemblyResolver.Resolve(asmRef, sourceModule ?? editedModule);
			if (asm is null)
				return null;

			return Save(CreateRef(asm.ManifestModule));
		}

		public CompilerMetadataReference? Create(AssemblyDef asm) => Save(CreateRef(asm.ManifestModule));
		public CompilerMetadataReference? Create(ModuleDef module) => Save(CreateRef(module));

		unsafe (RawModuleBytes rawData, CompilerMetadataReference mdRef) CreateRef(ModuleDef module) {
			var moduleData = rawModuleBytesProvider.GetRawModuleBytes(module);
			if (moduleData is null)
				return default;
			bool error = true;
			try {
				// Only file layout is supported by CompilerMetadataReference
				if (!moduleData.IsFileLayout)
					return default;

				var patcher = new ModulePatcher(moduleData, moduleData.IsFileLayout, tempAssembly, editedModule, nonNestedEditedType);
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
			foreach (var info in rawModuleBytesList)
				info.rawData.Dispose();
			rawModuleBytesList.Clear();
		}
	}
}
