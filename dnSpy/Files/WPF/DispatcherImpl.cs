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
using System.Diagnostics;
using System.Windows.Threading;

namespace dnSpy.Files.WPF {
	sealed class DispatcherImpl : IDispatcher {
		readonly Dispatcher disp;

		public DispatcherImpl(Dispatcher disp) {
			this.disp = disp;
		}

		System.Windows.Threading.DispatcherPriority Convert(DispatcherPrio priority) {
			switch (priority) {
			case DispatcherPrio.ContextIdle: return System.Windows.Threading.DispatcherPriority.ContextIdle;
			case DispatcherPrio.Background: return System.Windows.Threading.DispatcherPriority.Background;
			case DispatcherPrio.Loaded: return System.Windows.Threading.DispatcherPriority.Loaded;
			case DispatcherPrio.Send: return System.Windows.Threading.DispatcherPriority.Send;
			default:
				Debug.Fail("Unknown prio");
				return System.Windows.Threading.DispatcherPriority.Background;
			}
		}

		public void BeginInvoke(DispatcherPrio priority, Action method) {
			if (disp.HasShutdownFinished || disp.HasShutdownStarted || disp.CheckAccess())
				method();
			else
				disp.BeginInvoke(Convert(priority), method);
		}

		public bool CheckAccess() {
			return disp.CheckAccess();
		}

		public void VerifyAccess() {
			disp.VerifyAccess();
		}
	}
}
