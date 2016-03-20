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
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Themes;
using dnSpy.Debugger.CallStack;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Threads {
	interface IThreadsContent {
		object UIObject { get; }
		IInputElement FocusedElement { get; }
		FrameworkElement ScaleElement { get; }
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		ListView ListView { get; }
		IThreadsVM ThreadsVM { get; }
	}

	[Export, Export(typeof(IThreadsContent)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ThreadsContent : IThreadsContent {
		public object UIObject {
			get { return threadsControl; }
		}

		public IInputElement FocusedElement {
			get { return threadsControl.ListView; }
		}

		public FrameworkElement ScaleElement {
			get { return threadsControl; }
		}

		public ListView ListView {
			get { return threadsControl.ListView; }
		}

		public IThreadsVM ThreadsVM {
			get { return vmThreads; }
		}

		readonly ThreadsControl threadsControl;
		readonly IThreadsVM vmThreads;
		readonly Lazy<IStackFrameManager> stackFrameManager;
		readonly IFileTabManager fileTabManager;
		readonly Lazy<IModuleLoader> moduleLoader;

		[ImportingConstructor]
		ThreadsContent(IWpfCommandManager wpfCommandManager, IThreadsVM threadsVM, IThemeManager themeManager, Lazy<IStackFrameManager> stackFrameManager, IFileTabManager fileTabManager, Lazy<IModuleLoader> moduleLoader) {
			this.stackFrameManager = stackFrameManager;
			this.fileTabManager = fileTabManager;
			this.moduleLoader = moduleLoader;
			this.threadsControl = new ThreadsControl();
			this.vmThreads = threadsVM;
			this.threadsControl.DataContext = this.vmThreads;
			this.threadsControl.ThreadsListViewDoubleClick += ThreadsControl_ThreadsListViewDoubleClick;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;

			wpfCommandManager.Add(CommandConstants.GUID_DEBUGGER_THREADS_CONTROL, threadsControl);
			wpfCommandManager.Add(CommandConstants.GUID_DEBUGGER_THREADS_LISTVIEW, threadsControl.ListView);
		}

		void ThreadsControl_ThreadsListViewDoubleClick(object sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			SwitchToThreadThreadsCtxMenuCommand.GoTo(fileTabManager, moduleLoader.Value, stackFrameManager.Value, threadsControl.ListView.SelectedItem as ThreadVM, newTab);
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			vmThreads.RefreshThemeFields();
		}

		public void Focus() {
			UIUtils.FocusSelector(threadsControl.ListView);
		}

		public void OnClose() {
			vmThreads.IsEnabled = false;
		}

		public void OnShow() {
			vmThreads.IsEnabled = true;
		}

		public void OnHidden() {
			vmThreads.IsVisible = false;
		}

		public void OnVisible() {
			vmThreads.IsVisible = true;
		}
	}
}
