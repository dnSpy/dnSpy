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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Resources;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.TreeView.Text;
using dnSpy.Images;

namespace dnSpy.Settings.Dialog {
	[Export(typeof(IAppSettingsService))]
	sealed class AppSettingsService : IAppSettingsService {
		static readonly Guid rootGuid = Guid.Empty;
		readonly IAppWindow appWindow;
		readonly ITreeViewService treeViewService;
		readonly ITreeViewNodeTextElementProvider treeViewNodeTextElementProvider;
		readonly Lazy<IAppSettingsPageContainer, IAppSettingsPageContainerMetadata>[] appSettingsPageContainers;
		readonly Lazy<IAppSettingsPageProvider>[] appSettingsPageProviders;
		readonly Lazy<IAppSettingsModifiedListener, IAppSettingsModifiedListenerMetadata>[] appSettingsModifiedListeners;
		Guid? lastSelectedGuid;
		bool showingDialog;
		ContextVM currentContextVM;

		[ImportingConstructor]
		AppSettingsService(IAppWindow appWindow, ITreeViewService treeViewService, ITreeViewNodeTextElementProvider treeViewNodeTextElementProvider, [ImportMany] IEnumerable<Lazy<IAppSettingsPageContainer, IAppSettingsPageContainerMetadata>> appSettingsPageContainers, [ImportMany] IEnumerable<Lazy<IAppSettingsPageProvider>> appSettingsPageProviders, [ImportMany] IEnumerable<Lazy<IAppSettingsModifiedListener, IAppSettingsModifiedListenerMetadata>> appSettingsModifiedListeners) {
			this.appWindow = appWindow;
			this.treeViewService = treeViewService;
			this.treeViewNodeTextElementProvider = treeViewNodeTextElementProvider;
			this.appSettingsPageContainers = appSettingsPageContainers.OrderBy(a => a.Metadata.Order).ToArray();
			this.appSettingsPageProviders = appSettingsPageProviders.ToArray();
			this.appSettingsModifiedListeners = appSettingsModifiedListeners.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public void Show(Window owner) => Show2(null, owner);
		public void Show(Guid guid, Window owner) => Show2(guid, owner);

		void Show2(Guid? guid, Window owner) {
			Debug.Assert(!showingDialog);
			if (showingDialog)
				return;
			try {
				showingDialog = true;
				ShowCore(guid, owner);
			}
			finally {
				currentContextVM?.TreeView?.Dispose();
				currentContextVM = null;
				showingDialog = false;
			}
		}

		void ShowCore(Guid? guid, Window owner) {
			currentContextVM = new ContextVM {
				TreeViewNodeTextElementProvider = treeViewNodeTextElementProvider,
			};

			var allVMs = CreateSettingsPages();
			Debug.Assert(allVMs.Any(a => a.Page.Guid == rootGuid));
			var rootVM = CreateRootVM(allVMs);
			if (rootVM.Children.Count == 0)
				return;

			currentContextVM.TreeView = CreateTreeView(rootVM);

			if (guid == null)
				guid = lastSelectedGuid;
			var selectedItem = (guid != null ? allVMs.FirstOrDefault(a => a.Page.Guid == guid.Value) : null) ?? rootVM.Children.FirstOrDefault();
			if (selectedItem != null)
				currentContextVM.TreeView.SelectItems(new[] { selectedItem });

			var dlg = new AppSettingsDlg();
			dlg.treeViewContentPresenter.Content = currentContextVM.TreeView.UIObject;
			dlg.Owner = owner ?? appWindow.MainWindow;
			bool saveSettings = dlg.ShowDialog() == true;
			lastSelectedGuid = (currentContextVM.TreeView.SelectedItem as AppSettingsPageVM)?.Page.Guid;
			Debug.Assert(lastSelectedGuid != null);

			var appRefreshSettings = new AppRefreshSettings();
			if (saveSettings) {
				foreach (var vm in allVMs) {
					var page2 = vm.Page as IAppSettingsPage2;
					if (page2 != null)
						page2.OnApply(appRefreshSettings);
					else
						vm.Page.OnApply();
				}
			}

			foreach (var vm in allVMs)
				vm.Page.OnClosed();

			if (saveSettings) {
				foreach (var listener in appSettingsModifiedListeners)
					listener.Value.OnSettingsModified(appRefreshSettings);
			}
		}

		ITreeView CreateTreeView(AppSettingsPageVM rootVM) {
			var options = new TreeViewOptions {
				CanDragAndDrop = false,
				SelectionMode = SelectionMode.Single,
				ForegroundBrushResourceKey = "AppSettingsTreeViewForeground",
			};
			var treeView = treeViewService.Create(new Guid("99334011-E467-456F-A0DF-BD4DBD0F0519"), options);
			foreach (var vm in rootVM.Children)
				treeView.Root.AddChild(treeView.Create(vm));

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

		void RemoveEmptyNodes(AppSettingsPageVM vm) {
			for (int i = vm.Children.Count - 1; i >= 0; i--) {
				var child = vm.Children[i];
				RemoveEmptyNodes(child);
				if (child.Children.Count == 0 && child.Page is AppSettingsPageContainer)
					vm.Children.RemoveAt(i);
			}
		}

		void SortChildren(AppSettingsPageVM vm) {
			vm.Children.Sort(AppSettingsPageVMSorter.Instance);
			foreach (var child in vm.Children)
				SortChildren(child);
		}

		sealed class AppSettingsPageVMSorter : IComparer<AppSettingsPageVM> {
			public static readonly AppSettingsPageVMSorter Instance = new AppSettingsPageVMSorter();
			public int Compare(AppSettingsPageVM x, AppSettingsPageVM y) => x.Order.CompareTo(y.Order);
		}

		AppSettingsPageVM InitializeChildren(AppSettingsPageVM[] vms) {
			var dict = new Dictionary<Guid, AppSettingsPageVM>(vms.Length);
			foreach (var vm in vms) {
				Debug.Assert(!dict.ContainsKey(vm.Page.Guid));
				dict.Add(vm.Page.Guid, vm);
			}

			foreach (var vm in vms) {
				if (vm.Page.Guid == rootGuid)
					continue;

				AppSettingsPageVM parentVM;
				if (!dict.TryGetValue(vm.Page.ParentGuid, out parentVM)) {
					Debug.Fail($"No parent with Guid {vm.Page.ParentGuid}");
					continue;
				}

				parentVM.Children.Add(vm);
			}

			return vms.First(a => a.Page.Guid == rootGuid);
		}

		AppSettingsPageVM[] CreateSettingsPages() {
			var dict = new Dictionary<Guid, AppSettingsPageVM>();

			dict.Add(rootGuid, new AppSettingsPageVM(new AppSettingsPageContainer(string.Empty, 0, rootGuid, rootGuid, ImageReference.None), currentContextVM));

			foreach (var lz in appSettingsPageContainers) {
				var vm = TryCreate(lz.Value, lz.Metadata, currentContextVM);
				if (vm == null)
					continue;
				Debug.Assert(!dict.ContainsKey(vm.Page.Guid));
				if (!dict.ContainsKey(vm.Page.Guid))
					dict.Add(vm.Page.Guid, vm);
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
			public override object UIObject { get; }
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
	}
}
