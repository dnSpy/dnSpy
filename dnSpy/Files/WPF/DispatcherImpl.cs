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

		DispatcherPriority Convert(DispatcherPrio priority) {
			switch (priority) {
			case DispatcherPrio.ContextIdle: return DispatcherPriority.ContextIdle;
			case DispatcherPrio.Background: return DispatcherPriority.Background;
			case DispatcherPrio.Loaded: return DispatcherPriority.Loaded;
			case DispatcherPrio.Send: return DispatcherPriority.Send;
			default:
				Debug.Fail("Unknown prio");
				return DispatcherPriority.Background;
			}
		}

		public void BeginInvoke(DispatcherPrio priority, Action method) {
			// Don't call method() here if disp.CheckAccess() is true. That could lead to some
			// problems, eg. a file gets removed, the BP list gets saved and a file gets added,
			// causing an exception.
			if (disp.HasShutdownFinished || disp.HasShutdownStarted)
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
