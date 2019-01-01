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

using System.Threading.Tasks;
using dnlib.DotNet;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.AsmEditor.Compiler {
	sealed class AddMembersCodeVM : EditCodeVM {
		readonly TypeDef nonNestedType;
		readonly IMemberDef defToEdit;

		sealed class AddMembersDecompileCodeState : DecompileCodeState {
			public ReferenceDecompilerOutput MainOutput { get; }
			public StringBuilderDecompilerOutput HiddenOutput { get; }

			public AddMembersDecompileCodeState(object referenceToEdit) {
				MainOutput = new ReferenceDecompilerOutput(referenceToEdit, null);
				HiddenOutput = new StringBuilderDecompilerOutput();
			}
		}

		public AddMembersCodeVM(EditCodeVMOptions options, IMemberDef defToEdit)
			: base(options, defToEdit as TypeDef ?? defToEdit.DeclaringType) {
			this.defToEdit = defToEdit;
			nonNestedType = defToEdit as TypeDef ?? defToEdit.DeclaringType;
			while (nonNestedType.DeclaringType != null)
				nonNestedType = nonNestedType.DeclaringType;
			StartDecompile();
		}

		protected override DecompileCodeState CreateDecompileCodeState() =>
			new AddMembersDecompileCodeState(defToEdit as TypeDef ?? defToEdit.DeclaringType);

		protected override Task<DecompileAsyncResult> DecompileAsync(DecompileCodeState decompileCodeState) {
			var state = (AddMembersDecompileCodeState)decompileCodeState;
			state.CancellationToken.ThrowIfCancellationRequested();

			DecompileTypeMethods options;

			options = new DecompileTypeMethods(state.MainOutput, state.DecompilationContext, nonNestedType);
			options.Types.Add(nonNestedType);
			options.Types.AddRange(nonNestedType.GetTypes());
			options.DecompileHidden = false;
			decompiler.Decompile(DecompilationType.TypeMethods, options);

			state.CancellationToken.ThrowIfCancellationRequested();

			options = new DecompileTypeMethods(state.HiddenOutput, state.DecompilationContext, nonNestedType);
			options.Types.Add(nonNestedType);
			options.Types.AddRange(nonNestedType.GetTypes());
			options.DecompileHidden = true;
			decompiler.Decompile(DecompilationType.TypeMethods, options);

			state.CancellationToken.ThrowIfCancellationRequested();

			var result = new DecompileAsyncResult();
			result.AddDocument(MainCodeName, state.MainOutput.ToString(), state.MainOutput.Span);
			result.AddDocument(MainGeneratedCodeName, state.HiddenOutput.ToString(), null);
			return Task.FromResult(result);
		}

		protected override void Import(ModuleImporter importer, CompilationResult result) =>
			importer.ImportNewMembers(result.RawFile, result.DebugFile, nonNestedType);
	}
}
