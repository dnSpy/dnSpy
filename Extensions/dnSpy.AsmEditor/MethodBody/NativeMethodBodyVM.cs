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
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.MethodBody {
	sealed class NativeMethodBodyVM : ViewModelBase {
		readonly NativeMethodBodyOptions origOptions;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public UInt32VM RVA { get; }

		public NativeMethodBodyVM(NativeMethodBodyOptions options, bool initialize) {
			this.origOptions = options;
			this.RVA = new UInt32VM(a => HasErrorUpdated());

			if (initialize)
				Reinitialize();
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public NativeMethodBodyOptions CreateNativeMethodBodyOptions() => CopyTo(new NativeMethodBodyOptions());
		public void InitializeFrom(NativeMethodBodyOptions options) => this.RVA.Value = (uint)options.RVA;

		public NativeMethodBodyOptions CopyTo(NativeMethodBodyOptions options) {
			options.RVA = (dnlib.PE.RVA)(uint)this.RVA.Value;
			return options;
		}

		public override bool HasError => RVA.HasError;
	}
}
