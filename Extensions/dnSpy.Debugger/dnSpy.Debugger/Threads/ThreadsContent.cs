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
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Themes;
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

	[Export(typeof(IThreadsContent))]
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
		double zoomLevel;

		[ImportingConstructor]
		ThreadsContent(IWpfCommandService wpfCommandService, IThreadsVM threadsVM, IThemeService themeService, IDpiService dpiService, Lazy<IStackFrameService> stackFrameService, IDocumentTabService documentTabService, Lazy<IModuleLoader> moduleLoader, IModuleIdProvider moduleIdProvider) {
			this.stackFrameService = stackFrameService;
			this.documentTabService = documentTabService;
			this.moduleLoader = moduleLoader;
			this.threadsControl = new ThreadsControl();
			this.vmThreads = threadsVM;
			this.moduleIdProvider = moduleIdProvider;
			this.threadsControl.DataContext = this.vmThreads;
			this.threadsControl.ThreadsListViewDoubleClick += ThreadsControl_ThreadsListViewDoubleClick;
			themeService.ThemeChanged += ThemeService_ThemeChanged;
			dpiService.DpiChanged += DpiService_DpiChanged;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_THREADS_CONTROL, threadsControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_THREADS_LISTVIEW, threadsControl.ListView);
			UpdateImageOptions();
		}

		void UpdateImageOptions() {
			var options = new ImageOptions {
				Zoom = new Size(zoomLevel, zoomLevel),
				DpiObject = threadsControl,
			};
			vmThreads.SetImageOptions(options);
		}

		void ThreadsControl_ThreadsListViewDoubleClick(object sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			SwitchToThreadThreadsCtxMenuCommand.GoTo(moduleIdProvider, documentTabService, moduleLoader.Value, stackFrameService.Value, threadsControl.ListView.SelectedItem as ThreadVM, newTab);
		}

		void DpiService_DpiChanged(object sender, WindowDpiChangedEventArgs e) {
			if (e.Window == Window.GetWindow(threadsControl))
				vmThreads.RefreshThemeFields();
		}

		void ThemeService_ThemeChanged(object sender, ThemeChangedEventArgs e) => vmThreads.RefreshThemeFields();
		public void Focus() => UIUtilities.FocusSelector(threadsControl.ListView);
		public void OnClose() => vmThreads.IsEnabled = false;
		public void OnShow() => vmThreads.IsEnabled = true;
		public void OnHidden() => vmThreads.IsVisible = false;
		public void OnVisible() => vmThreads.IsVisible = true;

		public void OnZoomChanged(double value) {
			if (zoomLevel == value)
				return;
			zoomLevel = value;
			UpdateImageOptions();
		}
	}
}
