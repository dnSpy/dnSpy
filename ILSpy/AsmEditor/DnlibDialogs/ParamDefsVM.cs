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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	sealed class ParamDefsVM : ViewModelBase
	{
		public IEditParamDef EditParamDef {
			set { editParamDef = value; }
		}
		IEditParamDef editParamDef;

		public ICommand EditCommand {
			get { return new RelayCommand(a => EditCurrent(), a => EditCurrentCanExecute()); }
		}

		public ICommand AddCommand {
			get { return new RelayCommand(a => AddCurrent(), a => AddCurrentCanExecute()); }
		}

		public MyObservableCollection<ParamDefVM> ParamDefCollection {
			get { return paramDefCollection; }
		}
		readonly MyObservableCollection<ParamDefVM> paramDefCollection = new MyObservableCollection<ParamDefVM>();

		readonly ModuleDef module;
		readonly Language language;
		readonly TypeDef ownerType;
		readonly MethodDef ownerMethod;

		public ParamDefsVM(ModuleDef module, Language language, TypeDef ownerType, MethodDef ownerMethod)
		{
			this.module = module;
			this.language = language;
			this.ownerType = ownerType;
			this.ownerMethod = ownerMethod;
		}

		public void InitializeFrom(IEnumerable<ParamDef> pds)
		{
			ParamDefCollection.Clear();
			ParamDefCollection.AddRange(pds.Select(a => new ParamDefVM(new ParamDefOptions(a), module, language, ownerType, ownerMethod)));
		}

		public void EditCurrent()
		{
			if (!EditCurrentCanExecute())
				return;
			if (editParamDef == null)
				throw new InvalidOperationException();
			int index = ParamDefCollection.SelectedIndex;
			var caVm = editParamDef.Edit("Edit Parameter", new ParamDefVM(ParamDefCollection[index].CreateParamDefOptions(), module, language, ownerType, ownerMethod));
			if (caVm != null) {
				ParamDefCollection[index] = caVm;
				ParamDefCollection.SelectedIndex = index;
			}
		}

		bool EditCurrentCanExecute()
		{
			return ParamDefCollection.SelectedIndex >= 0 && ParamDefCollection.SelectedIndex < ParamDefCollection.Count;
		}

		void AddCurrent()
		{
			if (!AddCurrentCanExecute())
				return;

			if (editParamDef == null)
				throw new InvalidOperationException();
			var caVm = editParamDef.Edit("Create Parameter", new ParamDefVM(new ParamDefOptions(), module, language, ownerType, ownerMethod));
			if (caVm != null) {
				int index = GetParamDefCollectionIndex(caVm.Sequence.Value);
				ParamDefCollection.Insert(index, caVm);
				ParamDefCollection.SelectedIndex = index;
			}
		}

		int GetParamDefCollectionIndex(int sequence)
		{
			for (int i = 0; i < ParamDefCollection.Count; i++) {
				if (sequence < ParamDefCollection[i].Sequence.Value)
					return i;
			}
			return ParamDefCollection.Count;
		}

		bool AddCurrentCanExecute()
		{
			return true;
		}

		protected override string Verify(string columnName)
		{
			return string.Empty;
		}

		public override bool HasError {
			get { return false; }
		}
	}
}
