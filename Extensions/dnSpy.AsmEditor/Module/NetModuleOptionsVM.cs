/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Module {
	sealed class NetModuleOptionsVM : ViewModelBase {
		public string? Name {
			get => name;
			set {
				if (!StringComparer.Ordinal.Equals(name, value)) {
					name = value;
					OnPropertyChanged(nameof(Name));
				}
			}
		}
		string? name;

		internal static readonly EnumVM[] clrVersionList = new EnumVM[] {
			new EnumVM(Module.ClrVersion.CLR10, "1.0"),
			new EnumVM(Module.ClrVersion.CLR11, "1.1"),
			new EnumVM(Module.ClrVersion.CLR20, "2.0 - 3.5"),
			new EnumVM(Module.ClrVersion.CLR40, "4.0 - 4.8"),
		};

		public EnumListVM ClrVersion { get; } = new EnumListVM(clrVersionList);
		public GuidVM Mvid { get; }
		public ICommand GenerateNewMvidCommand => new RelayCommand(a => Mvid.Value = Guid.NewGuid());

		public NetModuleOptionsVM(ModuleDef? module = null) {
			Name = "MyNetModule.netmodule";
			Mvid = new GuidVM(Guid.NewGuid(), a => HasErrorUpdated());
			ClrVersion.SelectedItem = GetClrVersion(module);
		}

		static Module.ClrVersion GetClrVersion(ModuleDef? module) {
			if (module is null)
				return Module.ClrVersion.DefaultVersion;

			if (module.IsClr10) return Module.ClrVersion.CLR10;
			if (module.IsClr11) return Module.ClrVersion.CLR11;
			if (module.IsClr20) return Module.ClrVersion.CLR20;
			if (module.IsClr40) return Module.ClrVersion.CLR40;

			return Module.ClrVersion.DefaultVersion;
		}

		public NetModuleOptions CreateNetModuleOptions() {
			var options = new NetModuleOptions();
			options.Name = Name ?? UTF8String.Empty;
			options.ClrVersion = (ClrVersion)ClrVersion.SelectedItem!;
			options.Mvid = Mvid.Value;
			return options;
		}

		public override bool HasError => Mvid.HasError;
	}
}
