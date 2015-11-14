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

using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using dnSpy.Contracts;
using dnSpy.Contracts.Menus;
using dnSpy.TreeNodes;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.Tabs {
	public sealed class DecompileTabState : TabState {
		public readonly DecompilerTextView TextView = new DecompilerTextView();
		internal readonly NavigationHistory<NavigationState> History = new NavigationHistory<NavigationState>();
		internal bool ignoreDecompilationRequests;
		internal bool HasDecompiled;

		public override string FileName {
			get {
				var mod = ILSpyTreeNode.GetModule(DecompiledNodes);
				return mod == null ? null : mod.Location;
			}
		}

		public override string Name {
			get {
				var mod = ILSpyTreeNode.GetModule(DecompiledNodes);
				return mod == null ? null : mod.Name;
			}
		}

		public override UIElement FocusedElement {
			get {
				if (IsTextViewInVisualTree) {
					if (TextView.waitAdornerButton.IsVisible)
						return TextView.waitAdornerButton;
					return TextView.TextEditor.TextArea;
				}

				return TabItem.Content as UIElement;
			}
		}

		public override FrameworkElement ScaleElement {
			get {
				if (IsTextViewInVisualTree)
					return TextView.TextEditor.TextArea;
				return null;
			}
		}

		public override TabStateType Type {
			get { return TabStateType.DecompiledCode; }
		}

		public ILSpyTreeNode[] DecompiledNodes {
			get { return decompiledNodes; }
		}
		ILSpyTreeNode[] decompiledNodes = new ILSpyTreeNode[0];

		public bool IsTextViewInVisualTree {
			get { return TabItem.Content == TextView; }
		}

		public string Title {
			get { return title; }
			set {
				if (title != value) {
					title = value;
					UpdateHeader();
				}
			}
		}
		string title;

		public Language Language {
			get { return language; }
		}
		Language language;

		public override string Header {
			get {
				var nodes = DecompiledNodes;
				if (nodes == null || nodes.Length == 0)
					return Title ?? "<empty>";

				if (nodes.Length == 1)
					return nodes[0].ToString(Language);

				var sb = new StringBuilder();
				foreach (var node in nodes) {
					if (sb.Length > 0)
						sb.Append(", ");
					sb.Append(node.ToString(Language));
				}
				return sb.ToString();
			}
		}

		internal void SetDecompileProps(Language language, ILSpyTreeNode[] nodes) {
			this.language = language;
			UnhookEvents();
			this.decompiledNodes = nodes ?? new ILSpyTreeNode[0];
			HookEvents();
			this.title = null;
			UpdateHeader();
		}

		void HookEvents() {
			foreach (var node in decompiledNodes)
				node.PropertyChanged += node_PropertyChanged;
		}

		void UnhookEvents() {
			foreach (var node in decompiledNodes)
				node.PropertyChanged -= node_PropertyChanged;
		}

		void node_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "Text")
				UpdateHeader();
		}

		public static DecompileTabState GetDecompileTabState(DecompilerTextView elem) {
			return (DecompileTabState)GetTabState(elem);
		}

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				var textView = (DecompilerTextView)creatorObject.Object;
				var position = openedFromKeyboard ? textView.TextEditor.TextArea.Caret.Position : textView.GetPositionFromMousePosition();
				if (position != null)
					yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTVIEWPOSITION_GUID, position);

				var @ref = textView.GetReferenceSegmentAt(position);
				if (@ref != null) {
					yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_REFERENCE_GUID, new CodeReferenceSegment(@ref.Reference, @ref.IsLocal, @ref.IsLocalTarget));
					yield return new GuidObject(MenuConstants.GUIDOBJ_REFERENCE_GUID, @ref);
				}
			}
		}

		sealed class ContextMenuInitializer : IContextMenuInitializer {
			public void Initialize(IMenuItemContext context, ContextMenu menu) {
				var textView = (DecompilerTextView)context.CreatorObject.Object;
				if (context.OpenedFromKeyboard) {
					var scrollInfo = (IScrollInfo)textView.TextEditor.TextArea.TextView;
					var pos = textView.TextEditor.TextArea.TextView.GetVisualPosition(textView.TextEditor.TextArea.Caret.Position, VisualYPosition.TextBottom);
					pos = new Point(pos.X - scrollInfo.HorizontalOffset, pos.Y - scrollInfo.VerticalOffset);

					menu.HorizontalOffset = pos.X;
					menu.VerticalOffset = pos.Y;
					ContextMenuService.SetPlacement(textView, PlacementMode.Relative);
					ContextMenuService.SetPlacementTarget(textView, textView.TextEditor.TextArea.TextView);
					menu.Closed += (s, e2) => {
						textView.ClearValue(ContextMenuService.PlacementProperty);
						textView.ClearValue(ContextMenuService.PlacementTargetProperty);
					};
				}
				else {
					textView.ClearValue(ContextMenuService.PlacementProperty);
					textView.ClearValue(ContextMenuService.PlacementTargetProperty);
				}
			}
		}

		public DecompileTabState(Language language) {
			var view = TextView;
			view.Tag = this;
			this.language = language;
			DnSpy.App.MenuManager.InitializeContextMenu(view, MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID, new GuidObjectsCreator(), new ContextMenuInitializer());
			view.DragOver += view_DragOver;
			view.OnThemeUpdated();
			InstallMouseWheelZoomHandler(TextView.TextEditor.TextArea);
		}

		void view_DragOver(object sender, DragEventArgs e) {
			// The text editor seems to allow anything
			if (e.Data.GetDataPresent(typeof(TabItem))) {
				e.Effects = DragDropEffects.None;
				e.Handled = true;
				return;
			}
		}

		public override void FocusContent() {
			if (this.TextView == TabItem.Content)
				this.TextView.TextEditor.TextArea.Focus();
			else
				base.FocusContent();
		}

		protected override void Dispose(bool isDisposing) {
			if (isDisposing)
				TextView.Dispose();
			UnhookEvents();
			decompiledNodes = new ILSpyTreeNode[0];
		}

		public bool Equals(ILSpyTreeNode[] nodes, Language language) {
			if (Language != language)
				return false;
			if (DecompiledNodes.Length != nodes.Length)
				return false;
			for (int i = 0; i < DecompiledNodes.Length; i++) {
				if (DecompiledNodes[i] != nodes[i])
					return false;
			}
			return true;
		}

		public override SavedTabState CreateSavedTabState() {
			var savedState = new SavedDecompileTabState();
			savedState.Language = Language.NameUI;
			savedState.Paths = new List<FullNodePathName>();
			savedState.ActiveAutoLoadedAssemblies = new List<string>();
			foreach (var node in DecompiledNodes) {
				savedState.Paths.Add(node.CreateFullNodePathName());
				var autoAsm = GetAutoLoadedAssemblyNode(node);
				if (!string.IsNullOrEmpty(autoAsm))
					savedState.ActiveAutoLoadedAssemblies.Add(autoAsm);
			}
			savedState.EditorPositionState = TextView.EditorPositionState;
			return savedState;
		}

		static string GetAutoLoadedAssemblyNode(ILSpyTreeNode node) {
			var assyNode = MainWindow.GetAssemblyTreeNode(node);
			if (assyNode == null)
				return null;
			var loadedAssy = assyNode.DnSpyFile;
			if (!loadedAssy.IsAutoLoaded)
				return null;

			return loadedAssy.Filename;
		}
	}
}
