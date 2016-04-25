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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Tabs;
using dnSpy.Shared.MVVM;
using dnSpy.Tabs;

namespace dnSpy.Files.Tabs {
	sealed class TabContentImpl : ViewModelBase, ITabContent, IFileTab, IFocusable {
		readonly TabHistory tabHistory;

		public IFileTabManager FileTabManager {
			get { return fileTabManager; }
		}
		readonly FileTabManager fileTabManager;

		public bool IsActiveTab {
			get { return FileTabManager.ActiveTab == this; }
		}

		public IFileTabContent Content {
			get { return tabHistory.Current; }
			private set {
				bool saveCurrent = !(tabHistory.Current is NullFileTabContent);
				tabHistory.SetCurrent(value, saveCurrent);
			}
		}

		public IFileTabUIContext UIContext {
			get { return uiContext; }
			private set {
				uiContextVersion++;
				var newValue = value;
				Debug.Assert(newValue != null);
				if (newValue == null)
					newValue = new NullFileTabUIContext();
				if (uiContext != newValue) {
					uiContext.OnHide();
					elementScaler.InstallScale(newValue.ScaleElement);
					newValue.OnShow();
					uiContext = newValue;
					UIObject = uiContext.UIObject;
				}
			}
		}
		IFileTabUIContext uiContext;
		int uiContextVersion;

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

		IInputElement ITabContent.FocusedElement {
			get {
				if (UIContext != null)
					return UIContext.FocusedElement;
				return null;
			}
		}

		bool IFocusable.CanFocus {
			get {
				var focusable = UIContext as IFocusable;
				return focusable != null && focusable.CanFocus;
			}
		}

		void IFocusable.Focus() {
			var focusable = UIContext as IFocusable;
			Debug.Assert(focusable != null);
			if (focusable != null)
				focusable.Focus();
		}

		readonly IFileTabUIContextLocator fileTabUIContextLocator;
		readonly Lazy<IReferenceFileTabContentCreator, IReferenceFileTabContentCreatorMetadata>[] refFactories;
		readonly TabElementScaler elementScaler;

		public TabContentImpl(FileTabManager fileTabManager, IFileTabUIContextLocator fileTabUIContextLocator, Lazy<IReferenceFileTabContentCreator, IReferenceFileTabContentCreatorMetadata>[] refFactories) {
			this.elementScaler = new TabElementScaler();
			this.tabHistory = new TabHistory();
			this.tabHistory.SetCurrent(new NullFileTabContent(), false);
			this.fileTabManager = fileTabManager;
			this.fileTabUIContextLocator = fileTabUIContextLocator;
			this.refFactories = refFactories;
			this.uiContext = new NullFileTabUIContext();
			this.uiObject = this.uiContext.UIObject;
		}

#if DEBUG
		bool _added, _visible;
#endif
		void ITabContent.OnVisibilityChanged(TabContentVisibilityEvent visEvent) {
#if DEBUG
			switch (visEvent) {
			case TabContentVisibilityEvent.Added:
				Debug.Assert(!_added);
				Debug.Assert(!_visible);
				_added = true;
				break;
			case TabContentVisibilityEvent.Removed:
				Debug.Assert(_added);
				Debug.Assert(!_visible);
				_added = false;
				break;
			case TabContentVisibilityEvent.Visible:
				Debug.Assert(_added);
				Debug.Assert(!_visible);
				_visible = true;
				break;
			case TabContentVisibilityEvent.Hidden:
				Debug.Assert(_added);
				Debug.Assert(_visible);
				_visible = false;
				break;
			}
#endif

			if (visEvent == TabContentVisibilityEvent.Removed) {
				CancelAsyncWorker();
				elementScaler.Dispose();
				var id = fileTabUIContextLocator as IDisposable;
				Debug.Assert(id != null);
				if (id != null)
					id.Dispose();
				fileTabManager.OnRemoved(this);
			}
		}

		public void FollowReference(object @ref, IFileTabContent sourceContent, Action<ShowTabContentEventArgs> onShown) {
			var result = TryCreateContentFromReference(@ref, sourceContent);
			if (result != null) {
				Show(result.FileTabContent, result.SerializedUI, e => {
					// Call the original caller (onShown()) first and result last since both could
					// move the caret. The result should only move the caret if the original caller
					// hasn't moved the caret.
					if (onShown != null)
						onShown(e);
					if (result.OnShownHandler != null)
						result.OnShownHandler(e);
				});
			}
		}

