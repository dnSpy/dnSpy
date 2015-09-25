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
using dnSpy.MVVM;

namespace dnSpy.Debugger.Breakpoints {
	sealed class BreakpointVM : ViewModelBase, IDisposable {
		public bool IsEnabled {
			get { return bp.IsEnabled; }
			set { bp.IsEnabled = value; }
		}

		public object ImageObject { get { return this; } }
		public object NameObject { get { return this; } }
		public object AssemblyObject { get { return this; } }
		public object ModuleObject { get { return this; } }
		public object FileObject { get { return this; } }

		public Breakpoint Breakpoint {
			get { return bp; }
		}
		readonly Breakpoint bp;

		public BreakpointVM(Breakpoint bp) {
			this.bp = bp;
			bp.PropertyChanged += Breakpoint_PropertyChanged;
		}

		void Breakpoint_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "IsEnabled") {
				OnPropertyChanged("IsEnabled");
				RefreshImage();
			}
		}

		internal void RefreshThemeFields() {
			RefreshImage();
			RefreshNameField();
			OnPropertyChanged("AssemblyObject");
			OnPropertyChanged("ModuleObject");
			OnPropertyChanged("FileObject");
		}

		internal void RefreshNameField() {
			OnPropertyChanged("NameObject");
		}

		void RefreshImage() {
			OnPropertyChanged("ImageObject");
		}

		public void Dispose() {
			bp.PropertyChanged -= Breakpoint_PropertyChanged;
		}
	}
}
