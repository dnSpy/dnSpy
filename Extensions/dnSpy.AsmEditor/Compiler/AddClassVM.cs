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

using System.Threading.Tasks;
using dnlib.DotNet;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.App;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.AsmEditor.Compiler {
	sealed class AddClassVM : EditCodeVM {
		sealed class AddClassDecompileCodeState : DecompileCodeState {
		}

		public AddClassVM(IRawModuleBytesProvider rawModuleBytesProvider, IOpenFromGAC openFromGAC, IOpenAssembly openAssembly, ILanguageCompiler languageCompiler, IDecompiler decompiler, ModuleDef module)
			: base(rawModuleBytesProvider, openFromGAC, openAssembly, languageCompiler, decompiler, module) {
			StartDecompile();
		}

		protected override DecompileCodeState CreateDecompileCodeState() =>
			new AddClassDecompileCodeState();

		protected override Task<DecompileAsyncResult> DecompileAsync(DecompileCodeState decompileCodeState) {
			var result = new DecompileAsyncResult();
			result.AddDocument(MAIN_CODE_NAME, string.Empty, null);
			return Task.FromResult(result);
		}

		protected override void Import(ModuleImporter importer, CompilationResult result) =>
			importer.ImportEverything(result.RawFile, result.DebugFile);
	}
}
