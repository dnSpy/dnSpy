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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.TreeView.Text;

namespace dnSpy.Settings.Dialog {
	sealed class AppSettingsPageVM : TreeNodeData {
		public override Guid Guid => Guid.Empty;
		public override object ToolTip => null;
		public override ImageReference Icon => Page.Icon;

		public double Order => Page.Order;
		public List<AppSettingsPageVM> Children { get; }
		internal AppSettingsPage Page { get; }
		public object UIObject => uiObject ?? (uiObject = CreateUIObject());
		object uiObject;

		public override object Text {
			get {
				var writer = new TextClassifierTextColorWriter();
				writer.Write(BoxedTextColor.Text, Page.Title);
				var classifierContext = new TreeViewNodeClassifierContext(writer.Text, context.TreeView, this, isToolTip: false, colorize: true, colors: writer.Colors);
				return context.TreeViewNodeTextElementProvider.CreateTextElement(classifierContext, TreeViewContentTypes.TreeViewNodeAppSettings, TextElementFlags.FilterOutNewLines);
			}
		}

		readonly ContextVM context;

		public AppSettingsPageVM(AppSettingsPage page, ContextVM context) {
			if (page == null)
				throw new ArgumentNullException(nameof(page));
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			Page = page;
			Children = new List<AppSettingsPageVM>();
			this.context = context;
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
