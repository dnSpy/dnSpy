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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using dnSpy.MVVM;

namespace dnSpy.Debugger.Breakpoints {
	sealed class BreakpointsVM : ViewModelBase {
		public ObservableCollection<BreakpointVM> Collection {
			get { return breakpointList; }
		}
		readonly ObservableCollection<BreakpointVM> breakpointList;

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged("SelectedItem");
				}
			}
		}
		object selectedItem;

		public BreakpointsVM() {
			this.breakpointList = new ObservableCollection<BreakpointVM>();
			BreakpointSettings.Instance.PropertyChanged += BreakpointSettings_PropertyChanged;
			BreakpointManager.Instance.OnListModified += BreakpointManager_OnListModified;
			foreach (var bp in BreakpointManager.Instance.Breakpoints)
				AddBreakpoint(bp);
		}

		void BreakpointSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "ShowTokens")
				RefreshNameField();
		}

		public void Remove(IEnumerable<BreakpointVM> bps) {
			foreach (var bp in bps)
				BreakpointManager.Instance.Remove(bp.Breakpoint);
		}

		void BreakpointManager_OnListModified(object sender, BreakpointListModifiedEventArgs e) {
			if (e.Added)
				AddBreakpoint(e.Breakpoint);
			else
				RemoveBreakpoint(e.Breakpoint);
		}

		void AddBreakpoint(Breakpoint bp) {
			Collection.Add(new BreakpointVM(bp));
		}

		void RemoveBreakpoint(Breakpoint bp) {
			for (int i = 0; i < Collection.Count; i++) {
				var vm = Collection[i];
				if (Collection[i].Breakpoint == bp) {
					Collection.RemoveAt(i);
					vm.Dispose();
					return;
				}
			}
			Debug.Fail("Breakpoint got removed but it wasn't in BreakpointsVM's list");
		}

		internal void RefreshThemeFields() {
			foreach (var vm in breakpointList)
				vm.RefreshThemeFields();
		}

		internal void RefreshLanguageFields() {
			RefreshNameField();
		}

		void RefreshNameField() {
			foreach (var vm in breakpointList)
				vm.RefreshNameField();
		}
	}
}
