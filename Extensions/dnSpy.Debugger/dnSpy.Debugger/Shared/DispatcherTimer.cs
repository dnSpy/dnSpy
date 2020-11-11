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
using System.Threading;

namespace dnSpy.Debugger.Shared {
	sealed class DispatcherTimer {
		readonly object lockObj;
		readonly Dispatcher dispatcher;
		readonly TimeSpan interval;
		Timer? timer;

		public event EventHandler? Tick;

		public DispatcherTimer(Dispatcher dispatcher, TimeSpan interval) {
			if (interval < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(interval));
			if (interval > TimeSpan.FromMilliseconds(int.MaxValue))
				throw new ArgumentOutOfRangeException(nameof(interval));
			lockObj = new object();
			this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
			this.interval = interval;
		}

		void OnTimerCallback(object? state) {
			lock (lockObj) {
				if (timer is null)
					return;
			}
			if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) {
				Stop();
				return;
			}
			dispatcher.BeginInvoke(() => {
				lock (lockObj) {
					if (timer is null)
						return;
				}
				Tick?.Invoke(this, EventArgs.Empty);
			});
		}

		public void Start() {
			lock (lockObj) {
				if (timer is not null)
					return;
				timer = new Timer(OnTimerCallback, null, interval, interval);
			}
		}

		public void Stop() {
			Timer? oldTimer;
			lock (lockObj) {
				oldTimer = timer;
				timer = null;
			}
			oldTimer?.Dispose();
		}
	}
}
