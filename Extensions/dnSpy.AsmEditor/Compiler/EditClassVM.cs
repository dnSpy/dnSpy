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
using System.Threading.Tasks;
using dnlib.DotNet;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.AsmEditor.Compiler {
	sealed class EditClassVM : EditCodeVM {
		readonly IMemberDef defToEdit;
		readonly TypeDef nonNestedTypeToEdit;
		readonly MethodSourceStatement? methodSourceStatement;

		sealed class EditClassDecompileCodeState : DecompileCodeState {
			public ReferenceDecompilerOutput MainOutput { get; }

			public EditClassDecompileCodeState(object referenceToEdit, MethodSourceStatement? methodSourceStatement) {
				MainOutput = new ReferenceDecompilerOutput(referenceToEdit, methodSourceStatement);
				DecompilationContext.AsyncMethodBodyDecompilation = true;
			}
		}

		public EditClassVM(EditCodeVMOptions options, IMemberDef defToEdit, IList<MethodSourceStatement> statementsInMethodToEdit)
			: base(options, defToEdit as TypeDef ?? defToEdit.DeclaringType) {
			this.defToEdit = defToEdit;
			nonNestedTypeToEdit = defToEdit as TypeDef ?? defToEdit.DeclaringType;
			while (!(nonNestedTypeToEdit.DeclaringType is null))
				nonNestedTypeToEdit = nonNestedTypeToEdit.DeclaringType;
			methodSourceStatement = statementsInMethodToEdit.Count == 0 ? (MethodSourceStatement?)null : statementsInMethodToEdit[0];
			StartDecompile();
		}

		protected override DecompileCodeState CreateDecompileCodeState() =>
			new EditClassDecompileCodeState(defToEdit, methodSourceStatement);

		protected override Task<DecompileAsyncResult> DecompileAsync(DecompileCodeState decompileCodeState) {
			var state = (EditClassDecompileCodeState)decompileCodeState;
			state.CancellationToken.ThrowIfCancellationRequested();

			state.DecompilationContext.CalculateILSpans = true;
			var options = new DecompileTypeMethods(state.MainOutput, state.DecompilationContext, nonNestedTypeToEdit);
			options.DecompileHidden = false;
			options.ShowAll = true;
			decompiler.Decompile(DecompilationType.TypeMethods, options);

			state.CancellationToken.ThrowIfCancellationRequested();

			var result = new DecompileAsyncResult();
			result.AddDocument(MainCodeName, state.MainOutput.ToString(), state.MainOutput.Span);
			return Task.FromResult(result);
		}

		protected override void Import(ModuleImporter importer, CompilationResult result) =>
			importer.Import(result.RawFile!, result.DebugFile, nonNestedTypeToEdit);
	}
}
