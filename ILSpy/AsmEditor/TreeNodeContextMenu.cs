
using System.Windows.Controls;

namespace ICSharpCode.ILSpy.AsmEditor
{
	public abstract class TreeNodeContextMenu : IContextMenuEntry2
	{
		public virtual bool IsVisible(TextViewContext context)
		{
			return context.TreeView == MainWindow.Instance.treeView &&
				context.SelectedTreeNodes != null &&
				context.SelectedTreeNodes.Length > 0;
		}

		public virtual bool IsEnabled(TextViewContext context)
		{
			return IsVisible(context);
		}

		public abstract void Execute(TextViewContext context);

		public virtual void Initialize(TextViewContext context, MenuItem menuItem)
		{
		}

		protected void SetHeader(TextViewContext context, MenuItem menuItem, string singular, string plural)
		{
			menuItem.Header = context.SelectedTreeNodes != null &&
						context.SelectedTreeNodes.Length == 1 ?
						singular : plural;
		}
	}
}
