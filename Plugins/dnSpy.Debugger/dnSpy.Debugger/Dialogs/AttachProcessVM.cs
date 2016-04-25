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
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Debugger.Properties;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Dialogs {
	sealed class AttachProcessVM : ViewModelBase, IDisposable {
		public ICommand RefreshCommand {
			get { return new RelayCommand(a => Refresh(), a => CanRefresh); }
		}

		public string Title {
			get {
				if (!Environment.Is64BitOperatingSystem)
					return dnSpy_Debugger_Resources.Attach_AttachToProcess;
				return IntPtr.Size == 4 ? dnSpy_Debugger_Resources.Attach_AttachToProcess32 :
						dnSpy_Debugger_Resources.Attach_AttachToProcess64;
			}
		}

		public bool HasDebuggingText {
			get { return Environment.Is64BitOperatingSystem; }
		}

		public string DebuggingText {
			get {
				return IntPtr.Size == 4 ? dnSpy_Debugger_Resources.Attach_UseDnSpy32 :
						dnSpy_Debugger_Resources.Attach_UseDnSpy64;
			}
		}

		public ObservableCollection<ProcessVM> Collection {
			get { return processList; }
		}
		readonly ObservableCollection<ProcessVM> processList;

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged("SelectedItem");
					HasErrorUpdated();
				}
			}
		}
		object selectedItem;

		public ProcessVM SelectedProcess {
			get { return selectedItem as ProcessVM; }
		}

		readonly Dispatcher dispatcher;
		readonly ProcessContext processContext;

		public AttachProcessVM(Dispatcher dispatcher, bool syntaxHighlight) {
			this.dispatcher = dispatcher;
			this.processContext = new ProcessContext {
				SyntaxHighlight = syntaxHighlight,
			};
			this.processList = new ObservableCollection<ProcessVM>();
			Refresh();
		}

		public bool IsRefreshing {
			get { return refreshThread != null; }
		}

		bool CanRefresh {
			get { return !IsRefreshing; }
		}

		void Refresh() {
			if (IsRefreshing)
				return;

			Collection.Clear();
			cancellationToken = new CancellationTokenSource();
			refreshThread = new Thread(RefreshAsync);
			OnPropertyChanged("IsRefreshing");
			refreshThread.Start();
		}
		Thread refreshThread;
		CancellationTokenSource cancellationToken;

		void CancelRefresh() {
			if (refreshThread == null)
				return;

			cancellationToken.Cancel();
		}

		void ExecInOriginalThread(Action action) {
			if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
				return;
			dispatcher.BeginInvoke(DispatcherPriority.Background, action);
		}

		void AddInfo(ManagedProcessesFinder.Info info) {
			lock (infoListLock) {
				infoList.Add(info);
				if (infoList.Count == 1)
					ExecInOriginalThread(() => AddDiscoveredProcesses());
			}
		}
		readonly object infoListLock = new object();
		readonly List<ManagedProcessesFinder.Info> infoList = new List<ManagedProcessesFinder.Info>();

		void AddDiscoveredProcesses() {
			List<ManagedProcessesFinder.Info> list;
			lock (infoListLock) {
				list = new List<ManagedProcessesFinder.Info>(infoList);
				infoList.Clear();
			}

			foreach (var info in list)
				Collection.Add(new ProcessVM(info.ProcessId, info.Title, info.Machine, info.Type, info.FullPath, processContext));
		}

		void RefreshAsync() {
			try {
				var finder = new ManagedProcessesFinder();
				foreach (var info in finder.FindAll(cancellationToken.Token))
					AddInfo(info);
			}
			catch (OperationCanceledException) {
			}
			catch {
				//TODO: Show error to user
			}
			ExecInOriginalThread(() => {
				refreshThread = null;
				OnPropertyChanged("IsRefreshing");
			});
		}

		public void Dispose() {
			CancelRefresh();
		}

		public override bool HasError {
			get { return SelectedProcess == null; }
		}
	}
}
