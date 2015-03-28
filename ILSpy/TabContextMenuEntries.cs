using System.Windows.Controls;

namespace ICSharpCode.ILSpy
{
	[ExportContextMenuEntry(Header = "Open in New _Tab", Order = 130, InputGestureText = "Ctrl+T", Category = "Tabs")]
	class OpenInNewTabContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.SelectedTreeNodes != null &&
				context.SelectedTreeNodes.Length > 0 &&
				context.TreeView == MainWindow.Instance.treeView;
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.OpenNewTab();
		}
	}

	[ExportContextMenuEntry(Header = "_Close", Order = 100, InputGestureText = "Ctrl+W", Category = "Tabs")]
	class CloseTabContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.TabControl == MainWindow.Instance.tabControl &&
				MainWindow.Instance.CloseActiveTabPossible();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.CloseActiveTab();
		}
	}

	[ExportContextMenuEntry(Header = "Close _All But This", Order = 110, Category = "Tabs")]
	class CloseAllTabsButThisContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.TabControl == MainWindow.Instance.tabControl &&
				MainWindow.Instance.CloseAllButActiveTabPossible();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.CloseAllButActiveTab();
		}
	}

	[ExportContextMenuEntry(Header = "Clone _Tab", Order = 120, Category = "Tabs")]
	class CloneTabContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.TabControl == MainWindow.Instance.tabControl &&
				MainWindow.Instance.CloneActiveTabPossible();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.CloneActiveTab();
		}
	}

	[ExportContextMenuEntry(Header = "Open in New _Tab", Order = 130, Category = "Tabs")]
	class OpenReferenceInNewTabContextMenuEntry : IContextMenuEntry2
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.TextView != null &&
				context.Reference != null;
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.OpenReferenceInNewTab(context.TextView, context.Reference);
		}

		public void Initialize(TextViewContext context, MenuItem menuItem)
		{
			menuItem.InputGestureText = context.OpenedFromKeyboard ? "Ctrl+F12" : "Ctrl+Click";
		}
	}
}
