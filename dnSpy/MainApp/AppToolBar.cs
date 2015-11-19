/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.ToolBars;
using dnSpy.Controls;

namespace dnSpy.MainApp {
	[Export, PartCreationPolicy(CreationPolicy.Shared)]
	sealed class AppToolBar : IStackedContentChild {
		IStackedContent IStackedContentChild.StackedContent { get; set; }

		public object UIObject {
			get { return toolBar; }
		}
		readonly ToolBar toolBar;

		readonly IToolBarManager toolBarManager;

		[ImportingConstructor]
		public AppToolBar(IToolBarManager toolBarManager) {
			this.toolBarManager = toolBarManager;
			this.toolBar = new ToolBar { FocusVisualStyle = null };
		}

		internal void Initialize(Window window) {
			toolBarManager.InitializeToolBar(toolBar, new Guid(ToolBarConstants.APP_TB_GUID), window);
		}
	}
}
