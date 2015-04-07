
using System.Windows.Controls;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor
{
	public abstract class TreeNodeMainMenu : TreeNodeCommand, IMainMenuCommand
	{
		protected TreeNodeMainMenu()
		{
			MainWindow.Instance.SetMenuAlwaysRegenerate("_Edit");
		}

		public bool IsVisible {
			get { return CanExecute(GetSelectedNodes()); }
		}

		protected void SetHeader(MenuItem menuItem, string singular, string plural)
		{
			SetHeader(GetSelectedNodes(), menuItem, singular, plural);
		}

		protected void SetHeader(ILSpyTreeNode[] nodes, MenuItem menuItem, string singular, string plural)
		{
			menuItem.Header = nodes != null && nodes.Length == 1 ? singular : plural;
		}
	}
}
