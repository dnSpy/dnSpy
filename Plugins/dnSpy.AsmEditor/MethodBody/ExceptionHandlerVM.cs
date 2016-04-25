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
using dnSpy.Shared.MVVM;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class ExceptionHandlerVM : ViewModelBase, IIndexedItem {
		readonly ExceptionHandlerOptions origOptions;

		public ITypeSigCreator TypeSigCreator {
			set { typeSigCreator = value; }
		}
		ITypeSigCreator typeSigCreator;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public ICommand EditCatchTypeCommand {
			get { return new RelayCommand(a => EditCatchType()); }
		}

		public int Index {
			get { return index; }
			set {
				if (index != value) {
					index = value;
					OnPropertyChanged("Index");
				}
			}
		}
		int index;

		public ListVM<InstructionVM> TryStartVM {
			get { return tryStartVM; }
		}
		readonly ListVM<InstructionVM> tryStartVM;

		public ListVM<InstructionVM> TryEndVM {
			get { return tryEndVM; }
		}
		readonly ListVM<InstructionVM> tryEndVM;

		public ListVM<InstructionVM> FilterStartVM {
			get { return filterStartVM; }
		}
		readonly ListVM<InstructionVM> filterStartVM;

		public ListVM<InstructionVM> HandlerStartVM {
			get { return handlerStartVM; }
		}
		readonly ListVM<InstructionVM> handlerStartVM;

		public ListVM<InstructionVM> HandlerEndVM {
			get { return handlerEndVM; }
		}
		readonly ListVM<InstructionVM> handlerEndVM;

		public ITypeDefOrRef CatchType {
			get { return catchType; }
			set {
				if (catchType != value) {
					catchType = value;
					OnPropertyChanged("CatchType");
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
		public EnumListVM HandlerTypeVM {
			get { return handlerTypeVM; }
		}
		readonly EnumListVM handlerTypeVM;

		readonly TypeSigCreatorOptions typeSigCreatorOptions;

		public ExceptionHandlerVM(TypeSigCreatorOptions typeSigCreatorOptions, ExceptionHandlerOptions options) {
			this.typeSigCreatorOptions = typeSigCreatorOptions.Clone(dnSpy_AsmEditor_Resources.CreateAnExceptionCatchType);
			this.typeSigCreatorOptions.IsLocal = false;
			this.typeSigCreatorOptions.NullTypeSigAllowed = true;
			this.origOptions = options;
			this.handlerTypeVM = new EnumListVM(exceptionHandlerTypeList);
			this.tryStartVM = new ListVM<InstructionVM>((a, b) => OnSelectionChanged()) { DataErrorInfoDelegate = VerifyInstruction };
			this.tryEndVM = new ListVM<InstructionVM>((a, b) => OnSelectionChanged()) { DataErrorInfoDelegate = VerifyInstruction };
			this.filterStartVM = new ListVM<InstructionVM>((a, b) => OnSelectionChanged()) { DataErrorInfoDelegate = VerifyInstruction };
			this.handlerStartVM = new ListVM<InstructionVM>((a, b) => OnSelectionChanged()) { DataErrorInfoDelegate = VerifyInstruction };
			this.handlerEndVM = new ListVM<InstructionVM>((a, b) => OnSelectionChanged()) { DataErrorInfoDelegate = VerifyInstruction };

			Reinitialize();
		}

		void OnSelectionChanged() {
			HasErrorUpdated();
		}

		string VerifyInstruction(ListVM<InstructionVM> list) {
			var item = list.SelectedItem;
			var instr = item as InstructionVM;
			if (item != null && instr == null)
				return dnSpy_AsmEditor_Resources.Error_OnlyInstrsCanBeSelected;

			if (instr != null && instr.Index == -1)
				return dnSpy_AsmEditor_Resources.Error_InstrHasBeenRemoved;

			return string.Empty;
		}

		bool HasListError(ListVM<InstructionVM> list) {
			return !string.IsNullOrEmpty(VerifyInstruction(list));
		}

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

			bool canceled;
			var newType = typeSigCreator.Create(typeSigCreatorOptions, CatchType.ToTypeSig(), out canceled);
			if (canceled)
				return;

			CatchType = newType.ToTypeDefOrRef();
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public ExceptionHandlerOptions CreateExceptionHandlerOptions() {
			return CopyTo(new ExceptionHandlerOptions());
		}

		public void InitializeFrom(ExceptionHandlerOptions options) {
			this.TryStartVM.SelectedItem = options.TryStart ?? InstructionVM.Null;
			this.TryEndVM.SelectedItem = options.TryEnd ?? InstructionVM.Null;
			this.FilterStartVM.SelectedItem = options.FilterStart ?? InstructionVM.Null;
			this.HandlerStartVM.SelectedItem = options.HandlerStart ?? InstructionVM.Null;
			this.HandlerEndVM.SelectedItem = options.HandlerEnd ?? InstructionVM.Null;
			this.CatchType = options.CatchType;
			this.HandlerTypeVM.SelectedItem = options.HandlerType;
		}

		static InstructionVM RemoveNullInstance(InstructionVM vm) {
			Debug.Assert(vm != null);
			if (vm == null || vm == InstructionVM.Null)
				return null;
			return vm;
		}

		public ExceptionHandlerOptions CopyTo(ExceptionHandlerOptions options) {
			options.TryStart = RemoveNullInstance(this.TryStartVM.SelectedItem);
			options.TryEnd = RemoveNullInstance(this.TryEndVM.SelectedItem);
			options.FilterStart = RemoveNullInstance(this.FilterStartVM.SelectedItem);
			options.HandlerStart = RemoveNullInstance(this.HandlerStartVM.SelectedItem);
			options.HandlerEnd = RemoveNullInstance(this.HandlerEndVM.SelectedItem);
			options.CatchType = this.CatchType;
			options.HandlerType = (ExceptionHandlerType)this.HandlerTypeVM.SelectedItem;
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

		public IIndexedItem Clone() {
			return new ExceptionHandlerVM(typeSigCreatorOptions, CreateExceptionHandlerOptions());
		}
	}
}
