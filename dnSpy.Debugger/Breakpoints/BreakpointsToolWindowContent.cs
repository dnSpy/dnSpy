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
	[Export(typeof(IMainToolWindowContentCreator))]
	sealed class BreakpointsToolWindowContentCreator : IMainToolWindowContentCreator {
		readonly Lazy<IBreakpointsContent> breakpointsContent;

		public BreakpointsToolWindowContent BreakpointsToolWindowContent {
			get { return breakpointsToolWindowContent ?? (breakpointsToolWindowContent = new BreakpointsToolWindowContent(breakpointsContent)); }
		}
		BreakpointsToolWindowContent breakpointsToolWindowContent;

		[ImportingConstructor]
		BreakpointsToolWindowContentCreator(Lazy<IBreakpointsContent> breakpointsContent) {
			this.breakpointsContent = breakpointsContent;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(BreakpointsToolWindowContent.THE_GUID, BreakpointsToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_BREAKPOINTS, false); }
		}

		public IToolWindowContent GetOrCreate(Guid guid) {
			if (guid == BreakpointsToolWindowContent.THE_GUID)
				return BreakpointsToolWindowContent;
			return null;
		}
	}

	sealed class BreakpointsToolWindowContent : IToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("E5745D58-4DCB-4D92-B786-4E1635C86EED");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.Default;

		public IInputElement FocusedElement {
			get { return breakpointsContent.Value.FocusedElement; }
		}

		public FrameworkElement ScaleElement {
			get { return breakpointsContent.Value.ScaleElement; }
		}

		public Guid Guid {
			get { return THE_GUID; }
		}

		public string Title {
			get { return dnSpy_Debugger_Resources.Window_Breakpoints; }
		}

		public object ToolTip {
			get { return null; }
		}

		public object UIObject {
			get { return breakpointsContent.Value.UIObject; }
		}

		public bool CanFocus {
			get { return true; }
		}

		readonly Lazy<IBreakpointsContent> breakpointsContent;

		public BreakpointsToolWindowContent(Lazy<IBreakpointsContent> breakpointsContent) {
			this.breakpointsContent = breakpointsContent;
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				breakpointsContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				breakpointsContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
			case ToolWindowContentVisibilityEvent.Hidden:
				break;
			}
		}

		public void Focus() {
			breakpointsContent.Value.Focus();
		}
	}
}
