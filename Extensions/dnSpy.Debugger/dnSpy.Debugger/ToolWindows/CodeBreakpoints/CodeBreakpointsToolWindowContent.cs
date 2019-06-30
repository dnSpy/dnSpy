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

namespace dnSpy.Debugger.ToolWindows.CodeBreakpoints {
	[Export(typeof(IToolWindowContentProvider))]
	sealed class CodeBreakpointsToolWindowContentProvider : IToolWindowContentProvider {
		readonly Lazy<ICodeBreakpointsContent> codeBreakpointsContent;

		public CodeBreakpointsToolWindowContent CodeBreakpointsToolWindowContent => codeBreakpointsToolWindowContent ??= new CodeBreakpointsToolWindowContent(codeBreakpointsContent);
		CodeBreakpointsToolWindowContent? codeBreakpointsToolWindowContent;

		[ImportingConstructor]
		CodeBreakpointsToolWindowContentProvider(Lazy<ICodeBreakpointsContent> codeBreakpointsContent) => this.codeBreakpointsContent = codeBreakpointsContent;

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(CodeBreakpointsToolWindowContent.THE_GUID, CodeBreakpointsToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_CODEBREAKPOINTS, false); }
		}

		public ToolWindowContent? GetOrCreate(Guid guid) => guid == CodeBreakpointsToolWindowContent.THE_GUID ? CodeBreakpointsToolWindowContent : null;
	}

	sealed class CodeBreakpointsToolWindowContent : ToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("E5745D58-4DCB-4D92-B786-4E1635C86EED");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override IInputElement? FocusedElement => codeBreakpointsContent.Value.FocusedElement;
		public override FrameworkElement? ZoomElement => codeBreakpointsContent.Value.ZoomElement;
		public override Guid Guid => THE_GUID;
		public override string Title => dnSpy_Debugger_Resources.Window_Breakpoints;
		public override object? UIObject => codeBreakpointsContent.Value.UIObject;
		public bool CanFocus => true;

		readonly Lazy<ICodeBreakpointsContent> codeBreakpointsContent;

		public CodeBreakpointsToolWindowContent(Lazy<ICodeBreakpointsContent> codeBreakpointsContent) => this.codeBreakpointsContent = codeBreakpointsContent;

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				codeBreakpointsContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				codeBreakpointsContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				codeBreakpointsContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				codeBreakpointsContent.Value.OnHidden();
				break;
			}
		}

		public void Focus() => codeBreakpointsContent.Value.Focus();
	}
}
