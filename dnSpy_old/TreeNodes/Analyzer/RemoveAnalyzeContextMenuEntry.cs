// Copyright (c) 2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.UI.Menus;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer {
	[ExportMenuItem(Header = "_Remove", Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_ANALYZER_OTHER, Order = 20)]
	internal sealed class RemoveAnalyzeCtxMenuCommand : MenuItemBase {
		public override bool IsVisible(IMenuItemContext context) {
			return GetNodes(context) != null;
		}

		static SharpTreeNode[] GetNodes(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_ANALYZER_GUID))
				return null;
			var nodes = context.FindByType<SharpTreeNode[]>();
			if (nodes == null)
				return null;
			if (nodes.Length == 0 || !nodes.All(a => a.Parent.IsRoot))
				return null;
			return nodes;
		}

		public override void Execute(IMenuItemContext context) {
			var nodes = GetNodes(context);
			if (nodes != null) {
				foreach (var node in nodes)
					node.Parent.Children.Remove(node);
			}
		}
	}
}
