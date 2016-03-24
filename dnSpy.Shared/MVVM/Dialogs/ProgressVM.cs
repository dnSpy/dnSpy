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
using System.Windows.Input;
using System.Windows.Threading;

namespace dnSpy.Shared.MVVM.Dialogs {
	public interface IProgress {
		void SetTotalProgress(double progress);
		void SetDescription(string desc);
		void ThrowIfCancellationRequested();
		CancellationToken Token { get; }
	}

	public interface IProgressTask {
		bool IsIndeterminate { get; }
		double ProgressMaximum { get; }
		double ProgressMinimum { get; }
		void Execute(IProgress progress);
	}

	public sealed class ProgressVM : ViewModelBase, IProgress {
		public ICommand CancelCommand {
			get { return new RelayCommand(a => Cancel(), a => CanCancel); }
		}

		readonly Dispatcher dispatcher;
		readonly IProgressTask task;
		CancellationTokenSource cancellationTokenSource;

		public ProgressVM(Dispatcher dispatcher, IProgressTask task) {
			this.dispatcher = dispatcher;
			this.task = task;
			this.progressMinimum = task.ProgressMinimum;
			this.progressMaximum = task.ProgressMaximum;
			this.isIndeterminate = task.IsIndeterminate;
			this.cancellationTokenSource = new CancellationTokenSource();
			Start();
		}

		public event EventHandler OnCompleted;

		public bool CanCancel {
			get { return !cancelling; }
		}
		bool cancelling;

		public void Cancel() {
			cancelling = true;
			if (cancellationTokenSource != null)
				cancellationTokenSource.Cancel();
		}

		public bool IsIndeterminate {
			get { return isIndeterminate; }
		}
		readonly bool isIndeterminate;

		public double ProgressMinimum {
			get { return progressMinimum; }
		}
		readonly double progressMinimum;

		public double ProgressMaximum {
			get { return progressMaximum; }
		}
		readonly double progressMaximum;

		public double TotalProgress {
			get { return totalProgress; }
			set {
				if (totalProgress != value) {
					totalProgress = value;
					OnPropertyChanged("TotalProgress");
				}
			}
		}
		double totalProgress;

		public bool WasCanceled {
			get { return wasCanceled; }
			set {
				if (wasCanceled != value) {
					wasCanceled = value;
					OnPropertyChanged("WasCanceled");
				}
			}
		}
		bool wasCanceled;

		public bool HasCompleted {
			get { return hasCompleted; }
			set {
				if (hasCompleted != value) {
					hasCompleted = value;
					OnPropertyChanged("HasCompleted");
				}
			}
		}
		bool hasCompleted;

		public string CurrentItemDescription {
			get { return currentItemDescription; }
			set {
				if (currentItemDescription != value) {
					currentItemDescription = value;
					OnPropertyChanged("CurrentItemDescription");
				}
			}
		}
		string currentItemDescription;

		public bool WasError {
			get { return errorMessage != null; }
		}

		public string ErrorMessage {
			get { return errorMessage; }
		}
		string errorMessage;

		class MyAction {
			public readonly Action Action;

			public MyAction(Action action) {
				this.Action = action;
			}
		}

		sealed class UpdateProgressAction : MyAction {
			public UpdateProgressAction(Action action)
				: base(action) {
			}
		}

		sealed class SetDescriptionAction : MyAction {
			public SetDescriptionAction(Action action)
				: base(action) {
			}
		}

		void QueueAction(MyAction action, bool unique) {
			bool start;
			lock (lockObj) {
				if (unique) {
					for (int i = 0; i < actions.Count; i++) {
						if (actions[i].GetType() == action.GetType()) {
							actions.RemoveAt(i);
							break;
						}
					}
				}
				actions.Add(action);
				start = actions.Count == 1;
			}
			if (start)
				dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(EmptyQueue));
		}
		object lockObj = new object();
		List<MyAction> actions = new List<MyAction>();

		void EmptyQueue() {
			MyAction[] ary;
			lock (lockObj) {
				ary = actions.ToArray();
				actions.Clear();
			}
			foreach (var a in ary)
				a.Action();
		}

		void IProgress.SetTotalProgress(double progress) {
			QueueAction(new UpdateProgressAction(() => TotalProgress = progress), true);
		}

		void IProgress.SetDescription(string desc) {
			QueueAction(new SetDescriptionAction(() => CurrentItemDescription = desc), true);
		}

		public CancellationToken Token {
			get { return cancellationTokenSource.Token; }
		}

		void IProgress.ThrowIfCancellationRequested() {
			this.cancellationTokenSource.Token.ThrowIfCancellationRequested();
		}

		void OnTaskCompleted() {
			cancellationTokenSource.Dispose();
			cancellationTokenSource = null;
			HasCompleted = true;
			if (OnCompleted != null)
				OnCompleted(this, EventArgs.Empty);
		}

		void Start() {
			new Thread(ThreadProc).Start();
		}

		void ThreadProc() {
			try {
				task.Execute(this);
			}
			catch (OperationCanceledException) {
				QueueAction(new MyAction(() => WasCanceled = true), false);
			}
			catch (Exception ex) {
				errorMessage = ex.Message;
			}
			QueueAction(new MyAction(OnTaskCompleted), false);
		}
	}
}
