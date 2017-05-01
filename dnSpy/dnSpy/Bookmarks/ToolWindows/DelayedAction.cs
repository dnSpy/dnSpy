/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Timers;
using dnSpy.UI;

namespace dnSpy.Bookmarks.ToolWindows {
	sealed class DelayedAction {
		readonly Action action;
		readonly UIDispatcher uiDispatcher;
		readonly Timer timer;
		bool cancel;

		public DelayedAction(UIDispatcher uiDispatcher, int milliseconds, Action action) {
			this.uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
			this.action = action ?? throw new ArgumentNullException(nameof(action));
			cancel = true;
			timer = new Timer(milliseconds);
			timer.Elapsed += Timer_Elapsed;
		}

		void Timer_Elapsed(object sender, ElapsedEventArgs e) =>
			uiDispatcher.UI(() => ExecuteAction());

		void ExecuteAction() {
			uiDispatcher.VerifyAccess();
			timer.Stop();
			if (!cancel)
				action();
		}

		public void Cancel() {
			uiDispatcher.VerifyAccess();
			cancel = true;
			timer.Stop();
		}

		public void Start() {
			uiDispatcher.VerifyAccess();
			cancel = false;
			timer.Stop();
			timer.Start();
		}
	}
}
