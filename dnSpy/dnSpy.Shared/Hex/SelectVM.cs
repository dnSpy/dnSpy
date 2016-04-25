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

using dnSpy.Shared.MVVM;

namespace dnSpy.Shared.Hex {
	sealed class SelectVM : ViewModelBase {
		public UInt64VM StartVM {
			get { return startVM; }
		}
		UInt64VM startVM;

		public UInt64VM EndVM {
			get { return endVM; }
		}
		UInt64VM endVM;

		public SelectVM(ulong start, ulong end, ulong min, ulong max) {
			this.startVM = new UInt64VM(start, a => HasErrorUpdated(), false) {
				Min = min,
				Max = max,
			};
			this.endVM = new UInt64VM(end, a => HasErrorUpdated(), false) {
				Min = min,
				Max = max,
			};
		}

		public override bool HasError {
			get { return StartVM.HasError || EndVM.HasError; }
		}
	}
}
