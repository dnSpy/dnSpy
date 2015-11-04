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
using dnlib.DotNet;
using dnSpy.Contracts.Menus;
using dnSpy.Menus;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace dnSpy.Commands {
	static class GoToEntryPointCommand {
		internal static ModuleDef GetCurrentModule() {
			var tabState = MainWindow.Instance.GetActiveDecompileTabState();
			if (tabState == null)
				return null;
			return ILSpyTreeNode.GetModule(tabState.DecompiledNodes) as ModuleDef;
		}

		[ExportMenuItem(Header = "Go to _Entry Point", Icon = "EntryPoint", Group = MenuConstants.GROUP_CTX_CODE_EP, Order = 0)]
		sealed class CodeCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) {
				return GetEntryPoint(context) != null;
			}

			static MethodDef GetEntryPoint(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID))
					return null;
				var module = GetCurrentModule();
				return module == null ? null : module.EntryPoint as MethodDef;
			}

			public override void Execute(IMenuItemContext context) {
				var ep = GetEntryPoint(context);
				if (ep != null)
					MainWindow.Instance.JumpToReference(ep);
			}
		}

		[ExportMenuItem(Header = "Go to _Entry Point", Icon = "EntryPoint", Group = MenuConstants.GROUP_CTX_FILES_EP, Order = 0)]
		sealed class FilesCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) {
				return GetEntryPoint(context) != null;
			}

			static MethodDef GetEntryPoint(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID))
					return null;
				var nodes = context.FindByType<SharpTreeNode[]>();
				var module = ILSpyTreeNode.GetModule(nodes);
				return module == null ? null : module.EntryPoint as MethodDef;
			}

			public override void Execute(IMenuItemContext context) {
				var ep = GetEntryPoint(context);
				if (ep != null)
					MainWindow.Instance.JumpToReference(ep);
			}
		}
	}

	static class GoToGlobalTypeCctorCommand {
		[ExportMenuItem(Header = "Go to <Module> .ccto_r", Group = MenuConstants.GROUP_CTX_CODE_EP, Order = 10)]
		sealed class CodeCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) {
				return GetModuleCctor(context) != null;
			}

			static MethodDef GetModuleCctor(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID))
					return null;
				var module = GoToEntryPointCommand.GetCurrentModule();
				if (module == null)
					return null;
				var gt = module.GlobalType;
				return gt == null ? null : gt.FindStaticConstructor();
			}

			public override void Execute(IMenuItemContext context) {
				var ep = GetModuleCctor(context);
				if (ep != null)
					MainWindow.Instance.JumpToReference(ep);
			}
		}

		[ExportMenuItem(Header = "Go to <Module> .ccto_r", Group = MenuConstants.GROUP_CTX_FILES_EP, Order = 10)]
		sealed class FilesCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) {
				return GetModuleCctor(context) != null;
			}

			static MethodDef GetModuleCctor(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID))
					return null;
				var nodes = context.FindByType<SharpTreeNode[]>();
				var module = ILSpyTreeNode.GetModule(nodes);
				if (module == null)
					return null;
				var gt = module.GlobalType;
				return gt == null ? null : gt.FindStaticConstructor();
			}

			public override void Execute(IMenuItemContext context) {
				var ep = GetModuleCctor(context);
				if (ep != null)
					MainWindow.Instance.JumpToReference(ep);
			}
		}
	}
}
