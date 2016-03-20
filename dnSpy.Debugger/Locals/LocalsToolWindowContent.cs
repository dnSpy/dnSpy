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

namespace dnSpy.Debugger.Locals {
	[Export(typeof(IMainToolWindowContentCreator))]
	sealed class LocalsToolWindowContentCreator : IMainToolWindowContentCreator {
		readonly Lazy<ILocalsContent> localsContent;

		public LocalsToolWindowContent LocalsToolWindowContent {
			get { return localsToolWindowContent ?? (localsToolWindowContent = new LocalsToolWindowContent(localsContent)); }
		}
		LocalsToolWindowContent localsToolWindowContent;

		[ImportingConstructor]
		LocalsToolWindowContentCreator(Lazy<ILocalsContent> localsContent) {
			this.localsContent = localsContent;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(LocalsToolWindowContent.THE_GUID, LocalsToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_LOCALS, false); }
		}

		public IToolWindowContent GetOrCreate(Guid guid) {
			if (guid == LocalsToolWindowContent.THE_GUID)
				return LocalsToolWindowContent;
			return null;
		}
	}

	sealed class LocalsToolWindowContent : IToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("D799829F-CAE3-4F8F-AD81-1732ABC50636");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.Default;

		public IInputElement FocusedElement {
			get { return localsContent.Value.FocusedElement; }
		}

		public FrameworkElement ScaleElement {
			get { return localsContent.Value.ScaleElement; }
		}

		public Guid Guid {
			get { return THE_GUID; }
		}

		public string Title {
			get { return dnSpy_Debugger_Resources.Window_Locals; }
		}

		public object ToolTip {
			get { return null; }
		}

		public object UIObject {
			get { return localsContent.Value.UIObject; }
		}

		public bool CanFocus {
			get { return true; }
		}

		readonly Lazy<ILocalsContent> localsContent;

		public LocalsToolWindowContent(Lazy<ILocalsContent> localsContent) {
			this.localsContent = localsContent;
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				localsContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				localsContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				localsContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				localsContent.Value.OnHidden();
				break;
			}
		}

		public void Focus() {
			localsContent.Value.Focus();
		}
	}
}
