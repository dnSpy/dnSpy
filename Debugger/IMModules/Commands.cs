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

using System;
using dndbg.Engine;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.UI.Menus;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace dnSpy.Debugger.IMModules {
	static class ReloadAllMethodBodiesCommand {
		sealed class Context {
			public readonly MemoryModuleDefFile MemoryModuleDefFile;

			public Context(MemoryModuleDefFile file) {
				this.MemoryModuleDefFile = file;
			}
		}

		abstract class CommandBase : MenuItemBase<Context> {
			protected sealed override Context CreateContext(IMenuItemContext context) {
				var file = GetFile(context);
				return file == null ? null : new Context(file);
			}

			MemoryModuleDefFile GetFile(IMenuItemContext context) {
				var modNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(GetTreeNode(context));
				var mfile = modNode == null ? null : modNode.DnSpyFile as MemoryModuleDefFile;
				if (mfile == null)
					return null;
				if (mfile.Process.HasExited || mfile.Process.Debugger.ProcessState == DebuggerProcessState.Terminated)
					return null;
				return mfile;
			}

			protected abstract ILSpyTreeNode GetTreeNode(IMenuItemContext context);
		}

		[ExportMenuItem(Header = "Reload All Method Bodies", Icon = "Refresh", Group = MenuConstants.GROUP_CTX_FILES_DEBUGRT, Order = 0)]
		sealed class FilesCommand : CommandBase {
			protected sealed override object CachedContextKey {
				get { return ContextKey; }
			}
			static readonly object ContextKey = new object();

			public override void Execute(Context context) {
				ExecuteInternal(context);
			}

			protected override ILSpyTreeNode GetTreeNode(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID))
					return null;
				var nodes = context.FindByType<SharpTreeNode[]>();
				if (nodes != null && nodes.Length != 0)
					return nodes[0] as ILSpyTreeNode;
				return null;
			}
		}

		[ExportMenuItem(Header = "Reload All Method Bodies", Icon = "Refresh", Group = MenuConstants.GROUP_CTX_CODE_DEBUGRT, Order = 0)]
		sealed class CodeCommand : CommandBase {
			protected sealed override object CachedContextKey {
				get { return ContextKey; }
			}
			static readonly object ContextKey = new object();

			public override void Execute(Context context) {
				ExecuteInternal(context);
			}

			protected override ILSpyTreeNode GetTreeNode(IMenuItemContext context) {
				if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
					return MainWindow.Instance.TreeView.SelectedItem as ILSpyTreeNode;
				return null;
			}
		}

		static void ExecuteInternal(Context context) {
			InMemoryModuleManager.Instance.UpdateModuleMemory(context.MemoryModuleDefFile);
		}
	}
}
