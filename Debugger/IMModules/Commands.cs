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
using System.ComponentModel.Composition;
using dndbg.Engine;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.UI.Menus;

namespace dnSpy.Debugger.IMModules {
	static class ReloadAllMethodBodiesCommand {
		sealed class Context {
			public readonly MemoryModuleDefFile MemoryModuleDefFile;

			public Context(MemoryModuleDefFile file) {
				this.MemoryModuleDefFile = file;
			}
		}

		abstract class CommandBase : MenuItemBase<Context> {
			readonly Lazy<IInMemoryModuleManager> inMemoryModuleManager;

			protected CommandBase(Lazy<IInMemoryModuleManager> inMemoryModuleManager) {
				this.inMemoryModuleManager = inMemoryModuleManager;
			}

			protected sealed override Context CreateContext(IMenuItemContext context) {
				var file = GetFile(context);
				return file == null ? null : new Context(file);
			}

			MemoryModuleDefFile GetFile(IMenuItemContext context) {
				var node = GetTreeNode(context);
				var modNode = node.GetModuleNode();
				var mfile = modNode == null ? null : modNode.DnSpyFile as MemoryModuleDefFile;
				if (mfile == null)
					return null;
				if (mfile.Process.HasExited || mfile.Process.Debugger.ProcessState == DebuggerProcessState.Terminated)
					return null;
				return mfile;
			}

			protected abstract IFileTreeNodeData GetTreeNode(IMenuItemContext context);

			protected void ExecuteInternal(Context context) {
				inMemoryModuleManager.Value.UpdateModuleMemory(context.MemoryModuleDefFile);
			}
		}

		[ExportMenuItem(Header = "res:ReloadAllMethodBodiesCommand", Icon = "Refresh", Group = MenuConstants.GROUP_CTX_FILES_DEBUGRT, Order = 0)]
		sealed class FilesCommand : CommandBase {
			protected sealed override object CachedContextKey {
				get { return ContextKey; }
			}
			static readonly object ContextKey = new object();

			[ImportingConstructor]
			FilesCommand(Lazy<IInMemoryModuleManager> inMemoryModuleManager)
				: base(inMemoryModuleManager) {
			}

			public override void Execute(Context context) {
				ExecuteInternal(context);
			}

			protected override IFileTreeNodeData GetTreeNode(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID))
					return null;
				var nodes = context.Find<ITreeNodeData[]>();
				if (nodes != null && nodes.Length != 0)
					return nodes[0] as IFileTreeNodeData;
				return null;
			}
		}

		[ExportMenuItem(Header = "res:ReloadAllMethodBodiesCommand", Icon = "Refresh", Group = MenuConstants.GROUP_CTX_CODE_DEBUGRT, Order = 0)]
		sealed class CodeCommand : CommandBase {
			protected sealed override object CachedContextKey {
				get { return ContextKey; }
			}
			static readonly object ContextKey = new object();

			readonly IFileTreeView fileTreeView;

			[ImportingConstructor]
			CodeCommand(Lazy<IInMemoryModuleManager> inMemoryModuleManager, IFileTreeView fileTreeView)
				: base(inMemoryModuleManager) {
				this.fileTreeView = fileTreeView;
			}

			public override void Execute(Context context) {
				ExecuteInternal(context);
			}

			protected override IFileTreeNodeData GetTreeNode(IMenuItemContext context) {
				if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
					return fileTreeView.TreeView.SelectedItem as IFileTreeNodeData;
				return null;
			}
		}
	}
}
