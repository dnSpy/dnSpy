/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.Debugger.ToolWindows.Locals.Shared {
	abstract class LocalsToolWindowContentProviderBase : IToolWindowContentProvider {
		readonly Lazy<ILocalsContent> localsContent;

		public LocalsToolWindowContent LocalsToolWindowContent => localsToolWindowContent ?? (localsToolWindowContent = new LocalsToolWindowContent(contentGuid, contentTitle, localsContent));
		LocalsToolWindowContent localsToolWindowContent;

		readonly Guid contentGuid;
		readonly double contentOrder;
		readonly string contentTitle;

		protected LocalsToolWindowContentProviderBase(Guid guid, double contentOrder, string contentTitle, Lazy<ILocalsContent> localsContent) {
			contentGuid = guid;
			this.contentOrder = contentOrder;
			this.contentTitle = contentTitle;
			this.localsContent = localsContent;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(contentGuid, LocalsToolWindowContent.DEFAULT_LOCATION, contentOrder, false); }
		}

		public ToolWindowContent GetOrCreate(Guid guid) => guid == contentGuid ? LocalsToolWindowContent : null;
	}

	sealed class LocalsToolWindowContent : ToolWindowContent, IFocusable {
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override IInputElement FocusedElement => localsContent.Value.FocusedElement;
		public override FrameworkElement ZoomElement => localsContent.Value.ZoomElement;
		public override Guid Guid { get; }
		public override string Title { get; }
		public override object UIObject => localsContent.Value.UIObject;
		public bool CanFocus => true;

		readonly Lazy<ILocalsContent> localsContent;

		public LocalsToolWindowContent(Guid guid, string contentTitle, Lazy<ILocalsContent> localsContent) {
			Guid = guid;
			Title = contentTitle;
			this.localsContent = localsContent;
		}

		public void Focus() => localsContent.Value.Focus();

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
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
	}
}
