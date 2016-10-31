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
	static class CompilerMetadataReferenceCreator {
		public static CompilerMetadataReference? Create(IRawModuleBytesProvider rawModuleBytesProvider, ModuleDef module, bool makeEverythingPublic) {
			var moduleData = rawModuleBytesProvider.GetRawModuleBytes(module);
			if (moduleData == null)
				return null;
			if (makeEverythingPublic) {
				bool success = new MetadataFixer(moduleData).MakePublic();
				if (!success)
					return null;
			}
			var asmRef = module.Assembly.ToAssemblyRef();
			if (module.IsManifestModule)
				return CompilerMetadataReference.CreateAssemblyReference(moduleData, asmRef, module.Location);
			return CompilerMetadataReference.CreateModuleReference(moduleData, asmRef, module.Location);
		}
	}
}
