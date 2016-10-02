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

namespace dnSpy.Contracts.MVVM.Dialogs {
	/// <summary>
	/// Progress
	/// </summary>
	interface IProgress {
		/// <summary>
		/// Sets total progress
		/// </summary>
		/// <param name="progress">Total progress</param>
		void SetTotalProgress(double progress);

		/// <summary>
		/// Sets the description
		/// </summary>
		/// <param name="description">Description</param>
		void SetDescription(string description);

		/// <summary>
		/// Throws if it should be cancelled
		/// </summary>
		void ThrowIfCancellationRequested();

		/// <summary>
		/// Cancellation token
		/// </summary>
		CancellationToken Token { get; }
	}

	/// <summary>
	/// Progress task
	/// </summary>
	interface IProgressTask {
		/// <summary>
		/// true if an indeterminate progress bar should be used
		/// </summary>
		bool IsIndeterminate { get; }

		/// <summary>
		/// Max progress
		/// </summary>
		double ProgressMaximum { get; }

		/// <summary>
		/// Minimum progress
		/// </summary>
		double ProgressMinimum { get; }

		/// <summary>
		/// Executes the code
		/// </summary>
		/// <param name="progress">Progress</param>
		void Execute(IProgress progress);
	}

	/// <summary>
	/// Progress VM
	/// </summary>
	sealed class ProgressVM : ViewModelBase, IProgress {
		/// <summary>
		/// Cancel command
		/// </summary>
		public ICommand CancelCommand => new RelayCommand(a => Cancel(), a => CanCancel);

		readonly Dispatcher dispatcher;
		readonly IProgressTask task;
		CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dispatcher">Dispatcher to use</param>
		/// <param name="task">Task</param>
		public ProgressVM(Dispatcher dispatcher, IProgressTask task) {
			this.dispatcher = dispatcher;
			this.task = task;
			this.ProgressMinimum = task.ProgressMinimum;
			this.ProgressMaximum = task.ProgressMaximum;
			this.IsIndeterminate = task.IsIndeterminate;
			this.cancellationTokenSource = new CancellationTokenSource();
			Token = cancellationTokenSource.Token;
			Start();
		}

		/// <summary>
		/// Raised when it has completed
		/// </summary>
		public event EventHandler OnCompleted;

		/// <summary>
		/// true if it <see cref="Cancel"/> can be called
		/// </summary>
		public bool CanCancel => !cancelling;
		bool cancelling;

		/// <summary>
		/// Cancels the task
		/// </summary>
		public void Cancel() {
			if (cancelling)
				return;
			cancelling = true;
			cancellationTokenSource?.Cancel();
		}

		/// <summary>
		/// true if an indeterminate progress bar should be used
		/// </summary>
		public bool IsIndeterminate { get; }

		/// <summary>
		/// Minimum progress
		/// </summary>
		public double ProgressMinimum { get; }

		/// <summary>
		/// Max progress
		/// </summary>
		public double ProgressMaximum { get; }

		/// <summary>
		/// Gets/sets the total progress
		/// </summary>
		public double TotalProgress {
			get { return totalProgress; }
			set {
				if (totalProgress != value) {
					totalProgress = value;
					OnPropertyChanged(nameof(TotalProgress));
				}
			}
		}
		double totalProgress;

		/// <summary>
		/// Gets/sets whether it was cancelled
		/// </summary>
		public bool WasCanceled {
			get { return wasCanceled; }
			set {
				if (wasCanceled != value) {
					wasCanceled = value;
					OnPropertyChanged(nameof(WasCanceled));
				}
			}
		}
		bool wasCanceled;

		/// <summary>
		/// Gets/sets has-completed
		/// </summary>
		public bool HasCompleted {
			get { return hasCompleted; }
			set {
				if (hasCompleted != value) {
					hasCompleted = value;
					OnPropertyChanged(nameof(HasCompleted));
				}
			}
		}
		bool hasCompleted;

		/// <summary>
		/// Gets/sets current description
		/// </summary>
		public string CurrentItemDescription {
			get { return currentItemDescription; }
			set {
				if (currentItemDescription != value) {
					currentItemDescription = value;
					OnPropertyChanged(nameof(CurrentItemDescription));
				}
			}
		}
		string currentItemDescription;

		/// <summary>
		/// true if there was an error
		/// </summary>
		public bool WasError => ErrorMessage != null;

		/// <summary>
		/// Gets the error message or null if no error
		/// </summary>
		public string ErrorMessage { get; private set; }

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

		void IProgress.SetTotalProgress(double progress) =>
			QueueAction(new UpdateProgressAction(() => TotalProgress = progress), true);

		void IProgress.SetDescription(string desc) =>
			QueueAction(new SetDescriptionAction(() => CurrentItemDescription = desc), true);

		/// <summary>
		/// Gets the cancellation token
		/// </summary>
		public CancellationToken Token { get; }

		void IProgress.ThrowIfCancellationRequested() => Token.ThrowIfCancellationRequested();

		void OnTaskCompleted() {
			cancellationTokenSource.Dispose();
			cancellationTokenSource = null;
			HasCompleted = true;
			OnCompleted?.Invoke(this, EventArgs.Empty);
		}

		void Start() => new Thread(ThreadProc).Start();

		void ThreadProc() {
			try {
				task.Execute(this);
			}
			catch (OperationCanceledException) {
				QueueAction(new MyAction(() => WasCanceled = true), false);
			}
			catch (Exception ex) {
				ErrorMessage = ex.Message;
			}
			QueueAction(new MyAction(OnTaskCompleted), false);
		}
	}
}
