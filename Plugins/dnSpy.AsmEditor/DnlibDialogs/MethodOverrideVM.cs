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

using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.MVVM;
using dnSpy.Shared.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class MethodOverrideVM : ViewModelBase {
		readonly MethodOverrideOptions origOptions;
		MethodOverride methodOverride;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public string FullName => methodOverride.MethodDeclaration.ToString();

		public IMethodDefOrRef MethodBody {
			get { return methodOverride.MethodBody; }
			set {
				if (methodOverride.MethodBody != value) {
					methodOverride.MethodBody = value;
					OnPropertyChanged(nameof(MethodBody));
				}
			}
		}

		public IMethodDefOrRef MethodDeclaration {
			get { return methodOverride.MethodDeclaration; }
			set {
				if (methodOverride.MethodDeclaration != value) {
					methodOverride.MethodDeclaration = value;
					OnPropertyChanged(nameof(MethodDeclaration));
				}
			}
		}

		internal ModuleDef OwnerModule { get; }

		public MethodOverrideVM(MethodOverrideOptions options, ModuleDef ownerModule) {
			this.OwnerModule = ownerModule;
			this.origOptions = options;

			Reinitialize();
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public MethodOverrideOptions CreateMethodOverrideOptions() => CopyTo(new MethodOverrideOptions());
		void InitializeFrom(MethodOverrideOptions options) => methodOverride = options.Create();

		MethodOverrideOptions CopyTo(MethodOverrideOptions options) {
			options.MethodBody = methodOverride.MethodBody;
			options.MethodDeclaration = methodOverride.MethodDeclaration;
			return options;
		}
	}
}
