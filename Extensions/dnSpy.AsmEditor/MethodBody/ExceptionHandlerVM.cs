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
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class ExceptionHandlerVM : ViewModelBase, IIndexedItem {
		readonly ExceptionHandlerOptions origOptions;

		public ITypeSigCreator TypeSigCreator {
			set { typeSigCreator = value; }
		}
		ITypeSigCreator typeSigCreator;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand EditCatchTypeCommand => new RelayCommand(a => EditCatchType());

		public int Index {
			get => index;
			set {
				if (index != value) {
					index = value;
					OnPropertyChanged(nameof(Index));
				}
			}
		}
		int index;

		public ListVM<InstructionVM> TryStartVM { get; }
		public ListVM<InstructionVM> TryEndVM { get; }
		public ListVM<InstructionVM> FilterStartVM { get; }
		public ListVM<InstructionVM> HandlerStartVM { get; }
		public ListVM<InstructionVM> HandlerEndVM { get; }

		public ITypeDefOrRef CatchType {
			get => catchType;
			set {
				if (catchType != value) {
					catchType = value;
					OnPropertyChanged(nameof(CatchType));
				}
			}
		}
		ITypeDefOrRef catchType;

		internal static readonly EnumVM[] exceptionHandlerTypeList = new EnumVM[] {
			new EnumVM(ExceptionHandlerType.Catch, "Catch"),
			new EnumVM(ExceptionHandlerType.Filter, "Filter"),
			new EnumVM(ExceptionHandlerType.Finally, "Finally"),
			new EnumVM(ExceptionHandlerType.Fault, "Fault"),
		};
		public EnumListVM HandlerTypeVM { get; }

		readonly TypeSigCreatorOptions typeSigCreatorOptions;

		public ExceptionHandlerVM(TypeSigCreatorOptions typeSigCreatorOptions, ExceptionHandlerOptions options) {
			this.typeSigCreatorOptions = typeSigCreatorOptions.Clone(dnSpy_AsmEditor_Resources.CreateAnExceptionCatchType);
			this.typeSigCreatorOptions.IsLocal = false;
			this.typeSigCreatorOptions.NullTypeSigAllowed = true;
			origOptions = options;
			HandlerTypeVM = new EnumListVM(exceptionHandlerTypeList);
			TryStartVM = new ListVM<InstructionVM>((a, b) => OnSelectionChanged()) { DataErrorInfoDelegate = VerifyInstruction };
			TryEndVM = new ListVM<InstructionVM>((a, b) => OnSelectionChanged()) { DataErrorInfoDelegate = VerifyInstruction };
			FilterStartVM = new ListVM<InstructionVM>((a, b) => OnSelectionChanged()) { DataErrorInfoDelegate = VerifyInstruction };
			HandlerStartVM = new ListVM<InstructionVM>((a, b) => OnSelectionChanged()) { DataErrorInfoDelegate = VerifyInstruction };
			HandlerEndVM = new ListVM<InstructionVM>((a, b) => OnSelectionChanged()) { DataErrorInfoDelegate = VerifyInstruction };

			Reinitialize();
		}

		void OnSelectionChanged() => HasErrorUpdated();

		string VerifyInstruction(ListVM<InstructionVM> list) {
			var item = list.SelectedItem;
			var instr = item as InstructionVM;
			if (item != null && instr == null)
				return dnSpy_AsmEditor_Resources.Error_OnlyInstrsCanBeSelected;

			if (instr != null && instr.Index == -1)
				return dnSpy_AsmEditor_Resources.Error_InstrHasBeenRemoved;

			return string.Empty;
		}

		bool HasListError(ListVM<InstructionVM> list) => !string.IsNullOrEmpty(VerifyInstruction(list));

		public void InstructionChanged(IEnumerable<InstructionVM> instrs) {
			TryStartVM.InvalidateSelected(instrs, true, InstructionVM.Null);
			TryEndVM.InvalidateSelected(instrs, true, InstructionVM.Null);
			FilterStartVM.InvalidateSelected(instrs, true, InstructionVM.Null);
			HandlerStartVM.InvalidateSelected(instrs, true, InstructionVM.Null);
			HandlerEndVM.InvalidateSelected(instrs, true, InstructionVM.Null);
		}

		void EditCatchType() {
			if (typeSigCreator == null)
				throw new InvalidOperationException();

			var newType = typeSigCreator.Create(typeSigCreatorOptions, CatchType.ToTypeSig(), out bool canceled);
			if (canceled)
				return;

			CatchType = newType.ToTypeDefOrRef();
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public ExceptionHandlerOptions CreateExceptionHandlerOptions() => CopyTo(new ExceptionHandlerOptions());

		public void InitializeFrom(ExceptionHandlerOptions options) {
			TryStartVM.SelectedItem = options.TryStart ?? InstructionVM.Null;
			TryEndVM.SelectedItem = options.TryEnd ?? InstructionVM.Null;
			FilterStartVM.SelectedItem = options.FilterStart ?? InstructionVM.Null;
			HandlerStartVM.SelectedItem = options.HandlerStart ?? InstructionVM.Null;
			HandlerEndVM.SelectedItem = options.HandlerEnd ?? InstructionVM.Null;
			CatchType = options.CatchType;
			HandlerTypeVM.SelectedItem = options.HandlerType;
		}

		static InstructionVM RemoveNullInstance(InstructionVM vm) {
			Debug.Assert(vm != null);
			if (vm == null || vm == InstructionVM.Null)
				return null;
			return vm;
		}

		public ExceptionHandlerOptions CopyTo(ExceptionHandlerOptions options) {
			options.TryStart = RemoveNullInstance(TryStartVM.SelectedItem);
			options.TryEnd = RemoveNullInstance(TryEndVM.SelectedItem);
			options.FilterStart = RemoveNullInstance(FilterStartVM.SelectedItem);
			options.HandlerStart = RemoveNullInstance(HandlerStartVM.SelectedItem);
			options.HandlerEnd = RemoveNullInstance(HandlerEndVM.SelectedItem);
			options.CatchType = CatchType;
			options.HandlerType = (ExceptionHandlerType)HandlerTypeVM.SelectedItem;
			return options;
		}

		public override bool HasError {
			get {
				return HasListError(TryStartVM) ||
					HasListError(TryEndVM) ||
					HasListError(FilterStartVM) ||
					HasListError(HandlerStartVM) ||
					HasListError(HandlerEndVM);
			}
		}

		public IIndexedItem Clone() => new ExceptionHandlerVM(typeSigCreatorOptions, CreateExceptionHandlerOptions());
	}
}
