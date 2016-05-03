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

using System.ComponentModel;
using System.Diagnostics;
using dndbg.Engine;

namespace dnSpy.Debugger.Breakpoints {
	enum BreakpointKind {
		ILCode,
		DebugEvent,
	}

	abstract class Breakpoint : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propName) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		public abstract BreakpointKind Kind { get; }

		internal DnBreakpoint DnBreakpoint {
			get { return dnbp; }
			set {
				Debug.Assert(dnbp == null || value == null);
				dnbp = value;
				if (dnbp != null)
					dnbp.IsEnabled = IsEnabled;
			}
		}
		DnBreakpoint dnbp;

		public bool IsILCode => Kind == BreakpointKind.ILCode;
		public bool IsDebugEvent => Kind == BreakpointKind.DebugEvent;

		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					isEnabled = value;
					if (dnbp != null)
						dnbp.IsEnabled = isEnabled;
					OnIsEnabledChanged();
					OnPropertyChanged("IsEnabled");
				}
			}
		}
		bool isEnabled;

		protected Breakpoint(bool isEnabled) {
			this.isEnabled = isEnabled;
		}

		protected virtual void OnIsEnabledChanged() { }
	}
}
