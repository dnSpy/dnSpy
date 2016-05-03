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

using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using dnSpy.AsmEditor.Commands;
using dnSpy.Shared.MVVM;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class SwitchOperandVM : ViewModelBase {
		readonly IList<InstructionVM> origInstructions;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand AddInstructionCommand => new RelayCommand(a => AddInstruction(), a => AddInstructionCanExecute());
		public ICommand AppendInstructionCommand => new RelayCommand(a => AppendInstruction(), a => AppendInstructionCanExecute());

		public int SelectedIndex {
			get { return selectedIndex; }
			set {
				if (selectedIndex != value) {
					selectedIndex = value;
					OnPropertyChanged("SelectedIndex");
				}
			}
		}
		int selectedIndex;

		public IndexObservableCollection<SwitchInstructionVM> InstructionsListVM { get; }
		public ListVM<InstructionVM> AllInstructionsVM { get; }

		public SwitchOperandVM(IList<InstructionVM> allInstrs, IList<InstructionVM> instrs) {
			this.AllInstructionsVM = new ListVM<InstructionVM>(allInstrs);
			this.origInstructions = instrs;
			this.InstructionsListVM = new IndexObservableCollection<SwitchInstructionVM>();

			Reinitialize();
		}

		void AddInstruction() =>
			InstructionsListVM.Insert(SelectedIndex + 1, new SwitchInstructionVM(AllInstructionsVM.SelectedItem));
		bool AddInstructionCanExecute() => SelectedIndex >= 0;
		void AppendInstruction() =>
			InstructionsListVM.Insert(InstructionsListVM.Count, new SwitchInstructionVM(AllInstructionsVM.SelectedItem));
		bool AppendInstructionCanExecute() => true;
		void Reinitialize() => InitializeFrom(origInstructions);

		public void InitializeFrom(IList<InstructionVM> instrs) {
			InstructionsListVM.Clear();
			if (instrs != null)
				InstructionsListVM.AddRange(instrs.Select(a => new SwitchInstructionVM(a)));
			SelectedIndex = InstructionsListVM.Count == 0 ? -1 : 0;
		}

		public InstructionVM[] GetSwitchList() => InstructionsListVM.Select(a => a.InstructionVM).ToArray();
	}
}
