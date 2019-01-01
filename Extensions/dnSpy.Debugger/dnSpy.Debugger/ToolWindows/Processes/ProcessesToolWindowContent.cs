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

namespace dnSpy.Debugger.ToolWindows.Processes {
	[Export(typeof(IToolWindowContentProvider))]
	sealed class ProcessesToolWindowContentProvider : IToolWindowContentProvider {
		readonly Lazy<IProcessesContent> processesContent;

		public ProcessesToolWindowContent ProcessesToolWindowContent => processesToolWindowContent ?? (processesToolWindowContent = new ProcessesToolWindowContent(processesContent));
		ProcessesToolWindowContent processesToolWindowContent;

		[ImportingConstructor]
		ProcessesToolWindowContentProvider(Lazy<IProcessesContent> processesContent) => this.processesContent = processesContent;

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(ProcessesToolWindowContent.THE_GUID, ProcessesToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_PROCESSES, false); }
		}

		public ToolWindowContent GetOrCreate(Guid guid) => guid == ProcessesToolWindowContent.THE_GUID ? ProcessesToolWindowContent : null;
	}

	sealed class ProcessesToolWindowContent : ToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("F1EFB8BE-8941-4BE4-ACC4-ACA8809394BB");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override IInputElement FocusedElement => processesContent.Value.FocusedElement;
		public override FrameworkElement ZoomElement => processesContent.Value.ZoomElement;
		public override Guid Guid => THE_GUID;
		public override string Title => dnSpy_Debugger_Resources.Window_Processes;
		public override object UIObject => processesContent.Value.UIObject;
		public bool CanFocus => true;

		readonly Lazy<IProcessesContent> processesContent;

		public ProcessesToolWindowContent(Lazy<IProcessesContent> processesContent) => this.processesContent = processesContent;
		public void Focus() => processesContent.Value.Focus();

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				processesContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				processesContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				processesContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				processesContent.Value.OnHidden();
				break;
			}
		}
	}
}
