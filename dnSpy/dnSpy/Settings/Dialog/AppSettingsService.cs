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
using dnSpy.Images;

namespace dnSpy.Settings.Dialog {
	[Export(typeof(IAppSettingsService))]
	sealed class AppSettingsService : IAppSettingsService {
		static readonly Guid rootGuid = Guid.Empty;
		readonly IAppWindow appWindow;
		readonly ITreeViewService treeViewService;
		readonly Lazy<IAppSettingsTabContainer, IAppSettingsTabContainerMetadata>[] appSettingsTabContainers;
		readonly Lazy<IAppSettingsTabProvider>[] appSettingsTabProviders;
		readonly Lazy<IAppSettingsModifiedListener, IAppSettingsModifiedListenerMetadata>[] appSettingsModifiedListeners;
		Guid? lastSelectedGuid;
		bool showingDialog;

		[ImportingConstructor]
		AppSettingsService(IAppWindow appWindow, ITreeViewService treeViewService, [ImportMany] IEnumerable<Lazy<IAppSettingsTabContainer, IAppSettingsTabContainerMetadata>> appSettingsTabContainers, [ImportMany] IEnumerable<Lazy<IAppSettingsTabProvider>> appSettingsTabProviders, [ImportMany] IEnumerable<Lazy<IAppSettingsModifiedListener, IAppSettingsModifiedListenerMetadata>> appSettingsModifiedListeners) {
			this.appWindow = appWindow;
			this.treeViewService = treeViewService;
			this.appSettingsTabContainers = appSettingsTabContainers.OrderBy(a => a.Metadata.Order).ToArray();
			this.appSettingsTabProviders = appSettingsTabProviders.ToArray();
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
				showingDialog = false;
			}
		}

		void ShowCore(Guid? guid, Window owner) {
			var allVMs = CreateSettingsTabs();
			Debug.Assert(allVMs.Any(a => a.AppSettingsTab.Guid == rootGuid));
			var rootVM = CreateRootVM(allVMs);
			if (rootVM.Children.Count == 0)
				return;

			var treeView = CreateTreeView(rootVM);

			if (guid == null)
				guid = lastSelectedGuid;
			var selectedItem = (guid != null ? allVMs.FirstOrDefault(a => a.AppSettingsTab.Guid == guid.Value) : null) ?? rootVM.Children.FirstOrDefault();
			if (selectedItem != null)
				treeView.SelectItems(new[] { selectedItem });

			var dlg = new AppSettingsDlg();
			dlg.treeViewContentPresenter.Content = treeView.UIObject;
			dlg.Owner = owner ?? appWindow.MainWindow;
			bool saveSettings = dlg.ShowDialog() == true;
			lastSelectedGuid = (treeView.SelectedItem as AppSettingsTabVM)?.AppSettingsTab.Guid;
			Debug.Assert(lastSelectedGuid != null);

			var appRefreshSettings = new AppRefreshSettings();
			foreach (var vm in allVMs)
				vm.AppSettingsTab.OnClosed(saveSettings, appRefreshSettings);
			if (saveSettings) {
				foreach (var listener in appSettingsModifiedListeners)
					listener.Value.OnSettingsModified(appRefreshSettings);
			}
		}

		ITreeView CreateTreeView(AppSettingsTabVM rootVM) {
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

		AppSettingsTabVM CreateRootVM(AppSettingsTabVM[] allVMs) {
			var rootVM = InitializeChildren(allVMs);
			RemoveEmptyNodes(rootVM);
			SortChildren(rootVM);
			return rootVM;
		}

		void RemoveEmptyNodes(AppSettingsTabVM vm) {
			for (int i = vm.Children.Count - 1; i >= 0; i--) {
				var child = vm.Children[i];
				RemoveEmptyNodes(child);
				if (child.Children.Count == 0 && child.AppSettingsTab is AppSettingsTabContainer)
					vm.Children.RemoveAt(i);
			}
		}

		void SortChildren(AppSettingsTabVM vm) {
			vm.Children.Sort(AppSettingsTabVMSorter.Instance);
			foreach (var child in vm.Children)
				SortChildren(child);
		}

		sealed class AppSettingsTabVMSorter : IComparer<AppSettingsTabVM> {
			public static readonly AppSettingsTabVMSorter Instance = new AppSettingsTabVMSorter();
			public int Compare(AppSettingsTabVM x, AppSettingsTabVM y) => x.Order.CompareTo(y.Order);
		}

		AppSettingsTabVM InitializeChildren(AppSettingsTabVM[] vms) {
			var dict = new Dictionary<Guid, AppSettingsTabVM>(vms.Length);
			foreach (var vm in vms) {
				Debug.Assert(!dict.ContainsKey(vm.AppSettingsTab.Guid));
				dict.Add(vm.AppSettingsTab.Guid, vm);
			}

			foreach (var vm in vms) {
				if (vm.AppSettingsTab.Guid == rootGuid)
					continue;

				AppSettingsTabVM parentVM;
				if (!dict.TryGetValue(vm.AppSettingsTab.ParentGuid, out parentVM)) {
					Debug.Fail($"No parent with Guid {vm.AppSettingsTab.ParentGuid}");
					continue;
				}

				parentVM.Children.Add(vm);
			}

			return vms.First(a => a.AppSettingsTab.Guid == rootGuid);
		}

		AppSettingsTabVM[] CreateSettingsTabs() {
			var dict = new Dictionary<Guid, AppSettingsTabVM>();

			dict.Add(rootGuid, new AppSettingsTabVM(new AppSettingsTabContainer(string.Empty, 0, rootGuid, rootGuid, ImageReference.None)));

			foreach (var lz in appSettingsTabContainers) {
				var vm = TryCreate(lz.Value, lz.Metadata);
				if (vm == null)
					continue;
				Debug.Assert(!dict.ContainsKey(vm.AppSettingsTab.Guid));
				if (!dict.ContainsKey(vm.AppSettingsTab.Guid))
					dict.Add(vm.AppSettingsTab.Guid, vm);
			}

			foreach (var lz in appSettingsTabProviders) {
				foreach (var tab in lz.Value.Create()) {
					Debug.Assert(tab != null);
					if (tab == null)
						continue;
					var vm = new AppSettingsTabVM(tab);
					Debug.Assert(!dict.ContainsKey(vm.AppSettingsTab.Guid));
					if (!dict.ContainsKey(vm.AppSettingsTab.Guid))
						dict.Add(vm.AppSettingsTab.Guid, vm);
				}
			}

			return dict.Values.ToArray();
		}

		sealed class AppSettingsTabContainer : IAppSettingsTab {
			public Guid ParentGuid { get; }
			public Guid Guid { get; }
			public double Order { get; }
			public string Title { get; }
			public ImageReference Icon { get; }
			public object UIObject { get; }

			public AppSettingsTabContainer(string title, double order, Guid guid, Guid parentGuid, ImageReference icon) {
				Title = title;
				Order = order;
				Guid = guid;
				ParentGuid = parentGuid;
				Icon = icon;
			}

			void IAppSettingsTab.OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) { }
		}

		static AppSettingsTabVM TryCreate(object obj, IAppSettingsTabContainerMetadata md) {
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
			return new AppSettingsTabVM(new AppSettingsTabContainer(title, md.Order, guid.Value, parentGuid.Value, icon));
		}

		static Guid? TryParseGuid(string guidString) {
			Guid guid;
			if (Guid.TryParse(guidString, out guid))
				return guid;
			return null;
		}
	}
}
