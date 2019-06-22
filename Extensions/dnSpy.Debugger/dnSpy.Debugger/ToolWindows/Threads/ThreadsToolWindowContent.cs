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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Threads {
	[Export(typeof(IToolWindowContentProvider))]
	sealed class ThreadsToolWindowContentProvider : IToolWindowContentProvider {
		readonly Lazy<IThreadsContent> threadsContent;

		public ThreadsToolWindowContent ThreadsToolWindowContent => threadsToolWindowContent ??= new ThreadsToolWindowContent(threadsContent);
		ThreadsToolWindowContent? threadsToolWindowContent;

		[ImportingConstructor]
		ThreadsToolWindowContentProvider(Lazy<IThreadsContent> threadsContent) => this.threadsContent = threadsContent;

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(ThreadsToolWindowContent.THE_GUID, ThreadsToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_THREADS, false); }
		}

		public ToolWindowContent? GetOrCreate(Guid guid) => guid == ThreadsToolWindowContent.THE_GUID ? ThreadsToolWindowContent : null;
	}

	sealed class ThreadsToolWindowContent : ToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("3C01719C-B6B5-4261-9CD4-3EDCE1032E5C");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override IInputElement? FocusedElement => threadsContent.Value.FocusedElement;
		public override FrameworkElement? ZoomElement => threadsContent.Value.ZoomElement;
		public override Guid Guid => THE_GUID;
		public override string Title => dnSpy_Debugger_Resources.Window_Threads;
		public override object? UIObject => threadsContent.Value.UIObject;
		public bool CanFocus => true;

		readonly Lazy<IThreadsContent> threadsContent;

		public ThreadsToolWindowContent(Lazy<IThreadsContent> threadsContent) => this.threadsContent = threadsContent;

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
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

		public void Focus() => threadsContent.Value.Focus();
	}
}
