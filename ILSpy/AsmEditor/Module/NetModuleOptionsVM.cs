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
using System.Windows.Input;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.AsmEditor.Module
{
	sealed class NetModuleOptionsVM : ViewModelBase
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

		internal static readonly EnumVM[] clrVersionList = new EnumVM[] {
			new EnumVM(Module.ClrVersion.CLR10, "1.0"),
			new EnumVM(Module.ClrVersion.CLR11, "1.1"),
			new EnumVM(Module.ClrVersion.CLR20, "2.0 - 3.5"),
			new EnumVM(Module.ClrVersion.CLR40, "4.0 - 4.6"),
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
			get { return new RelayCommand(a => Mvid.Value = Guid.NewGuid()); }
		}

		public NetModuleOptionsVM(ModuleDef module = null)
		{
			Name = "MyNetModule.netmodule";
			mvid = new GuidVM(Guid.NewGuid(), a => HasErrorUpdated());
			ClrVersion.SelectedItem = GetClrVersion(module);
		}

		static Module.ClrVersion GetClrVersion(ModuleDef module)
		{
			if (module == null)
				return Module.ClrVersion.DefaultVersion;

			if (module.IsClr10) return Module.ClrVersion.CLR10;
			if (module.IsClr11) return Module.ClrVersion.CLR11;
			if (module.IsClr20) return Module.ClrVersion.CLR20;
			if (module.IsClr40) return Module.ClrVersion.CLR40;

			return Module.ClrVersion.DefaultVersion;
		}

		public NetModuleOptions CreateNetModuleOptions()
		{
			var options = new NetModuleOptions();
			options.Name = Name ?? UTF8String.Empty;
			options.ClrVersion = (ClrVersion)ClrVersion.SelectedItem;
			options.Mvid = Mvid.Value;
			return options;
		}

		protected override string Verify(string columnName)
		{
			return string.Empty;
		}

		public override bool HasError {
			get { return mvid.HasError; }
		}
	}
}
