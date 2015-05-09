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
	sealed class GenericParamsVM : ViewModelBase
	{
		public IEditGenericParam EditGenericParam {
			set { editGenericParam = value; }
		}
		IEditGenericParam editGenericParam;

		public ICommand EditCommand {
			get { return new RelayCommand(a => EditCurrent(), a => EditCurrentCanExecute()); }
		}

		public ICommand AddCommand {
			get { return new RelayCommand(a => AddCurrent(), a => AddCurrentCanExecute()); }
		}

		public MyObservableCollection<GenericParamVM> GenericParamCollection {
			get { return genericParamCollection; }
		}
		readonly MyObservableCollection<GenericParamVM> genericParamCollection = new MyObservableCollection<GenericParamVM>();

		readonly ModuleDef module;
		readonly Language language;
		readonly TypeDef ownerType;
		readonly MethodDef ownerMethod;

		public GenericParamsVM(ModuleDef module, Language language, TypeDef ownerType, MethodDef ownerMethod)
		{
			this.module = module;
			this.language = language;
			this.ownerType = ownerType;
			this.ownerMethod = ownerMethod;
		}

		public void InitializeFrom(IEnumerable<GenericParam> gps)
		{
			GenericParamCollection.Clear();
			GenericParamCollection.AddRange(gps.Select(a => new GenericParamVM(new GenericParamOptions(a), module, language, ownerType, ownerMethod)));
		}

		public void EditCurrent()
		{
			if (!EditCurrentCanExecute())
				return;
			if (editGenericParam == null)
				throw new InvalidOperationException();
			int index = GenericParamCollection.SelectedIndex;
			var gpVm = editGenericParam.Edit("Edit Generic Parameter", new GenericParamVM(GenericParamCollection[index].CreateGenericParamOptions(), module, language, ownerType, ownerMethod));
			if (gpVm != null) {
				GenericParamCollection[index] = gpVm;
				GenericParamCollection.SelectedIndex = index;
			}
		}

		bool EditCurrentCanExecute()
		{
			return GenericParamCollection.SelectedIndex >= 0 && GenericParamCollection.SelectedIndex < GenericParamCollection.Count;
		}

		void AddCurrent()
		{
			if (!AddCurrentCanExecute())
				return;

			if (editGenericParam == null)
				throw new InvalidOperationException();
			var gpVm = editGenericParam.Edit("Create Generic Parameter", new GenericParamVM(new GenericParamOptions(), module, language, ownerType, ownerMethod));
			if (gpVm != null) {
				int index = GetGenericParamCollectionIndex(gpVm.Number.Value);
				GenericParamCollection.Insert(index, gpVm);
				GenericParamCollection.SelectedIndex = index;
			}
		}

		int GetGenericParamCollectionIndex(int number)
		{
			for (int i = 0; i < GenericParamCollection.Count; i++) {
				if (number < GenericParamCollection[i].Number.Value)
					return i;
			}
			return GenericParamCollection.Count;
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