		public void FollowReferenceNewTab(object @ref, Action<ShowTabContentEventArgs> onShown) {
			var tab = FileTabManager.OpenEmptyTab();
			tab.FollowReference(@ref, Content, onShown);
			FileTabManager.SetFocus(tab);
		}

		public void FollowReference(object @ref, bool newTab, Action<ShowTabContentEventArgs> onShown) {
			if (newTab)
				FollowReferenceNewTab(@ref, onShown);
			else
				FollowReference(@ref, Content, onShown);
		}

		FileTabReferenceResult TryCreateContentFromReference(object @ref, IFileTabContent sourceContent) {
			foreach (var f in refFactories) {
				var c = f.Value.Create(FileTabManager, sourceContent, @ref);
				if (c != null)
					return c;
			}
			return null;
		}

		public void Show(IFileTabContent tabContent, object serializedUI, Action<ShowTabContentEventArgs> onShown) {
			if (tabContent == null)
				throw new ArgumentNullException();
			Debug.Assert(tabContent.FileTab == null || tabContent.FileTab == this);
			HideCurrentContent();
			Content = tabContent;
			ShowInternal(tabContent, serializedUI, onShown, false);
		}

		void HideCurrentContent() {
			CancelAsyncWorker();
			if (Content != null)
				Content.OnHide();
		}

		sealed class ShowContext : IShowContext {
			public IFileTabUIContext UIContext { get; private set; }
			public bool IsRefresh { get; private set; }
			public object UserData { get; set; }
			public Action<ShowTabContentEventArgs> OnShown { get; set; }
			public ShowContext(IFileTabUIContext uiCtx, bool isRefresh) {
				this.UIContext = uiCtx;
				this.IsRefresh = isRefresh;
			}
		}

