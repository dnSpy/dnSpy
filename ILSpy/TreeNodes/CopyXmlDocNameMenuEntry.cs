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
					if (mrNode != null && mrNode.Member != null)
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
