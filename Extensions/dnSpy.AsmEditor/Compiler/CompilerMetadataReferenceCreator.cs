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

using dnlib.DotNet;
using dnSpy.Contracts.AsmEditor.Compiler;

namespace dnSpy.AsmEditor.Compiler {
	static class CompilerMetadataReferenceCreator {
		public unsafe static (RawModuleBytes rawData, CompilerMetadataReference mdRef) Create(RawModuleBytesProvider rawModuleBytesProvider, ModuleDef module, bool makeEverythingPublic) {
			var info = rawModuleBytesProvider.GetRawModuleBytes(module);
			if (info.rawData == null)
				return default;
			bool error = true;
			try {
				// Only file layout is supported by CompilerMetadataReference
				if (!info.isFileLayout)
					return default;

				if (makeEverythingPublic) {
					bool success = new MetadataFixer(info.rawData, info.isFileLayout).MakePublic();
					if (!success)
						return default;
				}
				var asmRef = module.Assembly.ToAssemblyRef();
				CompilerMetadataReference mdRef;
				if (module.IsManifestModule)
					mdRef = CompilerMetadataReference.CreateAssemblyReference(info.rawData.Pointer, info.rawData.Size, asmRef, module.Location);
				else
					mdRef = CompilerMetadataReference.CreateModuleReference(info.rawData.Pointer, info.rawData.Size, asmRef, module.Location);
				error = false;
				return (info.rawData, mdRef);
			}
			finally {
				if (error)
					info.rawData.Dispose();
			}
		}
	}
}
