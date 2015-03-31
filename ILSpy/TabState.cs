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

		internal TabManagerBase Owner;

		const int MAX_HEADER_LENGTH = 40;
		public string ShortHeader {
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
		}

		public static TabState GetTabState(FrameworkElement elem)
		{
			if (elem == null)
				return null;
			return elem.Tag as TabState;
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
			if (elem == null)
				return null;
			return elem.Tag as TabStateDecompile;
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
