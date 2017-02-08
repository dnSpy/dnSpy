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

using System.Collections.Generic;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.App;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.AsmEditor.Compiler {
	sealed class EditClassVM : EditCodeVM {
		readonly IMemberDef defToEdit;
		readonly TypeDef nonNestedTypeToEdit;
		readonly MethodSourceStatement? methodSourceStatement;

		sealed class EditMethodDecompileCodeState : DecompileCodeState {
			public ReferenceDecompilerOutput MainOutput { get; }

			public EditMethodDecompileCodeState(object referenceToEdit, MethodSourceStatement? methodSourceStatement) {
				MainOutput = new ReferenceDecompilerOutput(referenceToEdit, methodSourceStatement);
			}
		}

		public EditClassVM(IRawModuleBytesProvider rawModuleBytesProvider, IOpenFromGAC openFromGAC, IOpenAssembly openAssembly, ILanguageCompiler languageCompiler, IDecompiler decompiler, IMemberDef defToEdit, IList<MethodSourceStatement> statementsInMethodToEdit)
			: base(rawModuleBytesProvider, openFromGAC, openAssembly, languageCompiler, decompiler, defToEdit.Module) {
			this.defToEdit = defToEdit;
			nonNestedTypeToEdit = defToEdit as TypeDef ?? defToEdit.DeclaringType;
			while (nonNestedTypeToEdit.DeclaringType != null)
				nonNestedTypeToEdit = nonNestedTypeToEdit.DeclaringType;
			methodSourceStatement = statementsInMethodToEdit.Count == 0 ? (MethodSourceStatement?)null : statementsInMethodToEdit[0];
			StartDecompile();
		}

		protected override DecompileCodeState CreateDecompileCodeState() =>
			new EditMethodDecompileCodeState(defToEdit, methodSourceStatement);

		protected override Task<DecompileAsyncResult> DecompileAsync(DecompileCodeState decompileCodeState) {
			var state = (EditMethodDecompileCodeState)decompileCodeState;
			state.CancellationToken.ThrowIfCancellationRequested();

			state.DecompilationContext.CalculateBinSpans = true;
			var options = new DecompileTypeMethods(state.MainOutput, state.DecompilationContext, nonNestedTypeToEdit);
			options.DecompileHidden = false;
			options.ShowAll = true;
			options.MakeEverythingPublic = makeEverythingPublic;
			decompiler.Decompile(DecompilationType.TypeMethods, options);

			state.CancellationToken.ThrowIfCancellationRequested();

			var result = new DecompileAsyncResult();
			result.AddDocument(MAIN_CODE_NAME, state.MainOutput.ToString(), state.MainOutput.Span);
			return Task.FromResult(result);
		}

		protected override void Import(ModuleImporter importer, CompilationResult result) =>
			importer.Import(result.RawFile, result.DebugFile, nonNestedTypeToEdit);
	}
}
