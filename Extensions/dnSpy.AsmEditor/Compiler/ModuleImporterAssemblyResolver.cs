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
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.AsmEditor.Compiler;

namespace dnSpy.AsmEditor.Compiler {
	sealed class ModuleImporterAssemblyResolver : IAssemblyResolver, IDisposable {
		readonly ModuleContext moduleContext;
		readonly ReferenceInfo[] references;

		sealed class ReferenceInfo : IDisposable {
			public readonly RawModuleBytes RawData;
			readonly CompilerMetadataReference mdRef;
			public ModuleDefMD Module;

			public IAssembly Assembly => mdRef.Assembly;

			public ReferenceInfo(RawModuleBytes rawData, in CompilerMetadataReference mdRef) {
				RawData = rawData;
				this.mdRef = mdRef;
			}

			public void Dispose() => Module?.Dispose();
		}

		public ModuleImporterAssemblyResolver((RawModuleBytes rawData, CompilerMetadataReference mdRef)[] references) {
			moduleContext = new ModuleContext(this, new Resolver(this));
			this.references = new ReferenceInfo[references.Length];
			for (int i = 0; i < references.Length; i++)
				this.references[i] = new ReferenceInfo(references[i].rawData, references[i].mdRef);
		}

		public AssemblyDef Resolve(IAssembly assembly, ModuleDef sourceModule) {
			if (TryResolve(assembly, AssemblyNameComparer.CompareAll, out var resolvedAssembly))
				return resolvedAssembly;
			if (TryResolve(assembly, AssemblyNameComparer.NameAndPublicKeyTokenOnly, out resolvedAssembly))
				return resolvedAssembly;
			if (TryResolve(assembly, AssemblyNameComparer.NameOnly, out resolvedAssembly))
				return resolvedAssembly;
			return null;
		}

		bool TryResolve(IAssembly assemblyReference, AssemblyNameComparer comparer, out AssemblyDef assembly) {
			foreach (var reference in references) {
				if (comparer.Equals(reference.Assembly, assemblyReference)) {
					assembly = GetModule(reference)?.Assembly;
					if (assembly != null)
						return true;
				}
			}

			assembly = null;
			return false;
		}

		unsafe ModuleDef GetModule(ReferenceInfo reference) {
			if (reference.Module == null) {
				var options = new ModuleCreationOptions(moduleContext);
				options.TryToLoadPdbFromDisk = false;
				reference.Module = ModuleDefMD.Load((IntPtr)reference.RawData.Pointer, options, reference.RawData.IsFileLayout ? ImageLayout.File : ImageLayout.Memory);
			}
			return reference.Module;
		}

		public void Dispose() {
			foreach (var reference in references)
				reference.Dispose();
		}
	}
}
