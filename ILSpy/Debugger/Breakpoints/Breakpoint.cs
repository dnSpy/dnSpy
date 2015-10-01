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

using System.ComponentModel;
using System.Diagnostics;
using dndbg.Engine;

namespace dnSpy.Debugger.Breakpoints {
	public enum BreakpointType {
		ILCode,
		DebugEvent,
	}

	public abstract class Breakpoint : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propName) {
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		public abstract BreakpointType Type { get; }

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

		public bool IsILCode {
			get { return Type == BreakpointType.ILCode; }
		}

		public bool IsDebugEvent {
			get { return Type == BreakpointType.DebugEvent; }
		}

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

		protected virtual void OnIsEnabledChanged() {
		}
	}
}
