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

using System;
using System.ComponentModel;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.Breakpoints {
	sealed class BreakpointVM : ViewModelBase, IDisposable {
		public bool IsEnabled {
			get { return Breakpoint.IsEnabled; }
			set { Breakpoint.IsEnabled = value; }
		}

		public object ImageObject => this;
		public object NameObject => this;
		public object AssemblyObject => this;
		public object ModuleObject => this;
		public object FileObject => this;

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

		public Breakpoint Breakpoint { get; }
		public IBreakpointContext Context { get; }

		readonly BreakpointsVM owner;

		public BreakpointVM(BreakpointsVM owner, IBreakpointContext context, Breakpoint bp) {
			this.owner = owner;
			this.Context = context;
			this.Breakpoint = bp;
			bp.PropertyChanged += Breakpoint_PropertyChanged;
		}

		void Breakpoint_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(Breakpoints.Breakpoint.IsEnabled)) {
				OnPropertyChanged(nameof(IsEnabled));
				RefreshImage();
			}
		}

		internal void RefreshThemeFields() {
			RefreshImage();
			RefreshNameField();
			OnPropertyChanged(nameof(AssemblyObject));
			OnPropertyChanged(nameof(ModuleObject));
			OnPropertyChanged(nameof(FileObject));
		}

		internal void RefreshNameField() => OnPropertyChanged(nameof(NameObject));
		void RefreshImage() => OnPropertyChanged(nameof(ImageObject));

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
			var file = Context.ModuleLoader.LoadModule(bp.SerializedDnToken.Module, canLoadDynFile, diskFileOk: true, isAutoLoaded: true);
			var mod = file == null ? null : file.ModuleDef;
			return mod == null ? null : mod.ResolveToken(bp.SerializedDnToken.Token) as MethodDef;
		}

		public void Dispose() {
			NameError = false;	// will notify owner if necessary
			Breakpoint.PropertyChanged -= Breakpoint_PropertyChanged;
		}
	}
}
