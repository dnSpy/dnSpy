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
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.TreeView.Text;

namespace dnSpy.Settings.Dialog {
	sealed class AppSettingsPageVM : TreeNodeData, INotifyPropertyChanged {
		public event PropertyChangedEventHandler? PropertyChanged;
		public AppSettingsPageVM? Parent { get; set; }
		public override Guid Guid => Guid.Empty;
		public override object? ToolTip => null;
		public override ImageReference Icon => Page.Icon;

		public double Order => Page.Order;
		public List<AppSettingsPageVM> Children { get; }
		internal AppSettingsPage Page { get; }
		public object? UIObject => VisiblePageAndUIObject?.UIObject;
		internal AppSettingsPageVM? VisiblePage => VisiblePageAndUIObject?.Page;
		PageAndUIObject? VisiblePageAndUIObject => pageAndUIObject ??= GetOrCreatePageAndUIObject();
		PageAndUIObject? pageAndUIObject;

		sealed class PageAndUIObject {
			public AppSettingsPageVM Page { get; }
			public object? UIObject { get; }
			public PageAndUIObject(AppSettingsPageVM page, object uiObject) {
				Page = page;
				UIObject = uiObject;
			}
		}

		public bool SavedIsExpanded { get; set; }

		public override object? Text {
			get {
				var writer = new TextClassifierTextColorWriter();
				writer.Write(BoxedTextColor.Text, Page.Title);
				var classifierContext = new AppSettingsTreeViewNodeClassifierContext(context.SearchMatcher, writer.Text, context.TreeView, this, isToolTip: false, colorize: true, colors: writer.Colors);
				return context.TreeViewNodeTextElementProvider.CreateTextElement(classifierContext, TreeViewContentTypes.TreeViewNodeAppSettings, TextElementFlags.FilterOutNewLines);
			}
		}

		readonly PageContext context;

		public AppSettingsPageVM(AppSettingsPage page, PageContext context) {
			Page = page ?? throw new ArgumentNullException(nameof(page));
			Children = new List<AppSettingsPageVM>();
			this.context = context ?? throw new ArgumentNullException(nameof(context));
		}

		PageAndUIObject? GetOrCreatePageAndUIObject() {
			var uiObj = context.PageUIObjectLoader.GetUIObject(Page);
			if (!(uiObj is null))
				return createdPageAndUIObject ??= new PageAndUIObject(this, CreateUIObject(uiObj));

			// Try to pick a visible child
			foreach (var child in Children) {
				if (!child.TreeNode.IsHidden)
					return child.VisiblePageAndUIObject;
			}

			if (Children.Count == 0)
				return null;
			return Children[0].VisiblePageAndUIObject;
		}
		PageAndUIObject? createdPageAndUIObject;

		static object CreateUIObject(object uiObj) {
			if (uiObj is ScrollViewer)
				return uiObj;
			return new ScrollViewer {
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				Content = uiObj,
			};
		}

		public void ClearUICache() =>
			// Make sure we don't show hidden pages
			pageAndUIObject = null;

		public void RefreshUI() {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UIObject)));
			TreeNode.RefreshUI();
		}

		public IEnumerable<string> GetSearchableStrings(FrameworkElement fwElem) {
			if (searchableStrings is null) {
				var list = new List<string>();
				foreach (var s in GetDataTemplateStrings(fwElem))
					list.Add(UIHelpers.RemoveAccessKeys(s));
				foreach (var s in Page.GetSearchStrings() ?? Array.Empty<string>())
					list.Add(UIHelpers.RemoveAccessKeys(s));
				searchableStrings = list.ToArray();
			}
			return searchableStrings;
		}
		string[]? searchableStrings;

		IEnumerable<string> GetDataTemplateStrings(FrameworkElement fwElem) {
			var obj = Page.GetStringsObject();
			if (obj is null)
				return Array.Empty<string>();

			if (obj is UIElement uiElem)
				return GetStrings(uiElem);

			var key = new DataTemplateKey(obj as Type ?? obj.GetType());
			var dt = fwElem.TryFindResource(key) as DataTemplate;
			if (dt is null)
				return Array.Empty<string>();

			return GetStrings(dt.LoadContent());
		}

		static IEnumerable<string> GetStrings(DependencyObject obj) {
			if (obj is null)
				yield break;
			var objString = TryGetString(obj);
			if (!(objString is null))
				yield return objString;
			foreach (var childObj in LogicalTreeHelper.GetChildren(obj)) {
				var child = childObj as DependencyObject;
				if (child is null)
					continue;
				foreach (var s in GetStrings(child))
					yield return s;
			}
		}

		static string? TryGetString(DependencyObject obj) {
			string? s;

			s = (obj as GroupBox)?.Header as string;
			if (!(s is null))
				return s;

			// Label, CheckBox, Button, TextControl and others
			s = (obj as ContentControl)?.Content as string;
			if (!(s is null))
				return s;

			Debug.Assert(!(obj is TextBlock), $"Use {nameof(TextControl)} instead so the text can be highlighted");
			s = (obj as TextBlock)?.Text;
			if (!(s is null))
				return s;

			return null;
		}

		public override IEnumerable<TreeNodeData> CreateChildren() => Children;
		public override void OnRefreshUI() { }
	}
}
