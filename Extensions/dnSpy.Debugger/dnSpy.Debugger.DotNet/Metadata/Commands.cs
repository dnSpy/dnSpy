/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Debugger.DotNet.Metadata {
	static class ReloadAllMethodBodiesCommand {
		sealed class Context {
			public readonly MemoryModuleDefDocument MemoryModuleDefDocument;
			public Context(MemoryModuleDefDocument file) => MemoryModuleDefDocument = file;
		}

		abstract class CommandBase : MenuItemBase<Context> {
			protected sealed override Context CreateContext(IMenuItemContext context) {
				var doc = GetDocument(context);
				return doc == null ? null : new Context(doc);
			}

			MemoryModuleDefDocument GetDocument(IMenuItemContext context) {
				var doc = GetTreeNode(context).GetModuleNode()?.Document as MemoryModuleDefDocument;
				if (doc == null)
					return null;
				if (doc.Process.State == DbgProcessState.Terminated)
					return null;
				return doc;
			}

			protected abstract DocumentTreeNodeData GetTreeNode(IMenuItemContext context);
			protected void ExecuteInternal(Context context) => context.MemoryModuleDefDocument.UpdateMemory();
		}

		[ExportMenuItem(Header = "res:ReloadAllMethodBodiesCommand", Icon = DsImagesAttribute.Refresh, Group = MenuConstants.GROUP_CTX_DOCUMENTS_DEBUGRT, Order = 0)]
		sealed class FilesCommand : CommandBase {
			protected sealed override object CachedContextKey => ContextKey;
			static readonly object ContextKey = new object();

			public override void Execute(Context context) => ExecuteInternal(context);

			protected override DocumentTreeNodeData GetTreeNode(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID))
					return null;
				var nodes = context.Find<TreeNodeData[]>();
				if (nodes != null && nodes.Length != 0)
					return nodes[0] as DocumentTreeNodeData;
				return null;
			}
		}

		[ExportMenuItem(Header = "res:ReloadAllMethodBodiesCommand", Icon = DsImagesAttribute.Refresh, Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUGRT, Order = 0)]
		sealed class CodeCommand : CommandBase {
			protected sealed override object CachedContextKey => ContextKey;
			static readonly object ContextKey = new object();

			readonly IDocumentTreeView documentTreeView;

			[ImportingConstructor]
			CodeCommand(IDocumentTreeView documentTreeView) => this.documentTreeView = documentTreeView;

			public override void Execute(Context context) => ExecuteInternal(context);

			protected override DocumentTreeNodeData GetTreeNode(IMenuItemContext context) {
				if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
					return documentTreeView.TreeView.SelectedItem as DocumentTreeNodeData;
				return null;
			}
		}
	}
}
