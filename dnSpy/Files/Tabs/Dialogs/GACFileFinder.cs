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
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using dnSpy.Shared.Files;

namespace dnSpy.Files.Tabs.Dialogs {
	interface IGACFileReceiver {
		void AddFiles(IEnumerable<GacFileInfo> files);
	}

	sealed class GACFileFinder {
		readonly IGACFileReceiver target;
		readonly Dispatcher dispatcher;
		readonly CancellationToken cancellationToken;

		public GACFileFinder(IGACFileReceiver target, Dispatcher dispatcher, CancellationToken cancellationToken) {
			this.target = target;
			this.dispatcher = dispatcher;
			this.cancellationToken = cancellationToken;
		}

		void ExecuteInThread(Action action) {
			dispatcher.BeginInvoke(DispatcherPriority.Background, action);
		}

		public void Find() {
			foreach (var info in GacInfo.GetAssemblies(4)) {
				cancellationToken.ThrowIfCancellationRequested();
				Add(info);
			}
			foreach (var info in GacInfo.GetAssemblies(2)) {
				cancellationToken.ThrowIfCancellationRequested();
				Add(info);
			}
		}

		void Add(GacFileInfo info) {
			bool start;
			lock (lockObj) {
				infos.Add(info);
				start = infos.Count == 1;
			}
			if (start)
				ExecuteInThread(Dequeue);
		}
		readonly List<GacFileInfo> infos = new List<GacFileInfo>();
		readonly object lockObj = new object();

		void Dequeue() {
			List<GacFileInfo> tmp;
			lock (lockObj) {
				tmp = new List<GacFileInfo>(infos);
				infos.Clear();
			}
			target.AddFiles(tmp);
		}
	}
}
