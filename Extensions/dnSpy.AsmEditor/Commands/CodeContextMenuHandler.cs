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
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Menus;

namespace dnSpy.AsmEditor.Commands {
	sealed class CodeContext {
		public IDocumentTreeNodeData[] Nodes { get; }
		public bool IsDefinition { get; }
		public IMenuItemContext MenuItemContextOrNull { get; }

		public CodeContext(IDocumentTreeNodeData[] nodes, bool isDefinition, IMenuItemContext menuItemContext) {
			this.Nodes = nodes ?? Array.Empty<IDocumentTreeNodeData>();
			this.IsDefinition = isDefinition;
			this.MenuItemContextOrNull = menuItemContext;
		}
	}

	abstract class CodeContextMenuHandler : MenuItemBase<CodeContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		public sealed override bool IsVisible(CodeContext context) => IsEnabled(context);

		readonly IDocumentTreeView documentTreeView;

		protected CodeContextMenuHandler(IDocumentTreeView documentTreeView) {
			this.documentTreeView = documentTreeView;
		}

		protected sealed override CodeContext CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return null;
			var textRef = context.Find<TextReference>();
			if (textRef == null)
				return null;
			var node = documentTreeView.FindNode(textRef.Reference);
			var nodes = node == null ? Array.Empty<IDocumentTreeNodeData>() : new IDocumentTreeNodeData[] { node };
			return new CodeContext(nodes, textRef.IsDefinition, context);
		}
	}
}
