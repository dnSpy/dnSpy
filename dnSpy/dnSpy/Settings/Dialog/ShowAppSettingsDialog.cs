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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Resources;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.TreeView.Text;
using dnSpy.Images;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Settings.Dialog {
	sealed class ShowAppSettingsDialog : ViewModelBase, IDisposable, IContentConverter {
		static readonly Guid rootGuid = Guid.Empty;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly ITextElementProvider textElementProvider;
		readonly ITreeViewService treeViewService;
		readonly Lazy<IAppSettingsPageContainer, IAppSettingsPageContainerMetadata>[] appSettingsPageContainers;
		readonly Lazy<IAppSettingsPageProvider>[] appSettingsPageProviders;
		readonly Lazy<IAppSettingsModifiedListener, IAppSettingsModifiedListenerMetadata>[] appSettingsModifiedListeners;
		readonly ContextVM currentContextVM;
		AppSettingsPageVM[] allPages;
		AppSettingsPageVM rootVM;
		AppSettingsDlg appSettingsDlg;
		int converterVersion;

		public object TreeViewUIObject => currentContextVM.TreeView.UIObject;
		public Guid? LastSelectedGuid { get; private set; }

		public string SearchText {
			get { return searchText; }
			set {
				if (searchText != value) {
					searchText = value;
					OnPropertyChanged(nameof(SearchText));
					FilterTreeView(searchText);
				}
			}
		}
		string searchText = string.Empty;

		public ShowAppSettingsDialog(IClassificationFormatMap classificationFormatMap, ITextElementProvider textElementProvider, ITreeViewService treeViewService, ITreeViewNodeTextElementProvider treeViewNodeTextElementProvider, Lazy<IAppSettingsPageContainer, IAppSettingsPageContainerMetadata>[] appSettingsPageContainers, Lazy<IAppSettingsPageProvider>[] appSettingsPageProviders, Lazy<IAppSettingsModifiedListener, IAppSettingsModifiedListenerMetadata>[] appSettingsModifiedListeners) {
			this.classificationFormatMap = classificationFormatMap;
			this.textElementProvider = textElementProvider;
			this.treeViewService = treeViewService;
			this.appSettingsPageContainers = appSettingsPageContainers;
			this.appSettingsPageProviders = appSettingsPageProviders;
			this.appSettingsModifiedListeners = appSettingsModifiedListeners;
			currentContextVM = new ContextVM {
				TreeViewNodeTextElementProvider = treeViewNodeTextElementProvider,
				SearchMatcher = new SearchMatcher(),
			};
			currentContextVM.SearchMatcher.SetSearchText(SearchText);
			converterVersion = ContentConverterProperties.DefaultContentConverterVersion + 1;
		}

		public void Select(Guid value) {
			var page = allPages.FirstOrDefault(a => a.Page.Guid == value);
			if (page?.Parent == null)
				return;
			currentContextVM.TreeView.SelectItems(new[] { page });
		}

		public void Show(Guid? guid, Window ownerWindow) {
			if (ownerWindow == null)
				throw new ArgumentNullException(nameof(ownerWindow));
			LastSelectedGuid = guid;

			allPages = CreateSettingsPages();
			Debug.Assert(allPages.Any(a => a.Page.Guid == rootGuid));
			rootVM = CreateRootVM(allPages);
			if (rootVM.Children.Count == 0)
				return;

			currentContextVM.TreeView = CreateTreeView(rootVM);

			var selectedItem = (guid != null ? allPages.FirstOrDefault(a => a.Page.Guid == guid.Value) : null) ?? rootVM.Children.FirstOrDefault();
			if (selectedItem != null)
				currentContextVM.TreeView.SelectItems(new[] { selectedItem });

			appSettingsDlg = new AppSettingsDlg();
			appSettingsDlg.DataContext = this;
			InitializeKeyboardBindings();

			ContentConverterProperties.SetContentConverter(appSettingsDlg, this);
			ContentConverterProperties.SetContentConverterVersion(appSettingsDlg, converterVersion);
			appSettingsDlg.Owner = ownerWindow;
			bool saveSettings = appSettingsDlg.ShowDialog() == true;
			LastSelectedGuid = (currentContextVM.TreeView.SelectedItem as AppSettingsPageVM)?.Page.Guid;

			var appRefreshSettings = new AppRefreshSettings();
			if (saveSettings) {
				foreach (var page in allPages) {
					var page2 = page.Page as IAppSettingsPage2;
					if (page2 != null)
						page2.OnApply(appRefreshSettings);
					else
						page.Page.OnApply();
				}
			}

			foreach (var page in allPages)
				page.Page.OnClosed();

			if (saveSettings) {
				foreach (var listener in appSettingsModifiedListeners)
					listener.Value.OnSettingsModified(appRefreshSettings);
			}
		}

		void InitializeKeyboardBindings() {
			var cmd = new RelayCommand(a => {
				appSettingsDlg.searchTextBox.Focus();
				appSettingsDlg.searchTextBox.SelectAll();
			});
			appSettingsDlg.InputBindings.Add(new KeyBinding(cmd, Key.E, ModifierKeys.Control));
			appSettingsDlg.InputBindings.Add(new KeyBinding(cmd, Key.F, ModifierKeys.Control));
		}

		void FilterTreeView(string searchText) {
			if (string.IsNullOrWhiteSpace(searchText))
				searchText = string.Empty;
			if (searchText == string.Empty) {
				if (!isFiltering)
					return;
				currentContextVM.SearchMatcher.SetSearchText(string.Empty);
				foreach (var page in allPages) {
					page.TreeNode.IsHidden = false;
					page.TreeNode.IsExpanded = page.SavedIsExpanded;
				}
				isFiltering = false;
			}
			else {
				currentContextVM.SearchMatcher.SetSearchText(searchText);
				if (!isFiltering) {
					foreach (var page in allPages)
						page.SavedIsExpanded = page.TreeNode.IsExpanded;
				}
				FilterChildren(rootVM, currentContextVM.SearchMatcher);
				isFiltering = true;
			}
			RefreshAllNodes();
			if (currentContextVM.TreeView.SelectedItem == null) {
				var first = rootVM.Children.FirstOrDefault(a => !a.TreeNode.IsHidden);
				if (first != null) {
					currentContextVM.TreeView.SelectItems(new[] { first });
					// The treeview steals the focus. It uses prio Loaded.
					appSettingsDlg.searchTextBox.Focus();
					currentContextVM.TreeView.UIObject.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
						appSettingsDlg.searchTextBox.Focus();
					}));
				}
			}
		}
		bool isFiltering;

		void RefreshAllNodes() {
			foreach (var page in allPages)
				page.ClearUICache();
			foreach (var page in allPages)
				page.RefreshUI();
			ContentConverterProperties.SetContentConverterVersion(appSettingsDlg, ++converterVersion);
		}

		void FilterChildren(AppSettingsPageVM page, SearchMatcher matcher) {
			bool atLeastOneChildVisible = false;
			foreach (var child in page.Children) {
				FilterChildren(child, matcher);
				atLeastOneChildVisible |= !child.TreeNode.IsHidden;
			}
			bool isVisible = atLeastOneChildVisible || IsVisible(page, matcher);
			page.TreeNode.IsHidden = !isVisible;
			if (isVisible)
				page.TreeNode.IsExpanded = true;
		}

		bool IsVisible(AppSettingsPageVM page, SearchMatcher matcher) {
			pageStringsList.Clear();
			pageTitlesList.Clear();
			var p = page;
			// Don't include the root
			while (p.Parent != null) {
				pageTitlesList.Add(p.Page.Title);
				p = p.Parent;
			}
			foreach (var s in page.GetSearchableStrings(appSettingsDlg))
				pageStringsList.Add(s);
			bool res = matcher.IsMatchAll(pageTitlesList, pageStringsList);
			pageStringsList.Clear();
			pageTitlesList.Clear();
			return res;
		}
		readonly List<string> pageTitlesList = new List<string>();
		readonly List<string> pageStringsList = new List<string>();

		object IContentConverter.Convert(object content, object ownerControl) {
			var result = TryConvert(content, ownerControl);
			if (result != null)
				return result;

			var textControl = ownerControl as TextControl;
			if (textControl != null) {
				return new TextBlock {
					Text = textControl.Content as string,
					TextTrimming = textControl.TextTrimming,
					TextWrapping = textControl.TextWrapping,
				};
			}

			return content;
		}

		object TryConvert(object content, object ownerControl) {
			if (!isFiltering)
				return null;
			var textContent = content as string;
			if (textContent == null)
				return null;

			textContent = UIHelpers.RemoveAccessKeys(textContent);

			// Quick check here because access keys aren't shown if we return a TextBlock
			if (!currentContextVM.SearchMatcher.IsMatchAny(textContent))
				return null;

			const bool colorize = true;
			var context = new AppSettingsTextClassifierContext(currentContextVM.SearchMatcher, textContent, PredefinedTextClassifierTags.OptionsDialogText, colorize);
			return textElementProvider.CreateTextElement(classificationFormatMap, context, ContentTypes.OptionsDialogText, GetTextFlags(ownerControl));
		}

		static TextElementFlags GetTextFlags(object ownerControl) {
			TextTrimming textTrimming = TextTrimming.None;
			TextWrapping textWrapping = TextWrapping.NoWrap;

			var textControl = ownerControl as TextControl;
			if (textControl != null) {
				textTrimming = textControl.TextTrimming;
				textWrapping = textControl.TextWrapping;
			}

			TextElementFlags flags = 0;
			switch (textTrimming) {
			case TextTrimming.None: flags |= TextElementFlags.NoTrimming; break;
			case TextTrimming.CharacterEllipsis: flags |= TextElementFlags.CharacterEllipsis; break;
			case TextTrimming.WordEllipsis: flags |= TextElementFlags.WordEllipsis; break;
			default: Debug.Fail($"Unknown trimming: {textTrimming}"); break;
			}
			switch (textWrapping) {
			case TextWrapping.WrapWithOverflow: flags |= TextElementFlags.WrapWithOverflow; break;
			case TextWrapping.NoWrap: flags |= TextElementFlags.NoWrap; break;
			case TextWrapping.Wrap: flags |= TextElementFlags.Wrap; break;
			default: Debug.Fail($"Unknown wrapping: {textWrapping}"); break;
			}
			return flags;
		}

		ITreeView CreateTreeView(AppSettingsPageVM rootVM) {
			var options = new TreeViewOptions {
				CanDragAndDrop = false,
				SelectionMode = SelectionMode.Single,
				ForegroundBrushResourceKey = "AppSettingsTreeViewForeground",
				RootNode = rootVM,
			};
			var treeView = treeViewService.Create(new Guid("99334011-E467-456F-A0DF-BD4DBD0F0519"), options);

			treeView.UIObject.Padding = new Thickness(0, 2, 0, 2);
			treeView.UIObject.BorderThickness = new Thickness(1);
			treeView.UIObject.SetResourceReference(Control.BorderBrushProperty, "AppSettingsTreeViewBorder");
			treeView.UIObject.SetResourceReference(Control.ForegroundProperty, "AppSettingsTreeViewForeground");
			treeView.UIObject.SetResourceReference(Control.BackgroundProperty, "AppSettingsTreeViewBackground");

			return treeView;
		}

		AppSettingsPageVM CreateRootVM(AppSettingsPageVM[] allVMs) {
			var rootVM = InitializeChildren(allVMs);
			RemoveEmptyNodes(rootVM);
			SortChildren(rootVM);
			return rootVM;
		}

		void RemoveEmptyNodes(AppSettingsPageVM page) {
			for (int i = page.Children.Count - 1; i >= 0; i--) {
				var child = page.Children[i];
				RemoveEmptyNodes(child);
				if (child.Children.Count == 0 && child.Page is AppSettingsPageContainer)
					page.Children.RemoveAt(i);
			}
		}

		void SortChildren(AppSettingsPageVM page) {
			page.Children.Sort(AppSettingsPageVMSorter.Instance);
			foreach (var child in page.Children)
				SortChildren(child);
		}

		sealed class AppSettingsPageVMSorter : IComparer<AppSettingsPageVM> {
			public static readonly AppSettingsPageVMSorter Instance = new AppSettingsPageVMSorter();
			public int Compare(AppSettingsPageVM x, AppSettingsPageVM y) => x.Order.CompareTo(y.Order);
		}

		AppSettingsPageVM InitializeChildren(AppSettingsPageVM[] pages) {
			var dict = new Dictionary<Guid, AppSettingsPageVM>(pages.Length);
			foreach (var page in pages) {
				Debug.Assert(!dict.ContainsKey(page.Page.Guid));
				dict.Add(page.Page.Guid, page);
			}

			foreach (var page in pages) {
				if (page.Page.Guid == rootGuid)
					continue;

				AppSettingsPageVM parentPage;
				if (!dict.TryGetValue(page.Page.ParentGuid, out parentPage)) {
					Debug.Fail($"No parent with Guid {page.Page.ParentGuid}");
					continue;
				}

				page.Parent = parentPage;
				parentPage.Children.Add(page);
			}

			return pages.First(a => a.Page.Guid == rootGuid);
		}

		AppSettingsPageVM[] CreateSettingsPages() {
			var dict = new Dictionary<Guid, AppSettingsPageVM>();

			dict.Add(rootGuid, new AppSettingsPageVM(new AppSettingsPageContainer(string.Empty, 0, rootGuid, rootGuid, ImageReference.None), currentContextVM));

			foreach (var lz in appSettingsPageContainers) {
				var page = TryCreate(lz.Value, lz.Metadata, currentContextVM);
				if (page == null)
					continue;
				Debug.Assert(!dict.ContainsKey(page.Page.Guid));
				if (!dict.ContainsKey(page.Page.Guid))
					dict.Add(page.Page.Guid, page);
			}

			foreach (var lz in appSettingsPageProviders) {
				foreach (var page in lz.Value.Create()) {
					Debug.Assert(page != null);
					if (page == null)
						continue;
					var vm = new AppSettingsPageVM(page, currentContextVM);
					Debug.Assert(!dict.ContainsKey(vm.Page.Guid));
					if (!dict.ContainsKey(vm.Page.Guid))
						dict.Add(vm.Page.Guid, vm);
				}
			}

			return dict.Values.ToArray();
		}

		sealed class AppSettingsPageContainer : AppSettingsPage {
			public override Guid ParentGuid => parentGuid;
			public override Guid Guid => guid;
			public override double Order => order;
			public override string Title => title;
			public override ImageReference Icon => icon;
			public override object UIObject => null;
			readonly string title;
			readonly double order;
			readonly Guid guid;
			readonly Guid parentGuid;
			readonly ImageReference icon;

			public AppSettingsPageContainer(string title, double order, Guid guid, Guid parentGuid, ImageReference icon) {
				this.title = title;
				this.order = order;
				this.guid = guid;
				this.parentGuid = parentGuid;
				this.icon = icon;
			}

			public override void OnApply() { }
		}

		static AppSettingsPageVM TryCreate(object obj, IAppSettingsPageContainerMetadata md, ContextVM context) {
			Guid? guid = md.Guid == null ? null : TryParseGuid(md.Guid);
			Debug.Assert(guid != null, "Invalid GUID");
			if (guid == null)
				return null;

			Guid? parentGuid = md.ParentGuid == null ? rootGuid : TryParseGuid(md.ParentGuid);
			Debug.Assert(parentGuid != null, "Invalid Parent GUID");
			if (parentGuid == null)
				return null;

			if (string.IsNullOrEmpty(md.Title))
				return null;

			var title = ResourceHelper.GetString(obj, md.Title);
			var icon = ImageReferenceHelper.GetImageReference(obj, md.Icon) ?? ImageReference.None;
			return new AppSettingsPageVM(new AppSettingsPageContainer(title, md.Order, guid.Value, parentGuid.Value, icon), context);
		}

		static Guid? TryParseGuid(string guidString) {
			Guid guid;
			if (Guid.TryParse(guidString, out guid))
				return guid;
			return null;
		}

		public void Dispose() {
			currentContextVM?.TreeView?.Dispose();
		}
	}
}