		void ShowInternal(IFileTabContent tabContent, object serializedUI, Action<ShowTabContentEventArgs> onShownHandler, bool isRefresh) {
			Debug.Assert(asyncWorkerContext == null);
			var oldUIContext = UIContext;
			UIContext = tabContent.CreateUIContext(fileTabUIContextLocator);
			var cachedUIContext = UIContext;
			Debug.Assert(cachedUIContext.FileTab == null || cachedUIContext.FileTab == this);
			cachedUIContext.FileTab = this;
			Debug.Assert(cachedUIContext.FileTab == this);
			Debug.Assert(tabContent.FileTab == null || tabContent.FileTab == this);
			tabContent.FileTab = this;
			Debug.Assert(tabContent.FileTab == this);

			UpdateTitleAndToolTip();
			var showCtx = new ShowContext(cachedUIContext, isRefresh);
			tabContent.OnShow(showCtx);
			bool asyncShow = false;
			var asyncTabContent = tabContent as IAsyncFileTabContent;
			if (asyncTabContent != null) {
				if (asyncTabContent.CanStartAsyncWorker(showCtx)) {
					asyncShow = true;
					var ctx = new AsyncWorkerContext();
					asyncWorkerContext = ctx;
					Task.Factory.StartNew(() => asyncTabContent.AsyncWorker(showCtx, ctx.CancellationTokenSource), ctx.CancellationTokenSource.Token)
					.ContinueWith(t => {
						bool canShowAsyncOutput = ctx == asyncWorkerContext &&
												cachedUIContext.FileTab == this &&
												UIContext == cachedUIContext;
						if (asyncWorkerContext == ctx)
							asyncWorkerContext = null;
						ctx.Dispose();
						asyncTabContent.EndAsyncShow(showCtx, new AsyncShowResult(t, canShowAsyncOutput));
						bool success = !t.IsFaulted && !t.IsCanceled;
						OnShown(serializedUI, onShownHandler, showCtx, success);
					}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
				}
				else
					asyncTabContent.EndAsyncShow(showCtx, new AsyncShowResult());
			}
			if (!asyncShow)
				OnShown(serializedUI, onShownHandler, showCtx, true);
			fileTabManager.OnNewTabContentShown(this);
		}

		sealed class AsyncWorkerContext : IDisposable {
			public readonly CancellationTokenSource CancellationTokenSource;

			public AsyncWorkerContext() {
				this.CancellationTokenSource = new CancellationTokenSource();
			}

			public void Dispose() {
				this.CancellationTokenSource.Dispose();
			}
		}
		AsyncWorkerContext asyncWorkerContext;

		public bool IsAsyncExecInProgress {
			get { return asyncWorkerContext != null; }
		}

		public void AsyncExec(Action<CancellationTokenSource> preExec, Action asyncAction, Action<IAsyncShowResult> postExec) {
			CancelAsyncWorker();

			var ctx = new AsyncWorkerContext();
			asyncWorkerContext = ctx;
			preExec(ctx.CancellationTokenSource);
			Task.Factory.StartNew(() => asyncAction(), ctx.CancellationTokenSource.Token)
			.ContinueWith(t => {
				if (asyncWorkerContext == ctx)
					asyncWorkerContext = null;
				ctx.Dispose();
				postExec(new AsyncShowResult(t, false));
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		void CancelAsyncWorker() {
			if (asyncWorkerContext == null)
				return;
			asyncWorkerContext.CancellationTokenSource.Cancel();
			asyncWorkerContext = null;
		}

		void OnShown(object serializedUI, Action<ShowTabContentEventArgs> onShownHandler, IShowContext showCtx, bool success) {
			if (serializedUI != null)
				Deserialize(serializedUI);
			if (onShownHandler != null || showCtx.OnShown != null) {
				var e = new ShowTabContentEventArgs(success, this);
				if (onShownHandler != null)
					onShownHandler(e);
				if (showCtx.OnShown != null)
					showCtx.OnShown(e);
			}
		}

		void Deserialize(object serializedUI) {
			if (serializedUI == null)
				return;
			UIContext.Deserialize(serializedUI);
			var uiel = UIContext.FocusedElement as UIElement ?? UIContext.UIObject as UIElement;
			if (uiel == null || uiel.IsVisible)
				return;
			int uiContextVersionTmp = uiContextVersion;
			new OnVisibleHelper(uiel, () => {
				if (uiContextVersionTmp == uiContextVersion)
					UIContext.Deserialize(serializedUI);
			});
		}

		sealed class OnVisibleHelper {
			readonly UIElement uiel;
			readonly Action exec;

			public OnVisibleHelper(UIElement uiel, Action exec) {
				this.uiel = uiel;
				this.exec = exec;
				this.uiel.IsVisibleChanged += UIElement_IsVisibleChanged;
			}

			void UIElement_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
				this.uiel.IsVisibleChanged -= UIElement_IsVisibleChanged;
				exec();
			}
		}

		internal void UpdateTitleAndToolTip() {
			Title = Content.Title;
			ToolTip = Content.ToolTip;
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
			var serialized = tabHistory.NavigateBackward();
			ShowInternal(tabHistory.Current, serialized, null, false);
		}

		public void NavigateForward() {
			if (!CanNavigateForward)
				return;
			HideCurrentContent();
			var serialized = tabHistory.NavigateForward();
			ShowInternal(tabHistory.Current, serialized, null, false);
		}

		public void Refresh() {
			// Pretend it gets hidden and then shown again. Will also cancel any async output threads
			HideCurrentContent();
			ShowInternal(Content, UIContext.Serialize(), null, true);
		}

		public void TrySetFocus() {
			if (IsActiveTab)
				FileTabManager.SetFocus(this);
		}

		public void Close() {
			FileTabManager.Close(this);
		}

		public void OnSelected() {
			Content.OnSelected();
		}

		public void OnUnselected() {
			Content.OnUnselected();
		}

		internal void OnTabsLoaded() {
			// Make sure that the tab initializes eg. Language to the language it's using.
			OnSelected();
		}

		const string SCALE_ATTR = "scale";

		public void DeserializeUI(ISettingsSection tabContentUI) {
			double? scale = tabContentUI.Attribute<double?>(SCALE_ATTR);
			elementScaler.ScaleValue = scale ?? 1.0;
		}

		public void SerializeUI(ISettingsSection tabContentUI) {
			tabContentUI.Attribute(SCALE_ATTR, elementScaler.ScaleValue);
		}

		public void OnNodesRemoved(HashSet<IDnSpyFileNode> removedFiles, Func<IFileTabContent> createEmptyContent) {
			tabHistory.RemoveFromBackwardList(a => CheckRemove(a, removedFiles));
			tabHistory.RemoveFromForwardList(a => CheckRemove(a, removedFiles));
			if (CheckRemove(tabHistory.Current, removedFiles)) {
				tabHistory.OverwriteCurrent(createEmptyContent());
				Refresh();
			}
		}

		bool CheckRemove(IFileTabContent content, HashSet<IDnSpyFileNode> removedFiles) {
			return content.Nodes.Any(a => removedFiles.Contains(a.GetDnSpyFileNode()));
		}
	}
}
