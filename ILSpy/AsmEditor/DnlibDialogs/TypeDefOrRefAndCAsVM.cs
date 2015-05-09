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
	sealed class TypeDefOrRefAndCAsVM : ViewModelBase
	{
		public IEditTypeDefOrRefAndCA EditTypeDefOrRefAndCA {
			set { editTypeDefOrRefAndCA = value; }
		}
		IEditTypeDefOrRefAndCA editTypeDefOrRefAndCA;

		public ICommand EditCommand {
			get { return new RelayCommand(a => EditCurrent(), a => EditCurrentCanExecute()); }
		}

		public ICommand AddCommand {
			get { return new RelayCommand(a => AddCurrent(), a => AddCurrentCanExecute()); }
		}

		public MyObservableCollection<TypeDefOrRefAndCAVM> TypeDefOrRefAndCACollection {
			get { return typeDefOrRefAndCACollection; }
		}
		readonly MyObservableCollection<TypeDefOrRefAndCAVM> typeDefOrRefAndCACollection = new MyObservableCollection<TypeDefOrRefAndCAVM>();

		readonly ModuleDef module;
		readonly Language language;
		readonly TypeDef ownerType;
		readonly MethodDef ownerMethod;
		readonly string editString;
		readonly string createString;

		public TypeDefOrRefAndCAsVM(ModuleDef module, Language language, TypeDef ownerType, MethodDef ownerMethod, string editString, string createString)
		{
			this.module = module;
			this.language = language;
			this.ownerType = ownerType;
			this.ownerMethod = ownerMethod;
			this.editString = editString;
			this.createString = createString;
		}

		public void InitializeFrom(IEnumerable<GenericParamConstraint> gpcs)
		{
			TypeDefOrRefAndCACollection.Clear();
			TypeDefOrRefAndCACollection.AddRange(gpcs.Select(a => new TypeDefOrRefAndCAVM(new TypeDefOrRefAndCAOptions(a), module, language, ownerType, ownerMethod)));
		}

		public void InitializeFrom(IEnumerable<InterfaceImpl> ifaces)
		{
			TypeDefOrRefAndCACollection.Clear();
			TypeDefOrRefAndCACollection.AddRange(ifaces.Select(a => new TypeDefOrRefAndCAVM(new TypeDefOrRefAndCAOptions(a), module, language, ownerType, ownerMethod)));
		}

		public void EditCurrent()
		{
			if (!EditCurrentCanExecute())
				return;
			if (editTypeDefOrRefAndCA == null)
				throw new InvalidOperationException();
			int index = TypeDefOrRefAndCACollection.SelectedIndex;
			var vm = editTypeDefOrRefAndCA.Edit(editString, new TypeDefOrRefAndCAVM(TypeDefOrRefAndCACollection[index].CreateTypeDefOrRefAndCAOptions(), module, language, ownerType, ownerMethod));
			if (vm != null) {
				TypeDefOrRefAndCACollection[index] = vm;
				TypeDefOrRefAndCACollection.SelectedIndex = index;
			}
		}

		bool EditCurrentCanExecute()
		{
			return TypeDefOrRefAndCACollection.SelectedIndex >= 0 && TypeDefOrRefAndCACollection.SelectedIndex < TypeDefOrRefAndCACollection.Count;
		}

		void AddCurrent()
		{
			if (!AddCurrentCanExecute())
				return;

			if (editTypeDefOrRefAndCA == null)
				throw new InvalidOperationException();
			var vm = editTypeDefOrRefAndCA.Edit(createString, new TypeDefOrRefAndCAVM(new TypeDefOrRefAndCAOptions(), module, language, ownerType, ownerMethod));
			if (vm != null) {
				TypeDefOrRefAndCACollection.Add(vm);
				TypeDefOrRefAndCACollection.SelectedIndex = TypeDefOrRefAndCACollection.Count - 1;
			}
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
