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

namespace dnSpy.Debugger.Breakpoints {
	[Export(typeof(IToolWindowContentProvider))]
	sealed class BreakpointsToolWindowContentProvider : IToolWindowContentProvider {
		readonly Lazy<IBreakpointsContent> breakpointsContent;

		public BreakpointsToolWindowContent BreakpointsToolWindowContent => breakpointsToolWindowContent ?? (breakpointsToolWindowContent = new BreakpointsToolWindowContent(breakpointsContent));
		BreakpointsToolWindowContent breakpointsToolWindowContent;

		[ImportingConstructor]
		BreakpointsToolWindowContentProvider(Lazy<IBreakpointsContent> breakpointsContent) {
			this.breakpointsContent = breakpointsContent;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(BreakpointsToolWindowContent.THE_GUID, BreakpointsToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_BREAKPOINTS, false); }
		}

		public ToolWindowContent GetOrCreate(Guid guid) => guid == BreakpointsToolWindowContent.THE_GUID ? BreakpointsToolWindowContent : null;
	}

	sealed class BreakpointsToolWindowContent : ToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("E5745D58-4DCB-4D92-B786-4E1635C86EED");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override IInputElement FocusedElement => breakpointsContent.Value.FocusedElement;
		public override FrameworkElement ZoomElement => breakpointsContent.Value.ZoomElement;
		public override Guid Guid => THE_GUID;
		public override string Title => dnSpy_Debugger_Resources.Window_Breakpoints;
		public override object UIObject => breakpointsContent.Value.UIObject;
		public bool CanFocus => true;

		readonly Lazy<IBreakpointsContent> breakpointsContent;

		public BreakpointsToolWindowContent(Lazy<IBreakpointsContent> breakpointsContent) {
			this.breakpointsContent = breakpointsContent;
		}

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				breakpointsContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				breakpointsContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				breakpointsContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				breakpointsContent.Value.OnHidden();
				break;
			}
		}

		public void Focus() => breakpointsContent.Value.Focus();
	}
}
