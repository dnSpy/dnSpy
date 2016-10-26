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
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Tabs;
using dnSpy.Tabs;

namespace dnSpy.Documents.Tabs {
	sealed class TabContentImpl : ViewModelBase, ITabContent, IDocumentTab, IFocusable {
		readonly TabHistory tabHistory;

		public IDocumentTabService DocumentTabService => documentTabService;
		readonly DocumentTabService documentTabService;

		public bool IsActiveTab => DocumentTabService.ActiveTab == this;

		public DocumentTabContent Content {
			get { return tabHistory.Current; }
			private set {
				bool saveCurrent = !(tabHistory.Current is NullDocumentTabContent);
				tabHistory.SetCurrent(value, saveCurrent);
			}
		}

		public DocumentTabUIContext UIContext {
			get { return uiContext; }
			private set {
				uiContextVersion++;
				var newValue = value;
				Debug.Assert(newValue != null);
				if (newValue == null)
					newValue = new NullDocumentTabUIContext();
				if (uiContext != newValue) {
					uiContext.OnHide();
					elementZoomer.InstallZoom(newValue, newValue.ZoomElement);
					newValue.OnShow();
					uiContext = newValue;
					UIObject = uiContext.UIObject;
				}
			}
		}
		DocumentTabUIContext uiContext;
		int uiContextVersion;

		public string Title {
			get { return title; }
			set {
				if (title != value) {
					title = value;
					OnPropertyChanged(nameof(Title));
				}
			}
		}
		string title;

		public object ToolTip {
			get { return toolTip; }
			set {
				if (!object.Equals(toolTip, value)) {
					toolTip = value;
					OnPropertyChanged(nameof(ToolTip));
				}
			}
		}
		object toolTip;

		public object UIObject {
			get { return uiObject; }
			set {
				if (uiObject != value) {
					uiObject = value;
					OnPropertyChanged(nameof(UIObject));
				}
			}
		}
		object uiObject;

		IInputElement ITabContent.FocusedElement => UIContext?.FocusedElement;

		bool IFocusable.CanFocus => (UIContext as IFocusable)?.CanFocus == true;

		void IFocusable.Focus() {
			var focusable = UIContext as IFocusable;
			Debug.Assert(focusable != null);
			if (focusable != null)
				focusable.Focus();
		}

		readonly IDocumentTabUIContextLocator documentTabUIContextLocator;
		readonly Lazy<IReferenceDocumentTabContentProvider, IReferenceDocumentTabContentProviderMetadata>[] referenceDocumentTabContentProviders;
		readonly Lazy<IDefaultDocumentTabContentProvider, IDefaultDocumentTabContentProviderMetadata>[] defaultDocumentTabContentProviders;
		readonly TabElementZoomer elementZoomer;

		public TabContentImpl(DocumentTabService documentTabService, IDocumentTabUIContextLocator documentTabUIContextLocator, Lazy<IReferenceDocumentTabContentProvider, IReferenceDocumentTabContentProviderMetadata>[] referenceDocumentTabContentProviders, Lazy<IDefaultDocumentTabContentProvider, IDefaultDocumentTabContentProviderMetadata>[] defaultDocumentTabContentProviders) {
			this.elementZoomer = new TabElementZoomer();
			this.tabHistory = new TabHistory();
			this.tabHistory.SetCurrent(new NullDocumentTabContent(), false);
			this.documentTabService = documentTabService;
			this.documentTabUIContextLocator = documentTabUIContextLocator;
			this.referenceDocumentTabContentProviders = referenceDocumentTabContentProviders;
			this.defaultDocumentTabContentProviders = defaultDocumentTabContentProviders;
			this.uiContext = new NullDocumentTabUIContext();
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
				elementZoomer.Dispose();
				var id = documentTabUIContextLocator as IDisposable;
				Debug.Assert(id != null);
				if (id != null)
					id.Dispose();
				documentTabService.OnRemoved(this);
			}
		}

		public void FollowReference(object @ref, DocumentTabContent sourceContent, Action<ShowTabContentEventArgs> onShown) {
			var result = TryCreateContentFromReference(@ref, sourceContent);
			if (result != null) {
				Show(result.DocumentTabContent, result.UIState, e => {
					// Call the original caller (onShown()) first and result last since both could
					// move the caret. The result should only move the caret if the original caller
					// hasn't moved the caret.
					onShown?.Invoke(e);
					result.OnShownHandler?.Invoke(e);
				});
			}
			else {
				var defaultContent = TryCreateDefaultContent();
				if (defaultContent != null) {
					Show(defaultContent, null, e => {
						onShown?.Invoke(new ShowTabContentEventArgs(false, this));
					});
				}
				else
					onShown?.Invoke(new ShowTabContentEventArgs(false, this));
			}
		}

