using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy
{
	abstract class TabState : IDisposable
	{
		public abstract string Header { get; }
		public TabItem TabItem;

		const int MAX_HEADER_LENGTH = 40;
		string ShortHeader {
			get {
				var header = Header;
				if (header.Length <= MAX_HEADER_LENGTH)
					return header;
				return header.Substring(0, MAX_HEADER_LENGTH) + "...";
			}
		}

		protected TabState()
		{
			var tabItem = new TabItem();
			TabItem = tabItem;
			tabItem.Tag = this;
			tabItem.AllowDrop = true;

			tabItem.MouseRightButtonDown += tabItem_MouseRightButtonDown;
			tabItem.PreviewMouseDown += tabItem_PreviewMouseDown;
			tabItem.DragOver += tabItem_DragOver;
			tabItem.Drop += tabItem_Drop;
		}

		public static TabState GetTabState(FrameworkElement elem)
		{
			return (TabState)elem.Tag;
		}

		void tabItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			var tabControl = TabItem.Parent as TabControl;
			if (tabControl != null)
				tabControl.SelectedItem = TabItem;
		}

		void tabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			var tabItem = sender as TabItem;
			if (tabItem == null || tabItem != TabItem)
				return;

			var mousePos = e.GetPosition(tabItem);
			var tabSize = new Point(tabItem.ActualWidth, tabItem.ActualHeight);
			if (mousePos.X >= tabSize.X || mousePos.Y > tabSize.Y)
				return;

			var tabControl = tabItem.Parent as TabControl;
			if (tabControl == null)
				return;

			if (e.LeftButton == MouseButtonState.Pressed) {
				tabControl.SelectedItem = tabItem;//TODO: Doesn't work immediately!
				DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.Move);
			}
		}

		void tabItem_DragOver(object sender, DragEventArgs e)
		{
			bool canDrag = false;

			if (e.Data.GetDataPresent(typeof(TabItem))) {
				var tabItem = (TabItem)e.Data.GetData(typeof(TabItem));
				//TODO: Moving to another tab control is not supported at the moment (hasn't been tested)
				if (tabItem.Parent == TabItem.Parent)
					canDrag = true;
			}

			e.Effects = canDrag ? DragDropEffects.Move : DragDropEffects.None;
			e.Handled = true;
		}

		void tabItem_Drop(object sender, DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(typeof(TabItem)))
				return;
			var target = sender as TabItem;
			var source = (TabItem)e.Data.GetData(typeof(TabItem));
			if (target == null || source == null || target == source)
				return;
			var tabControlTarget = target.Parent as TabControl;
			if (tabControlTarget == null)
				return;
			var tabControlSource = source.Parent as TabControl;
			if (tabControlSource == null)
				return;

			//TODO: Remove MainWindow.Instance reference
			var old = MainWindow.Instance.tabManager_SelectionChanged_dont_select;
			MainWindow.Instance.tabManager_SelectionChanged_dont_select = true;
			try {
				int index = tabControlTarget.Items.IndexOf(target);
				tabControlSource.Items.Remove(source);
				tabControlTarget.Items.Insert(index, source);
				tabControlTarget.SelectedItem = source;
			}
			finally {
				MainWindow.Instance.tabManager_SelectionChanged_dont_select = old;
			}
		}

		public void InitializeHeader()
		{
			var shortHeader = ShortHeader;
			var header = Header;
			TabItem.Header = new TextBlock {
				Text = shortHeader,
				ToolTip = shortHeader == header ? null : header,
			};
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool isDisposing)
		{
		}
	}

	sealed class TabStateDecompile : TabState
	{
		public readonly DecompilerTextView TextView = new DecompilerTextView();
		public readonly NavigationHistory<NavigationState> History = new NavigationHistory<NavigationState>();
		public bool ignoreDecompilationRequests;
		public ILSpyTreeNode[] DecompiledNodes = new ILSpyTreeNode[0];
		public string Title;
		public Language Language;

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

		public static TabStateDecompile GetTabStateDecompile(FrameworkElement elem)
		{
			return (TabStateDecompile)elem.Tag;
		}

		public TabStateDecompile(Language language)
		{
			this.TextView.Tag = this;
			var view = TextView;
			TabItem.Content = view;
			Language = language;
			InitializeHeader();
			ContextMenuProvider.Add(view);
			view.DragOver += view_DragOver;
		}

		void view_DragOver(object sender, DragEventArgs e)
		{
			// The text editor seems to allow anything
			if (e.Data.GetDataPresent(typeof(TabItem))) {
				e.Effects = DragDropEffects.None;
				e.Handled = true;
				return;
			}
		}

		protected override void Dispose(bool isDisposing)
		{
			if (isDisposing)
				TextView.Dispose();
		}

		public bool Equals(ILSpyTreeNode[] nodes, Language language)
		{
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
	}
}
