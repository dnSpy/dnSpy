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

namespace dnSpy.Debugger.CallStack {
	[Export(typeof(IMainToolWindowContentCreator))]
	sealed class CallStackToolWindowContentCreator : IMainToolWindowContentCreator {
		readonly Lazy<ICallStackContent> callStackContent;

		public CallStackToolWindowContent CallStackToolWindowContent {
			get { return callStackToolWindowContent ?? (callStackToolWindowContent = new CallStackToolWindowContent(callStackContent)); }
		}
		CallStackToolWindowContent callStackToolWindowContent;

		[ImportingConstructor]
		CallStackToolWindowContentCreator(Lazy<ICallStackContent> callStackContent) {
			this.callStackContent = callStackContent;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(CallStackToolWindowContent.THE_GUID, CallStackToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_CALLSTACK, false); }
		}

		public IToolWindowContent GetOrCreate(Guid guid) {
			if (guid == CallStackToolWindowContent.THE_GUID)
				return CallStackToolWindowContent;
			return null;
		}
	}

	sealed class CallStackToolWindowContent : IToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("0E53B79D-EC30-44B6-86A3-DFFCE364EB4A");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public IInputElement FocusedElement {
			get { return callStackContent.Value.FocusedElement; }
		}

		public FrameworkElement ScaleElement {
			get { return callStackContent.Value.ScaleElement; }
		}

		public Guid Guid {
			get { return THE_GUID; }
		}

		public string Title {
			get { return dnSpy_Debugger_Resources.Window_CallStack; }
		}

		public object ToolTip {
			get { return null; }
		}

		public object UIObject {
			get { return callStackContent.Value.UIObject; }
		}

		public bool CanFocus {
			get { return true; }
		}

		readonly Lazy<ICallStackContent> callStackContent;

		public CallStackToolWindowContent(Lazy<ICallStackContent> callStackContent) {
			this.callStackContent = callStackContent;
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				callStackContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				callStackContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				callStackContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				callStackContent.Value.OnHidden();
				break;
			}
		}

		public void Focus() {
			callStackContent.Value.Focus();
		}
	}
}
