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
	sealed class DeclSecuritiesVM : ViewModelBase
	{
		public IEditDeclSecurity EditDeclSecurity {
			set { editDeclSecurity = value; }
		}
		IEditDeclSecurity editDeclSecurity;

		public ICommand EditCommand {
			get { return new RelayCommand(a => EditCurrent(), a => EditCurrentCanExecute()); }
		}

		public ICommand AddCommand {
			get { return new RelayCommand(a => AddCurrent(), a => AddCurrentCanExecute()); }
		}

		public MyObservableCollection<DeclSecurityVM> DeclSecurityCollection {
			get { return declSecurityCollection; }
		}
		readonly MyObservableCollection<DeclSecurityVM> declSecurityCollection = new MyObservableCollection<DeclSecurityVM>();

		readonly ModuleDef module;
		readonly Language language;

		public DeclSecuritiesVM(ModuleDef module, Language language)
		{
			this.module = module;
			this.language = language;
		}

		public void InitializeFrom(IEnumerable<DeclSecurity> decls)
		{
			DeclSecurityCollection.Clear();
			DeclSecurityCollection.AddRange(decls.Select(a => new DeclSecurityVM(new DeclSecurityOptions(a), module, language)));
		}

		public void EditCurrent()
		{
			if (!EditCurrentCanExecute())
				return;
			if (editDeclSecurity == null)
				throw new InvalidOperationException();
			int index = DeclSecurityCollection.SelectedIndex;
			var declVm = editDeclSecurity.Edit("Edit Security Declaration", new DeclSecurityVM(DeclSecurityCollection[index].CreateDeclSecurityOptions(), module, language));
			if (declVm != null) {
				DeclSecurityCollection[index] = declVm;
				DeclSecurityCollection.SelectedIndex = index;
			}
		}

		bool EditCurrentCanExecute()
		{
			return DeclSecurityCollection.SelectedIndex >= 0 && DeclSecurityCollection.SelectedIndex < DeclSecurityCollection.Count;
		}

		void AddCurrent()
		{
			if (!AddCurrentCanExecute())
				return;

			if (editDeclSecurity == null)
				throw new InvalidOperationException();
			var declVm = editDeclSecurity.Edit("Create Security Declaration", new DeclSecurityVM(new DeclSecurityOptions(), module, language));
			if (declVm != null) {
				DeclSecurityCollection.Add(declVm);
				DeclSecurityCollection.SelectedIndex = DeclSecurityCollection.Count - 1;
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
