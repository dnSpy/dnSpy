/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Linq;
using System.Windows.Controls;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class SwitchOperandsListHelper : ListBoxHelperBase<SwitchInstructionVM> {
		protected override string AddNewBeforeSelectionMessage {
			get { return dnSpy_AsmEditor_Resources.Instr_Command1; }
		}

		protected override string AddNewAfterSelectionMessage {
			get { return dnSpy_AsmEditor_Resources.Instr_Command2; }
		}

		protected override string AppendNewMessage {
			get { return dnSpy_AsmEditor_Resources.Instr_Command3; }
		}

		protected override string RemoveSingularMessage {
			get { return dnSpy_AsmEditor_Resources.Instr_Command4; }
		}

		protected override string RemovePluralMessage {
			get { return dnSpy_AsmEditor_Resources.Instr_Command5; }
		}

		protected override string RemoveAllMessage {
			get { return dnSpy_AsmEditor_Resources.Instr_Command6; }
		}

		public SwitchOperandsListHelper(ListBox listBox)
			: base(listBox) {
		}

		protected override SwitchInstructionVM[] GetSelectedItems() {
			return listBox.SelectedItems.Cast<SwitchInstructionVM>().ToArray();
		}

		protected override void CopyItemsAsText(SwitchInstructionVM[] instrs) {
			Array.Sort(instrs, (a, b) => a.Index.CompareTo(b.Index));
			InstructionsListHelper.CopyItemsAsTextToClipboard(instrs.Select(a => a.InstructionVM).ToArray());
		}

		protected override void OnDataContextChangedInternal(object dataContext) {
			this.coll = ((SwitchOperandVM)dataContext).InstructionsListVM;
			AddStandardMenuHandlers();
		}
	}
}
