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
using dndbg.Engine;
using dnlib.DotNet;
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

		internal bool NameError {
			get { return nameError; }
			set {
				if (nameError != value) {
					nameError = value;
					owner.OnNameErrorChanged(this);
				}
			}
		}
		bool nameError;

		public Breakpoint Breakpoint {
			get { return bp; }
		}
		readonly Breakpoint bp;

		readonly BreakpointsVM owner;

		public BreakpointVM(BreakpointsVM owner, Breakpoint bp) {
			this.owner = owner;
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

		internal void RefreshIfNameError(SerializedDnModule serMod) {
			if (!NameError)
				return;

			var dnbp = Breakpoint.DnBreakpoint as DnILCodeBreakpoint;
			if (dnbp == null)
				return;
			if (dnbp.Module != serMod)
				return;

			// If we still can't resolve the method, there's no need to refresh the name field
			if (GetMethodDef(true) == null)
				return;

			RefreshNameField();
		}

		internal MethodDef GetMethodDef(bool canLoadDynFile) {
			var bp = Breakpoint as ILCodeBreakpoint;
			if (bp == null)
				return null;
			var file = ModuleLoader.Instance.LoadModule(bp.SerializedDnSpyToken.Module, canLoadDynFile);
			var mod = file == null ? null : file.ModuleDef;
			return mod == null ? null : mod.ResolveToken(bp.SerializedDnSpyToken.Token) as MethodDef;
		}

		public void Dispose() {
			NameError = false;	// will notify owner if necessary
			bp.PropertyChanged -= Breakpoint_PropertyChanged;
		}
	}
}
