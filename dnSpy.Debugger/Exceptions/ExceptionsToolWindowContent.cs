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

namespace dnSpy.Debugger.Exceptions {
	[Export(typeof(IMainToolWindowContentCreator))]
	sealed class ExceptionsToolWindowContentCreator : IMainToolWindowContentCreator {
		readonly Lazy<IExceptionsContent> exceptionsContent;

		public ExceptionsToolWindowContent ExceptionsToolWindowContent {
			get { return exceptionsToolWindowContent ?? (exceptionsToolWindowContent = new ExceptionsToolWindowContent(exceptionsContent)); }
		}
		ExceptionsToolWindowContent exceptionsToolWindowContent;

		[ImportingConstructor]
		ExceptionsToolWindowContentCreator(Lazy<IExceptionsContent> exceptionsContent) {
			this.exceptionsContent = exceptionsContent;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(ExceptionsToolWindowContent.THE_GUID, ExceptionsToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_EXCEPTIONS, false); }
		}

		public IToolWindowContent GetOrCreate(Guid guid) {
			if (guid == ExceptionsToolWindowContent.THE_GUID)
				return ExceptionsToolWindowContent;
			return null;
		}
	}

	sealed class ExceptionsToolWindowContent : IToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("82575354-AB18-408B-846B-AA585B7B2B4A");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.Default;

		public IInputElement FocusedElement {
			get { return exceptionsContent.Value.FocusedElement; }
		}

		public FrameworkElement ScaleElement {
			get { return exceptionsContent.Value.ScaleElement; }
		}

		public Guid Guid {
			get { return THE_GUID; }
		}

		public string Title {
			get { return dnSpy_Debugger_Resources.Window_ExceptionSettings; }
		}

		public object ToolTip {
			get { return null; }
		}

		public object UIObject {
			get { return exceptionsContent.Value.UIObject; }
		}

		public bool CanFocus {
			get { return true; }
		}

		readonly Lazy<IExceptionsContent> exceptionsContent;

		public ExceptionsToolWindowContent(Lazy<IExceptionsContent> exceptionsContent) {
			this.exceptionsContent = exceptionsContent;
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
		}

		public void Focus() {
			exceptionsContent.Value.Focus();
		}
	}
}
