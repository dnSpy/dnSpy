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
using System.ComponentModel;
using System.Windows.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Processes {
	sealed class ProcessVM : ViewModelBase {
		//TODO: init
		internal bool IsSelectedProcess {
			get => isSelectedProcess;
			set {
				Context.Dispatcher.VerifyAccess();
				if (isSelectedProcess != value) {
					isSelectedProcess = value;
					OnPropertyChanged(nameof(ImageReference));
				}
			}
		}
		bool isSelectedProcess;

		public ImageReference ImageReference => IsSelectedProcess ? DsImages.CurrentInstructionPointer : ImageReference.None;
		public object NameObject => new FormatterObject<ProcessVM>(this, PredefinedTextClassifierTags.ProcessesWindowName);
		public object IdObject => new FormatterObject<ProcessVM>(this, PredefinedTextClassifierTags.ProcessesWindowId);
		public object TitleObject => new FormatterObject<ProcessVM>(this, PredefinedTextClassifierTags.ProcessesWindowTitle);
		public object StateObject => new FormatterObject<ProcessVM>(this, PredefinedTextClassifierTags.ProcessesWindowState);
		public object DebuggingObject => new FormatterObject<ProcessVM>(this, PredefinedTextClassifierTags.ProcessesWindowDebugging);
		public object PathObject => new FormatterObject<ProcessVM>(this, PredefinedTextClassifierTags.ProcessesWindowPath);
		public DbgProcess Process { get; }
		public IProcessContext Context { get; }

		internal string Title {
			get {
				if (title == null)
					title = GetProcessTitle() ?? string.Empty;
				return title;
			}
			private set {
				Context.Dispatcher.VerifyAccess();
				var newValue = value ?? string.Empty;
				if (title != newValue) {
					title = newValue;
					OnPropertyChanged(nameof(TitleObject));
				}
			}
		}
		string title;

		internal int Order { get; }

		public ProcessVM(DbgProcess process, IProcessContext context, int order) {
			Process = process ?? throw new ArgumentNullException(nameof(process));
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Order = order;
			process.PropertyChanged += DbgProcess_PropertyChanged;
		}

		string GetProcessTitle() {
			try {
				using (var p = System.Diagnostics.Process.GetProcessById(Process.Id))
					return p.MainWindowTitle;
			}
			catch {
			}
			return null;
		}

		// UI thread
		internal void RefreshTitle_UI() {
			Context.Dispatcher.VerifyAccess();
			Title = GetProcessTitle();
		}

		// UI thread
		internal void RefreshThemeFields_UI() {
			Context.Dispatcher.VerifyAccess();
			OnPropertyChanged(nameof(NameObject));
			OnPropertyChanged(nameof(IdObject));
			OnPropertyChanged(nameof(TitleObject));
			OnPropertyChanged(nameof(StateObject));
			OnPropertyChanged(nameof(DebuggingObject));
			OnPropertyChanged(nameof(PathObject));
		}

		// UI thread
		internal void RefreshHexFields_UI() {
			Context.Dispatcher.VerifyAccess();
			OnPropertyChanged(nameof(IdObject));
		}

		// DbgManager thread
		void DbgProcess_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			Context.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => DbgProcess_PropertyChanged_UI(e.PropertyName)));

		// UI thread
		void DbgProcess_PropertyChanged_UI(string propertyName) {
			Context.Dispatcher.VerifyAccess();
			if (!Context.IsVisible)
				return;
			switch (propertyName) {
			case nameof(Process.Filename):
				OnPropertyChanged(nameof(NameObject));
				OnPropertyChanged(nameof(PathObject));
				break;

			case nameof(Process.Id):
				OnPropertyChanged(nameof(IdObject));
				break;

			case nameof(Process.State):
				OnPropertyChanged(nameof(StateObject));
				break;

			case nameof(Process.Debugging):
				OnPropertyChanged(nameof(DebuggingObject));
				break;
			}
		}

		// UI thread
		internal void Dispose() {
			Context.Dispatcher.VerifyAccess();
			Process.PropertyChanged -= DbgProcess_PropertyChanged;
		}
	}
}
