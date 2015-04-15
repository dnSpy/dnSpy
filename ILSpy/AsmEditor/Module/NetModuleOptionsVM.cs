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
using System.ComponentModel;
using System.Windows.Input;

namespace ICSharpCode.ILSpy.AsmEditor.Module
{
	sealed class NetModuleOptionsVM : INotifyPropertyChanged, IDataErrorInfo
	{
		public string Name {
			get { return name; }
			set {
				if (!value.Equals(name, StringComparison.Ordinal)) {
					name = value;
					OnPropertyChanged("Name");
				}
			}
		}
		string name;

		static readonly EnumVM[] clrVersionList = new EnumVM[] {
			new EnumVM(Module.ClrVersion.CLR10, "1.0"),
			new EnumVM(Module.ClrVersion.CLR11, "1.1"),
			new EnumVM(Module.ClrVersion.CLR20, "2.0 - 3.5"),
			new EnumVM(Module.ClrVersion.CLR40, "4.0 - 4.5"),
		};

		public EnumListVM ClrVersion {
			get { return clrVersionVM; }
		}
		readonly EnumListVM clrVersionVM = new EnumListVM(clrVersionList);

		public GuidVM Mvid {
			get { return mvid; }
		}
		GuidVM mvid;

		public ICommand GenerateNewMvidCommand {
			get { return generateNewMvidCommand ?? (generateNewMvidCommand = new RelayCommand(a => Mvid.Value = Guid.NewGuid())); }
		}
		ICommand generateNewMvidCommand;

		public NetModuleOptionsVM()
		{
			Name = "MyNetModule.netmodule";
			mvid = new GuidVM(Guid.NewGuid(), a => HasErrorUpdated());
			ClrVersion.SelectedItem = Module.ClrVersion.CLR40;
		}

		public NetModuleOptions CreateNetModuleOptions()
		{
			var options = new NetModuleOptions();
			options.Name = Name;
			options.ClrVersion = (ClrVersion)ClrVersion.SelectedItem;
			options.Mvid = Mvid.Value;
			return options;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		public string Error {
			get { throw new NotImplementedException(); }
		}

		public string this[string columnName] {
			get {
				HasErrorUpdated();
				return Verify(columnName);
			}
		}

		string Verify(string columnName)
		{
			return string.Empty;
		}

		void HasErrorUpdated()
		{
			OnPropertyChanged("HasError");
			OnPropertyChanged("HasNoError");
		}

		public bool HasNoError {
			get { return !HasError; }
		}

		public bool HasError {
			get { return mvid.HasError; }
		}
	}
}
