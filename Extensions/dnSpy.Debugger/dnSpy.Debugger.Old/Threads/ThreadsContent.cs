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
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.CallStack;

namespace dnSpy.Debugger.Threads {
	interface IThreadsContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		ListView ListView { get; }
		IThreadsVM ThreadsVM { get; }
	}

	//[Export(typeof(IThreadsContent))]
	sealed class ThreadsContent : IThreadsContent {
		public object UIObject => threadsControl;
		public IInputElement FocusedElement => threadsControl.ListView;
		public FrameworkElement ZoomElement => threadsControl;
		public ListView ListView => threadsControl.ListView;
		public IThreadsVM ThreadsVM => vmThreads;

		readonly ThreadsControl threadsControl;
		readonly IThreadsVM vmThreads;
		readonly Lazy<IStackFrameService> stackFrameService;
		readonly IDocumentTabService documentTabService;
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly IModuleIdProvider moduleIdProvider;

		[ImportingConstructor]
		ThreadsContent(IWpfCommandService wpfCommandService, IThreadsVM threadsVM, Lazy<IStackFrameService> stackFrameService, IDocumentTabService documentTabService, Lazy<IModuleLoader> moduleLoader, IModuleIdProvider moduleIdProvider) {
			this.stackFrameService = stackFrameService;
			this.documentTabService = documentTabService;
			this.moduleLoader = moduleLoader;
			threadsControl = new ThreadsControl();
			vmThreads = threadsVM;
			this.moduleIdProvider = moduleIdProvider;
			threadsControl.DataContext = vmThreads;
			threadsControl.ThreadsListViewDoubleClick += ThreadsControl_ThreadsListViewDoubleClick;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_THREADS_CONTROL, threadsControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_THREADS_LISTVIEW, threadsControl.ListView);
		}

		void ThreadsControl_ThreadsListViewDoubleClick(object sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			SwitchToThreadThreadsCtxMenuCommand.GoTo(moduleIdProvider, documentTabService, moduleLoader.Value, stackFrameService.Value, threadsControl.ListView.SelectedItem as ThreadVM, newTab);
		}

		public void Focus() => UIUtilities.FocusSelector(threadsControl.ListView);
		public void OnClose() => vmThreads.IsEnabled = false;
		public void OnShow() => vmThreads.IsEnabled = true;
		public void OnHidden() => vmThreads.IsVisible = false;
		public void OnVisible() => vmThreads.IsVisible = true;
	}
}
