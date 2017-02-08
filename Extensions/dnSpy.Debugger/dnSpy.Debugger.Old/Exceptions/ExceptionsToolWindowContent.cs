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
using dnSpy.Debugger.Old.Properties;

namespace dnSpy.Debugger.Exceptions {
	[Export(typeof(IToolWindowContentProvider))]
	sealed class ExceptionsToolWindowContentProvider : IToolWindowContentProvider {
		readonly Lazy<IExceptionsContent> exceptionsContent;

		public ExceptionsToolWindowContent ExceptionsToolWindowContent => exceptionsToolWindowContent ?? (exceptionsToolWindowContent = new ExceptionsToolWindowContent(exceptionsContent));
		ExceptionsToolWindowContent exceptionsToolWindowContent;

		[ImportingConstructor]
		ExceptionsToolWindowContentProvider(Lazy<IExceptionsContent> exceptionsContent) {
			this.exceptionsContent = exceptionsContent;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(ExceptionsToolWindowContent.THE_GUID, ExceptionsToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_EXCEPTIONS, false); }
		}

		public ToolWindowContent GetOrCreate(Guid guid) => guid == ExceptionsToolWindowContent.THE_GUID ? ExceptionsToolWindowContent : null;
	}

	sealed class ExceptionsToolWindowContent : ToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("82575354-AB18-408B-846B-AA585B7B2B4A");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override IInputElement FocusedElement => exceptionsContent.Value.FocusedElement;
		public override FrameworkElement ZoomElement => exceptionsContent.Value.ZoomElement;
		public override Guid Guid => THE_GUID;
		public override string Title => dnSpy_Debugger_Resources.Window_ExceptionSettings;
		public override object UIObject => exceptionsContent.Value.UIObject;
		public bool CanFocus => true;

		readonly Lazy<IExceptionsContent> exceptionsContent;

		public ExceptionsToolWindowContent(Lazy<IExceptionsContent> exceptionsContent) {
			this.exceptionsContent = exceptionsContent;
		}

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				exceptionsContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				exceptionsContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				exceptionsContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				exceptionsContent.Value.OnHidden();
				break;
			}
		}

		public void Focus() => exceptionsContent.Value.Focus();
	}
}