		public void FollowReferenceNewTab(object @ref, Action<ShowTabContentEventArgs> onShown) {
			var tab = DocumentTabService.OpenEmptyTab();
			tab.FollowReference(@ref, Content, onShown);
			DocumentTabService.SetFocus(tab);
		}

		public void FollowReference(object @ref, bool newTab, Action<ShowTabContentEventArgs> onShown) {
			if (newTab)
				FollowReferenceNewTab(@ref, onShown);
			else
				FollowReference(@ref, Content, onShown);
		}

		DocumentTabReferenceResult TryCreateContentFromReference(object @ref, DocumentTabContent sourceContent) {
			foreach (var f in referenceDocumentTabContentProviders) {
				var c = f.Value.Create(DocumentTabService, sourceContent, @ref);
				if (c != null)
					return c;
			}
			return null;
		}

		DocumentTabContent TryCreateDefaultContent() {
			foreach (var f in defaultDocumentTabContentProviders) {
				var c = f.Value.Create(DocumentTabService);
				if (c != null)
					return c;
			}
			return null;
		}

		public void Show(DocumentTabContent tabContent, object uiState, Action<ShowTabContentEventArgs> onShown) {
			if (tabContent == null)
				throw new ArgumentNullException(nameof(tabContent));
			Debug.Assert(tabContent.DocumentTab == null || tabContent.DocumentTab == this);
			HideCurrentContent();
			Content = tabContent;
			ShowInternal(tabContent, uiState, onShown, false);
		}

		void HideCurrentContent() {
			CancelAsyncWorker();
			Content?.OnHide();
		}

		sealed class ShowContext : IShowContext {
			public DocumentTabUIContext UIContext { get; }
			public bool IsRefresh { get; }
			public object Tag { get; set; }
			public Action<ShowTabContentEventArgs> OnShown { get; set; }
			public ShowContext(DocumentTabUIContext uiCtx, bool isRefresh) {
				this.UIContext = uiCtx;
				this.IsRefresh = isRefresh;
			}
		}

		sealed class AsyncShowContext : IAsyncShowContext {
			public DocumentTabUIContext UIContext => showContext.UIContext;
			public bool IsRefresh => showContext.IsRefresh;
			public object Tag { get { return showContext.Tag; } set { showContext.Tag = value; } }
			public Action<ShowTabContentEventArgs> OnShown { get { return showContext.OnShown; } set { showContext.OnShown = value; } }
			public CancellationToken CancellationToken => asyncWorkerContext.CancellationToken;

			readonly IShowContext showContext;
			readonly AsyncWorkerContext asyncWorkerContext;

			public AsyncShowContext(IShowContext showContext, AsyncWorkerContext asyncWorkerContext) {
				this.showContext = showContext;
				this.asyncWorkerContext = asyncWorkerContext;
			}

			public void Cancel() => asyncWorkerContext.Cancel();
		}

