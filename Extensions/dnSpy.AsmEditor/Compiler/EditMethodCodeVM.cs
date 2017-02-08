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
	sealed class EditMethodCodeVM : EditCodeVM {
		readonly MethodDef methodToEdit;
		readonly MethodSourceStatement? methodSourceStatement;

		sealed class EditMethodDecompileCodeState : DecompileCodeState {
			public ReferenceDecompilerOutput MainOutput { get; }
			public StringBuilderDecompilerOutput HiddenOutput { get; }

			public EditMethodDecompileCodeState(object referenceToEdit, MethodSourceStatement? methodSourceStatement) {
				MainOutput = new ReferenceDecompilerOutput(referenceToEdit, methodSourceStatement);
				HiddenOutput = new StringBuilderDecompilerOutput();
			}
		}

		public EditMethodCodeVM(IRawModuleBytesProvider rawModuleBytesProvider, IOpenFromGAC openFromGAC, IOpenAssembly openAssembly, ILanguageCompiler languageCompiler, IDecompiler decompiler, MethodDef methodToEdit, IList<MethodSourceStatement> statementsInMethodToEdit)
			: base(rawModuleBytesProvider, openFromGAC, openAssembly, languageCompiler, decompiler, methodToEdit.Module) {
			this.methodToEdit = methodToEdit;
			methodSourceStatement = statementsInMethodToEdit.Count == 0 ? (MethodSourceStatement?)null : statementsInMethodToEdit[0];
			StartDecompile();
		}

		protected override DecompileCodeState CreateDecompileCodeState() =>
			new EditMethodDecompileCodeState(methodToEdit, methodSourceStatement);

		protected override Task<DecompileAsyncResult> DecompileAsync(DecompileCodeState decompileCodeState) {
			var state = (EditMethodDecompileCodeState)decompileCodeState;
			state.CancellationToken.ThrowIfCancellationRequested();

			var type = methodToEdit.DeclaringType;
			while (type.DeclaringType != null)
				type = type.DeclaringType;

			DecompileTypeMethods options;

			state.DecompilationContext.CalculateBinSpans = true;
			options = new DecompileTypeMethods(state.MainOutput, state.DecompilationContext, type);
			options.Methods.Add(methodToEdit);
			options.DecompileHidden = false;
			options.MakeEverythingPublic = makeEverythingPublic;
			decompiler.Decompile(DecompilationType.TypeMethods, options);

			state.CancellationToken.ThrowIfCancellationRequested();

			state.DecompilationContext.CalculateBinSpans = false;
			options = new DecompileTypeMethods(state.HiddenOutput, state.DecompilationContext, type);
			options.Methods.Add(methodToEdit);
			options.DecompileHidden = true;
			options.MakeEverythingPublic = makeEverythingPublic;
			decompiler.Decompile(DecompilationType.TypeMethods, options);

			state.CancellationToken.ThrowIfCancellationRequested();

			var result = new DecompileAsyncResult();
			result.AddDocument(MAIN_CODE_NAME, state.MainOutput.ToString(), state.MainOutput.Span);
			result.AddDocument(MAIN_G_CODE_NAME, state.HiddenOutput.ToString(), null);
			return Task.FromResult(result);
		}

		protected override void Import(ModuleImporter importer, CompilationResult result) =>
			importer.Import(result.RawFile, result.DebugFile, methodToEdit);
	}
}
