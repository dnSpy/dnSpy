
using System.Linq;
using System.Text;
using System.Windows;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes {
	// XML Doc names are used when selecting a type/member/ns from the command line
	[ExportContextMenuEntryAttribute(Header = "Copy _XML Doc Name", Order = 960, Category = "Other")]
	class CopyXmlDocNameMenuEntry : IContextMenuEntry {

		public bool IsVisible(TextViewContext context)
		{
			return context.SelectedTreeNodes != null &&
				context.SelectedTreeNodes.Any(n => CanCopyXmlKey(n));
		}

		public bool IsEnabled(TextViewContext context)
		{
			return IsVisible(context);
		}

		static bool CanCopyXmlKey(SharpTreeNode node)
		{
			return node is IMemberTreeNode ||
				node is NamespaceTreeNode;
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null || context.SelectedTreeNodes.Length == 0)
				return;
			var sb = new StringBuilder();
			int numAdded = 0;
			foreach (var node in context.SelectedTreeNodes) {
				if (CanCopyXmlKey(node)) {
					if (numAdded++ > 0)
						sb.AppendLine();
					var mrNode = node as IMemberTreeNode;
					if (mrNode != null)
						sb.Append(XmlDocKeyProvider.GetKey(mrNode.Member));
					else {
						var nsNode = (NamespaceTreeNode)node;
						sb.Append("N:");
						sb.Append(nsNode.Name);
					}
				}
			}
			if (numAdded > 1)
				sb.AppendLine();
			Clipboard.SetText(sb.ToString());
		}
	}
}
