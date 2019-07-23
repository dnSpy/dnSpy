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
using System.Windows.Threading;

namespace dnSpy.Contracts.Utilities {
	/// <summary>
	/// Executes code after a slight delay
	/// </summary>
	sealed class DelayedAction {
		readonly int milliseconds;
		readonly Action action;
		readonly DispatcherTimer dispatcherTimer;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="milliseconds">Milliseconds to wait</param>
		/// <param name="action">Code to execute</param>
		public DelayedAction(int milliseconds, Action action) {
			this.milliseconds = milliseconds;
			this.action = action ?? throw new ArgumentNullException(nameof(action));
			dispatcherTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(milliseconds), DispatcherPriority.Send, StartIt, Dispatcher.CurrentDispatcher);
		}

		void StartIt(object? sender, EventArgs e) {
			dispatcherTimer.Stop();
			action();
		}

		/// <summary>
		/// Cancels the timer
		/// </summary>
		public void Cancel() => dispatcherTimer.Stop();

		/// <summary>
		/// Starts the timer
		/// </summary>
		public void Start() {
			dispatcherTimer.Stop();
			dispatcherTimer.Start();
		}
	}
}
