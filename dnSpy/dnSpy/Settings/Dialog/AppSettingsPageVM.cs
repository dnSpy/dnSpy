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
using System.Windows.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Settings.Dialog {
	sealed class AppSettingsPageVM : TreeNodeData {
		public override Guid Guid => Guid.Empty;
		public override object Text => Page.Title;
		public override object ToolTip => null;
		public override ImageReference Icon => Page.Icon;

		public double Order => Page.Order;
		public List<AppSettingsPageVM> Children { get; }
		internal IAppSettingsPage Page { get; }
		public object UIObject => uiObject ?? (uiObject = CreateUIObject());
		object uiObject;

		public AppSettingsPageVM(IAppSettingsPage page) {
			if (page == null)
				throw new ArgumentNullException(nameof(page));
			Page = page;
			Children = new List<AppSettingsPageVM>();
		}

		object CreateUIObject() {
			var uiObj = Page.UIObject;
			if (uiObj == null && Children.Count != 0)
				return Children[0].UIObject;
			return CreateUIObject(uiObj);
		}

		static object CreateUIObject(object uiObj) {
			if (uiObj is ScrollViewer)
				return uiObj;
			return new ScrollViewer {
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				Content = uiObj,
			};
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() => Children;
		public override void OnRefreshUI() { }
	}
}
