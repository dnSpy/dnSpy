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

namespace dnSpy.Debugger.ToolWindows.ModuleBreakpoints {
	[Export(typeof(IToolWindowContentProvider))]
	sealed class ModuleBreakpointsToolWindowContentProvider : IToolWindowContentProvider {
		readonly Lazy<IModuleBreakpointsContent> moduleBreakpointsContent;

		public ModuleBreakpointsToolWindowContent ModuleBreakpointsToolWindowContent => moduleBreakpointsToolWindowContent ??= new ModuleBreakpointsToolWindowContent(moduleBreakpointsContent);
		ModuleBreakpointsToolWindowContent? moduleBreakpointsToolWindowContent;

		[ImportingConstructor]
		ModuleBreakpointsToolWindowContentProvider(Lazy<IModuleBreakpointsContent> moduleBreakpointsContent) => this.moduleBreakpointsContent = moduleBreakpointsContent;

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(ModuleBreakpointsToolWindowContent.THE_GUID, ModuleBreakpointsToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_MODULEBREAKPOINTS, false); }
		}

		public ToolWindowContent? GetOrCreate(Guid guid) => guid == ModuleBreakpointsToolWindowContent.THE_GUID ? ModuleBreakpointsToolWindowContent : null;
	}

	sealed class ModuleBreakpointsToolWindowContent : ToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("9D7D28F0-F031-4439-99BF-F7B747FA4B19");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override IInputElement? FocusedElement => moduleBreakpointsContent.Value.FocusedElement;
		public override FrameworkElement? ZoomElement => moduleBreakpointsContent.Value.ZoomElement;
		public override Guid Guid => THE_GUID;
		public override string Title => dnSpy_Debugger_Resources.Window_ModuleBreakpoints;
		public override object? UIObject => moduleBreakpointsContent.Value.UIObject;
		public bool CanFocus => true;

		readonly Lazy<IModuleBreakpointsContent> moduleBreakpointsContent;

		public ModuleBreakpointsToolWindowContent(Lazy<IModuleBreakpointsContent> moduleBreakpointsContent) => this.moduleBreakpointsContent = moduleBreakpointsContent;

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				moduleBreakpointsContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				moduleBreakpointsContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				moduleBreakpointsContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				moduleBreakpointsContent.Value.OnHidden();
				break;
			}
		}

		public void Focus() => moduleBreakpointsContent.Value.Focus();
	}
}