		void ShowInternal(DocumentTabContent tabContent, object uiState, Action<ShowTabContentEventArgs> onShownHandler, bool isRefresh) {
			Debug.Assert(asyncWorkerContext == null);
			UIContext = tabContent.CreateUIContext(documentTabUIContextLocator);
			var cachedUIContext = UIContext;
			Debug.Assert(cachedUIContext.DocumentTab == null || cachedUIContext.DocumentTab == this);
			cachedUIContext.DocumentTab = this;
			Debug.Assert(cachedUIContext.DocumentTab == this);
			Debug.Assert(tabContent.DocumentTab == null || tabContent.DocumentTab == this);
			tabContent.DocumentTab = this;
			Debug.Assert(tabContent.DocumentTab == this);

			UpdateTitleAndToolTip();
			var showCtx = new ShowContext(cachedUIContext, isRefresh);
			tabContent.OnShow(showCtx);
			bool asyncShow = false;
			var asyncTabContent = tabContent as AsyncDocumentTabContent;
			if (asyncTabContent != null) {
				if (asyncTabContent.NeedAsyncWork(showCtx)) {
					asyncShow = true;
					var ctx = new AsyncWorkerContext();
					asyncWorkerContext = ctx;
					var asyncShowCtx = new AsyncShowContext(showCtx, ctx);
					Task.Run(() => asyncTabContent.CreateContentAsync(asyncShowCtx), ctx.CancellationToken)
					.ContinueWith(t => {
						bool canShowAsyncOutput = ctx == asyncWorkerContext &&
												cachedUIContext.DocumentTab == this &&
												UIContext == cachedUIContext;
						if (asyncWorkerContext == ctx)
							asyncWorkerContext = null;
						ctx.Dispose();
						asyncTabContent.OnShowAsync(showCtx, new AsyncShowResult(t, canShowAsyncOutput));
						bool success = !t.IsFaulted && !t.IsCanceled;
						OnShown(uiState, onShownHandler, showCtx, success);
					}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
				}
				else
					asyncTabContent.OnShowAsync(showCtx, new AsyncShowResult());
			}
			if (!asyncShow)
				OnShown(uiState, onShownHandler, showCtx, true);
			documentTabService.OnNewTabContentShown(this);
		}

		sealed class AsyncWorkerContext : IDisposable {
			public readonly CancellationTokenSource CancellationTokenSource;
			public readonly CancellationToken CancellationToken;
			bool disposed;

			public AsyncWorkerContext() {
				this.CancellationTokenSource = new CancellationTokenSource();
				this.CancellationToken = CancellationTokenSource.Token;
			}

			public void Cancel() {
				if (!disposed)
					CancellationTokenSource.Cancel();
			}

			public void Dispose() {
				disposed = true;
				CancellationTokenSource.Dispose();
			}
		}
		AsyncWorkerContext asyncWorkerContext;

		public bool IsAsyncExecInProgress => asyncWorkerContext != null;

		public void AsyncExec(Action<CancellationTokenSource> preExec, Action asyncAction, Action<IAsyncShowResult> postExec) {
			CancelAsyncWorker();

			var ctx = new AsyncWorkerContext();
			asyncWorkerContext = ctx;
			preExec(ctx.CancellationTokenSource);
			Task.Factory.StartNew(() => asyncAction(), ctx.CancellationToken)
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
			asyncWorkerContext.Cancel();
			asyncWorkerContext = null;
		}

		void OnShown(object uiState, Action<ShowTabContentEventArgs> onShownHandler, IShowContext showCtx, bool success) {
			if (uiState != null)
				RestoreUIState(uiState);
			if (onShownHandler != null || showCtx.OnShown != null) {
				var e = new ShowTabContentEventArgs(success, this);
				onShownHandler?.Invoke(e);
				showCtx.OnShown?.Invoke(e);
			}
		}

		void RestoreUIState(object uiState) {
			if (uiState == null)
				return;
			UIContext.RestoreUIState(uiState);
			var uiel = UIContext.FocusedElement as UIElement ?? UIContext.UIObject as UIElement;
			if (uiel == null || uiel.IsVisible)
				return;
			int uiContextVersionTmp = uiContextVersion;
			new OnVisibleHelper(uiel, () => {
				if (uiContextVersionTmp == uiContextVersion)
					UIContext.RestoreUIState(uiState);
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

		public bool CanNavigateBackward => tabHistory.CanNavigateBackward;
		public bool CanNavigateForward => tabHistory.CanNavigateForward;

		public void NavigateBackward() {
			if (!CanNavigateBackward)
				return;
			HideCurrentContent();
			var uiState = tabHistory.NavigateBackward();
			ShowInternal(tabHistory.Current, uiState, null, false);
		}

		public void NavigateForward() {
			if (!CanNavigateForward)
				return;
			HideCurrentContent();
			var uiState = tabHistory.NavigateForward();
			ShowInternal(tabHistory.Current, uiState, null, false);
		}

		public void Refresh() {
			// Pretend it gets hidden and then shown again. Will also cancel any async output threads
			HideCurrentContent();
			ShowInternal(Content, UIContext.CreateUIState(), null, true);
		}

		public void TrySetFocus() {
			if (IsActiveTab)
				DocumentTabService.SetFocus(this);
		}

		public void Close() => DocumentTabService.Close(this);
		public void OnSelected() => Content.OnSelected();
		public void OnUnselected() => Content.OnUnselected();

		internal void OnTabsLoaded() {
			// Make sure that the tab initializes eg. Language to the language it's using.
			OnSelected();
		}

		const string ZOOM_ATTR = "zoom";

		public void DeserializeUI(ISettingsSection tabContentUI) {
			double? zoom = tabContentUI.Attribute<double?>(ZOOM_ATTR);
			elementZoomer.ZoomValue = zoom ?? 1.0;
		}

		public void SerializeUI(ISettingsSection tabContentUI) =>
			tabContentUI.Attribute(ZOOM_ATTR, elementZoomer.ZoomValue);

		public void OnNodesRemoved(HashSet<DsDocumentNode> removedDocuments, Func<DocumentTabContent> createEmptyContent) {
			tabHistory.RemoveFromBackwardList(a => CheckRemove(a, removedDocuments));
			tabHistory.RemoveFromForwardList(a => CheckRemove(a, removedDocuments));
			if (CheckRemove(tabHistory.Current, removedDocuments)) {
				tabHistory.OverwriteCurrent(createEmptyContent());
				Refresh();
			}
		}

		bool CheckRemove(DocumentTabContent content, HashSet<DsDocumentNode> removedDocuments) =>
			content.Nodes.Any(a => removedDocuments.Contains(a.GetDocumentNode()));
	}
}
