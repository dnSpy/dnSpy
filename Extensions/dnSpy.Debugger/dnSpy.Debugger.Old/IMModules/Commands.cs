/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dndbg.Engine;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Debugger.IMModules {
	static class ReloadAllMethodBodiesCommand {
		sealed class Context {
			public readonly MemoryModuleDefFile MemoryModuleDefFile;

			public Context(MemoryModuleDefFile file) {
				MemoryModuleDefFile = file;
			}
		}

		abstract class CommandBase : MenuItemBase<Context> {
			readonly Lazy<IInMemoryModuleService> inMemoryModuleService;

			protected CommandBase(Lazy<IInMemoryModuleService> inMemoryModuleService) {
				this.inMemoryModuleService = inMemoryModuleService;
			}

			protected sealed override Context CreateContext(IMenuItemContext context) {
				var file = GetFile(context);
				return file == null ? null : new Context(file);
			}

			MemoryModuleDefFile GetFile(IMenuItemContext context) {
				var mfile = GetTreeNode(context).GetModuleNode()?.Document as MemoryModuleDefFile;
				if (mfile == null)
					return null;
				if (mfile.Process.HasExited || mfile.Process.Debugger.ProcessState == DebuggerProcessState.Terminated)
					return null;
				return mfile;
			}

			protected abstract DocumentTreeNodeData GetTreeNode(IMenuItemContext context);
			protected void ExecuteInternal(Context context) =>
				inMemoryModuleService.Value.UpdateModuleMemory(context.MemoryModuleDefFile);
		}

		//[ExportMenuItem(Header = "res:ReloadAllMethodBodiesCommand", Icon = DsImagesAttribute.Refresh, Group = MenuConstants.GROUP_CTX_DOCUMENTS_DEBUGRT, Order = 0)]
		sealed class FilesCommand : CommandBase {
			protected sealed override object CachedContextKey => ContextKey;
			static readonly object ContextKey = new object();

			[ImportingConstructor]
			FilesCommand(Lazy<IInMemoryModuleService> inMemoryModuleService)
				: base(inMemoryModuleService) {
			}

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

		//[ExportMenuItem(Header = "res:ReloadAllMethodBodiesCommand", Icon = DsImagesAttribute.Refresh, Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUGRT, Order = 0)]
		sealed class CodeCommand : CommandBase {
			protected sealed override object CachedContextKey => ContextKey;
			static readonly object ContextKey = new object();

			readonly IDocumentTreeView documentTreeView;

			[ImportingConstructor]
			CodeCommand(Lazy<IInMemoryModuleService> inMemoryModuleService, IDocumentTreeView documentTreeView)
				: base(inMemoryModuleService) {
				this.documentTreeView = documentTreeView;
			}

			public override void Execute(Context context) => ExecuteInternal(context);

			protected override DocumentTreeNodeData GetTreeNode(IMenuItemContext context) {
				if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
					return documentTreeView.TreeView.SelectedItem as DocumentTreeNodeData;
				return null;
			}
		}
	}
}
