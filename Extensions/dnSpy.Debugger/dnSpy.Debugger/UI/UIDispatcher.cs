/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.Windows.Threading;
using dnSpy.Contracts.App;

namespace dnSpy.Debugger.UI {
	[Export(typeof(UIDispatcher))]
	sealed class UIDispatcher {
		public Dispatcher Dispatcher { get; }

		[ImportingConstructor]
		UIDispatcher(IAppWindow appWindow) => Dispatcher = appWindow.MainWindow.Dispatcher;

		public void VerifyAccess() => Dispatcher.VerifyAccess();
		public bool CheckAccess() => Dispatcher.CheckAccess();

		public void UIBackground(Action callback) =>
			Dispatcher.BeginInvoke(DispatcherPriority.Background, callback);

		public void UI(Action callback) =>
			// Use Send so the windows are updated as fast as possible when adding new items
			Dispatcher.BeginInvoke(DispatcherPriority.Send, callback);

		public void UI(TimeSpan delay, Action callback) {
			var timer = new DispatcherTimer(DispatcherPriority.Send, Dispatcher);
			timer.Interval = delay;
			EventHandler handler = null;
			handler = (s, e) => {
				timer.Stop();
				timer.Tick -= handler;
				callback();
			};
			timer.Tick += handler;
			timer.Start();
		}
	}
}
