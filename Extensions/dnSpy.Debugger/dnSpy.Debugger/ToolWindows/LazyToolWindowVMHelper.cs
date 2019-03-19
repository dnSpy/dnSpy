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
using System.Diagnostics;
using System.Windows.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows {
	interface ILazyToolWindowVM {
		/// <summary>
		/// Called to initialize everything so the window can be shown.
		/// </summary>
		void Show();

		/// <summary>
		/// Does the reverse of <see cref="Show"/>
		/// </summary>
		void Hide();
	}

	class LazyToolWindowVMHelper {
		static readonly TimeSpan hideWindowTimeout = TimeSpan.FromMinutes(5);

		// true if the tool window is open but not necessarily visible (could be hidden by
		// another tool window)
		public bool IsOpen {
			get => isEnabled;
			set {
				if (isEnabled == value)
					return;
				isEnabled = value;
				OnIsEnabledChanged();
				if (!isEnabled) {
					Debug.Assert(!IsVisible);
					TryHideWindow();
				}
			}
		}
		bool isEnabled;

		// Tool window's visibility
		public bool IsVisible {
			get => isVisible;
			set {
				if (isVisible == value)
					return;
				isVisible = value;
				if (isVisible) {
					if (!IsWindowShown)
						Show();
					StopTimer();
				}
				else
					StartTimer();
				OnIsVisibleChanged();
			}
		}
		bool isVisible;

		bool IsWindowShown { get; set; }

		readonly ILazyToolWindowVM vm;
		readonly UIDispatcher uiDispatcher;
		DispatcherTimer timer;

		public LazyToolWindowVMHelper(ILazyToolWindowVM vm, UIDispatcher uiDispatcher) {
			this.vm = vm ?? throw new ArgumentNullException(nameof(vm));
			this.uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
		}

		// random thread
		protected void UI(Action callback) => uiDispatcher.UI(callback);

		void StartTimer() {
			StopTimer();
			Debug.Assert(!IsVisible);
			if (IsVisible)
				return;
			timer = new DispatcherTimer(DispatcherPriority.Send, uiDispatcher.Dispatcher);
			timer.Interval = hideWindowTimeout;
			timer.Tick += Timer_Tick_UI;
			timer.Start();
		}

		void StopTimer() {
			if (timer != null) {
				timer.Tick -= Timer_Tick_UI;
				timer.Stop();
				timer = null;
			}
		}

		void Timer_Tick_UI(object sender, EventArgs e) {
			if (timer != sender)
				return;
			StopTimer();
			if (!IsVisible)
				TryHideWindow();
		}

		protected virtual void OnIsEnabledChanged() { }
		protected virtual void OnIsVisibleChanged() { }

		public void TryHideWindow() {
			// Can't reset it if the window's visible
			if (IsVisible)
				return;
			Hide();
		}

		protected virtual void OnShow() { }
		void Show() {
			if (IsWindowShown)
				return;
			IsWindowShown = true;
			StopTimer();
			OnShow();
			vm.Show();
		}

		protected virtual void OnHide() { }
		void Hide() {
			if (!IsWindowShown)
				return;
			IsWindowShown = false;
			StopTimer();
			OnHide();
			vm.Hide();
		}
	}

	sealed class DebuggerLazyToolWindowVMHelper : LazyToolWindowVMHelper {
		readonly Lazy<DbgManager> dbgManager;

		public DebuggerLazyToolWindowVMHelper(ILazyToolWindowVM vm, UIDispatcher uiDispatcher, Lazy<DbgManager> dbgManager)
			: base(vm, uiDispatcher) => this.dbgManager = dbgManager;

		protected override void OnShow() => dbgManager.Value.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
		protected override void OnHide() => dbgManager.Value.IsDebuggingChanged -= DbgManager_IsDebuggingChanged;
		protected override void OnIsVisibleChanged() => CheckCanLazyInitialize();

		void DbgManager_IsDebuggingChanged(object sender, EventArgs e) {
			if (!dbgManager.Value.IsDebugging) {
				UI(() => {
					// We've already checked IsDebugging above. It's possible that it's now
					// true again, but we should still try to hide the window. This happens
					// if Restart button is pressed.
					if (!IsVisible)
						TryHideWindow();
				});
			}
		}

		void CheckCanLazyInitialize() {
			// If the window isn't visible and debugging stopped, we can go back to
			// lazy initialization next time debugging starts.
			if (!IsVisible && !dbgManager.Value.IsDebugging)
				TryHideWindow();
		}
	}
}
