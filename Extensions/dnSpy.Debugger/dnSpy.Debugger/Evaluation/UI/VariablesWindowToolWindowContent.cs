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

namespace dnSpy.Debugger.Evaluation.UI {
	abstract class VariablesWindowToolWindowContentProviderBase : IToolWindowContentProvider {
		readonly Lazy<IVariablesWindowContent> variablesWindowContent;

		public VariablesWindowToolWindowContent VariablesWindowToolWindowContent => variablesWindowToolWindowContent ?? (variablesWindowToolWindowContent = new VariablesWindowToolWindowContent(contentGuid, contentTitle, variablesWindowContent));
		VariablesWindowToolWindowContent variablesWindowToolWindowContent;

		readonly Guid contentGuid;
		readonly double contentOrder;
		readonly string contentTitle;

		protected VariablesWindowToolWindowContentProviderBase(Guid guid, double contentOrder, string contentTitle, Lazy<IVariablesWindowContent> variablesWindowContent) {
			contentGuid = guid;
			this.contentOrder = contentOrder;
			this.contentTitle = contentTitle;
			this.variablesWindowContent = variablesWindowContent;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(contentGuid, VariablesWindowToolWindowContent.DEFAULT_LOCATION, contentOrder, false); }
		}

		public ToolWindowContent GetOrCreate(Guid guid) => guid == contentGuid ? VariablesWindowToolWindowContent : null;
	}

	sealed class VariablesWindowToolWindowContent : ToolWindowContent, IFocusable {
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override IInputElement FocusedElement => variablesWindowContent.Value.FocusedElement;
		public override FrameworkElement ZoomElement => variablesWindowContent.Value.ZoomElement;
		public override Guid Guid { get; }
		public override string Title { get; }
		public override object UIObject => variablesWindowContent.Value.UIObject;
		public bool CanFocus => true;

		readonly Lazy<IVariablesWindowContent> variablesWindowContent;

		public VariablesWindowToolWindowContent(Guid guid, string contentTitle, Lazy<IVariablesWindowContent> variablesWindowContent) {
			Guid = guid;
			Title = contentTitle;
			this.variablesWindowContent = variablesWindowContent;
		}

		public void Focus() => variablesWindowContent.Value.Focus();

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				variablesWindowContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				variablesWindowContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				variablesWindowContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				variablesWindowContent.Value.OnHidden();
				break;
			}
		}
	}
}
