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
using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Threads {
	[Export(typeof(IMainToolWindowContentCreator))]
	sealed class ThreadsToolWindowContentCreator : IMainToolWindowContentCreator {
		readonly Lazy<IThreadsContent> threadsContent;

		public ThreadsToolWindowContent ThreadsToolWindowContent {
			get { return threadsToolWindowContent ?? (threadsToolWindowContent = new ThreadsToolWindowContent(threadsContent)); }
		}
		ThreadsToolWindowContent threadsToolWindowContent;

		[ImportingConstructor]
		ThreadsToolWindowContentCreator(Lazy<IThreadsContent> threadsContent) {
			this.threadsContent = threadsContent;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(ThreadsToolWindowContent.THE_GUID, ThreadsToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_THREADS, false); }
		}

		public IToolWindowContent GetOrCreate(Guid guid) {
			if (guid == ThreadsToolWindowContent.THE_GUID)
				return ThreadsToolWindowContent;
			return null;
		}
	}

	sealed class ThreadsToolWindowContent : IToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("3C01719C-B6B5-4261-9CD4-3EDCE1032E5C");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public IInputElement FocusedElement {
			get { return threadsContent.Value.FocusedElement; }
		}

		public FrameworkElement ScaleElement {
			get { return threadsContent.Value.ScaleElement; }
		}

		public Guid Guid {
			get { return THE_GUID; }
		}

		public string Title {
			get { return dnSpy_Debugger_Resources.Window_Threads; }
		}

		public object ToolTip {
			get { return null; }
		}

		public object UIObject {
			get { return threadsContent.Value.UIObject; }
		}

		public bool CanFocus {
			get { return true; }
		}

		readonly Lazy<IThreadsContent> threadsContent;

		public ThreadsToolWindowContent(Lazy<IThreadsContent> threadsContent) {
			this.threadsContent = threadsContent;
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				threadsContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				threadsContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				threadsContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				threadsContent.Value.OnHidden();
				break;
			}
		}

		public void Focus() {
			threadsContent.Value.Focus();
		}
	}
}
