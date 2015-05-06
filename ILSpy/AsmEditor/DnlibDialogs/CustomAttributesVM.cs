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
	sealed class CustomAttributesVM : ViewModelBase
	{
		public IEditCustomAttribute EditCustomAttribute {
			set { editCustomAttribute = value; }
		}
		IEditCustomAttribute editCustomAttribute;

		public ICommand EditCommand {
			get { return new RelayCommand(a => EditCurrent(), a => EditCurrentCanExecute()); }
		}

		public ICommand AddCommand {
			get { return new RelayCommand(a => AddCurrent(), a => AddCurrentCanExecute()); }
		}

		public MyObservableCollection<CustomAttributeVM> CustomAttributeCollection {
			get { return customAttributeCollection; }
		}
		readonly MyObservableCollection<CustomAttributeVM> customAttributeCollection = new MyObservableCollection<CustomAttributeVM>();

		readonly TypeSigCreatorOptions typeSigCreatorOptions;

		public CustomAttributesVM(ModuleDef module, Language language)
		{
			this.typeSigCreatorOptions = new TypeSigCreatorOptions(module, language);
		}

		public void InitializeFrom(IEnumerable<CustomAttribute> cas)
		{
			CustomAttributeCollection.Clear();
			CustomAttributeCollection.AddRange(cas.Select(a => new CustomAttributeVM(new CustomAttributeOptions(a), typeSigCreatorOptions)));
		}

		public void EditCurrent()
		{
			if (!EditCurrentCanExecute())
				return;
			if (editCustomAttribute == null)
				throw new InvalidOperationException();
			int index = CustomAttributeCollection.SelectedIndex;
			var caVm = editCustomAttribute.Edit("Edit Custom Attribute", new CustomAttributeVM(CustomAttributeCollection[index].CreateCustomAttributeOptions(), typeSigCreatorOptions));
			if (caVm != null) {
				CustomAttributeCollection[index] = caVm;
				CustomAttributeCollection.SelectedIndex = index;
			}
		}

		bool EditCurrentCanExecute()
		{
			return CustomAttributeCollection.SelectedIndex >= 0 && CustomAttributeCollection.SelectedIndex < CustomAttributeCollection.Count;
		}

		void AddCurrent()
		{
			if (!AddCurrentCanExecute())
				return;

			if (editCustomAttribute == null)
				throw new InvalidOperationException();
			var caVm = editCustomAttribute.Edit("Create Custom Attribute", new CustomAttributeVM(new CustomAttributeOptions(), typeSigCreatorOptions));
			if (caVm != null) {
				CustomAttributeCollection.Add(caVm);
				CustomAttributeCollection.SelectedIndex = CustomAttributeCollection.Count - 1;
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
