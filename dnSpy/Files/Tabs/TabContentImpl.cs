/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Diagnostics;
using System.Windows;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Tabs;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Files.Tabs {
	sealed class TabContentImpl : ViewModelBase, ITabContent, IFileTab {
		readonly TabHistory tabHistory;

		public IFileTabManager FileTabManager {
			get { return fileTabManager; }
		}
		readonly FileTabManager fileTabManager;

		public IFileTabContent FileTabContent {
			get { return tabHistory.Current; }
			set {
				bool saveCurrent = !(tabHistory.Current is NullFileTabContent);
				tabHistory.SetCurrent(value, saveCurrent);
			}
		}

		public IFileTabUIContext UIContext {
			get { return uiContext; }
			set {
				uiContext = value;
				UIObject = uiContext.UIObject;
			}
		}
		IFileTabUIContext uiContext;

		public string Title {
			get { return title; }
			set {
				if (title != value) {
					title = value;
					OnPropertyChanged("Title");
				}
			}
		}
		string title;

		public object ToolTip {
			get { return toolTip; }
			set {
				if (toolTip != value) {
					toolTip = value;
					OnPropertyChanged("ToolTip");
				}
			}
		}
		object toolTip;

		public object UIObject {
			get { return uiObject; }
			set {
				if (uiObject != value) {
					uiObject = value;
					OnPropertyChanged("UIObject");
				}
			}
		}
		object uiObject;

		UIElement ITabContent.FocusedElement {
			get {
				if (UIContext != null)
					return UIContext.FocusedElement;
				return null;
			}
		}

		readonly IFileTabUIContextLocator fileTabUIContextLocator;
		readonly Lazy<IReferenceFileTabContentCreator, IReferenceFileTabContentCreatorMetadata>[] refFactories;

		public TabContentImpl(FileTabManager fileTabManager, IFileTabUIContextLocator fileTabUIContextLocator, Lazy<IReferenceFileTabContentCreator, IReferenceFileTabContentCreatorMetadata>[] refFactories) {
			this.tabHistory = new TabHistory();
			this.tabHistory.SetCurrent(new NullFileTabContent(), false);
			this.fileTabManager = fileTabManager;
			this.fileTabUIContextLocator = fileTabUIContextLocator;
			this.refFactories = refFactories;
			this.UIContext = new NullFileTabUIContext();
		}

		void ITabContent.OnVisibilityChanged(TabContentVisibilityEvent visEvent) {
		}

		public void FollowReference(object @ref) {
			var tabContent = TryCreateContentFromReference(@ref);
			if (tabContent != null)
				Show(tabContent);
		}

		IFileTabContent TryCreateContentFromReference(object @ref) {
			foreach (var f in refFactories) {
				var c = f.Value.Create(FileTabManager, @ref);
				if (c != null)
					return c;
			}
			return null;
		}

		public void Show(IFileTabContent tabContent) {
			if (tabContent == null)
				throw new ArgumentNullException();
			Debug.Assert(tabContent.FileTab == null || tabContent.FileTab == this);
			HideCurrentContent();
			FileTabContent = tabContent;
			ShowInternal(tabContent);
		}

		void HideCurrentContent() {
			//TODO: cancel any async workers
			if (FileTabContent != null)
				FileTabContent.OnHide();
			UIContext.Clear();
		}

		void ShowInternal(IFileTabContent tabContent) {
			UIContext = tabContent.CreateUIContext(fileTabUIContextLocator);
			Debug.Assert(UIContext != null);
			if (UIContext == null)
				UIContext = new NullFileTabUIContext();
			UIContext.FileTab = this;
			tabContent.FileTab = this;

			UpdateTitleAndToolTip();
			tabContent.OnShow(UIContext);
			var asyncTabContent = tabContent as IAsyncFileTabContent;
			if (asyncTabContent != null) {
				if (asyncTabContent.CanStartAsyncWorker(UIContext)) {
					//TODO: Call this in a worker thread
					asyncTabContent.AsyncWorker(UIContext);
					asyncTabContent.EndAsyncShow(UIContext);
				}
				else
					asyncTabContent.EndAsyncShow(UIContext);
			}
			fileTabManager.OnNewTabContentShown(this);
		}

		internal void UpdateTitleAndToolTip() {
			Title = FileTabContent.Title;
			ToolTip = FileTabContent.ToolTip;
		}

		public bool CanNavigateBackward {
			get { return tabHistory.CanNavigateBackward; }
		}

		public bool CanNavigateForward {
			get { return tabHistory.CanNavigateForward; }
		}

		public void NavigateBackward() {
			if (!CanNavigateBackward)
				return;
			HideCurrentContent();
			tabHistory.NavigateBackward();
			ShowInternal(tabHistory.Current);
		}

		public void NavigateForward() {
			if (!CanNavigateForward)
				return;
			HideCurrentContent();
			tabHistory.NavigateForward();
			ShowInternal(tabHistory.Current);
		}
	}
}
