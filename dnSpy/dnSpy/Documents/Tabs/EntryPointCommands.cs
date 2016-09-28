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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.Tabs {
	static class GoToEntryPointCommand {
		internal static ModuleDef GetCurrentModule(IDocumentTabService documentTabService) {
			var tab = documentTabService.ActiveTab;
			if (tab == null)
				return null;
			return tab.Content.Nodes.FirstOrDefault().GetModule();
		}

		[ExportMenuItem(Header = "res:GoToEntryPointCommand", Icon = DsImagesAttribute.EntryPoint, Group = MenuConstants.GROUP_CTX_DOCVIEWER_TOKENS, Order = 0)]
		sealed class CodeCommand : MenuItemBase {
			readonly IDocumentTabService documentTabService;

			[ImportingConstructor]
			CodeCommand(IDocumentTabService documentTabService) {
				this.documentTabService = documentTabService;
			}

			public override bool IsVisible(IMenuItemContext context) => GetEntryPoint(documentTabService, context) != null;

			static MethodDef GetEntryPoint(IDocumentTabService documentTabService, IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
					return null;
				var module = GetCurrentModule(documentTabService);
				return module == null ? null : module.EntryPoint as MethodDef;
			}

			public override void Execute(IMenuItemContext context) {
				var ep = GetEntryPoint(documentTabService, context);
				if (ep != null)
					documentTabService.FollowReference(ep);
			}
		}

		[ExportMenuItem(Header = "res:GoToEntryPointCommand", Icon = DsImagesAttribute.EntryPoint, Group = MenuConstants.GROUP_CTX_DOCUMENTS_TOKENS, Order = 0)]
		sealed class DocumentsCommand : MenuItemBase {
			readonly IDocumentTabService documentTabService;

			[ImportingConstructor]
			DocumentsCommand(IDocumentTabService documentTabService) {
				this.documentTabService = documentTabService;
			}

			public override bool IsVisible(IMenuItemContext context) => GetEntryPoint(context) != null;

			static MethodDef GetEntryPoint(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID))
					return null;
				var nodes = context.Find<ITreeNodeData[]>();
				var node = nodes == null || nodes.Length == 0 ? null : nodes[0];
				var module = node.GetModule();
				return module == null ? null : module.EntryPoint as MethodDef;
			}

			public override void Execute(IMenuItemContext context) {
				var ep = GetEntryPoint(context);
				if (ep != null)
					documentTabService.FollowReference(ep);
			}
		}
	}

	static class GoToGlobalTypeCctorCommand {
		[ExportMenuItem(Header = "res:GoToGlobalCctorCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_TOKENS, Order = 10)]
		sealed class CodeCommand : MenuItemBase {
			readonly IDocumentTabService documentTabService;

			[ImportingConstructor]
			CodeCommand(IDocumentTabService documentTabService) {
				this.documentTabService = documentTabService;
			}

			public override bool IsVisible(IMenuItemContext context) => GetModuleCctor(documentTabService, context) != null;

			static MethodDef GetModuleCctor(IDocumentTabService documentTabService, IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
					return null;
				var module = GoToEntryPointCommand.GetCurrentModule(documentTabService);
				if (module == null)
					return null;
				var gt = module.GlobalType;
				return gt == null ? null : gt.FindStaticConstructor();
			}

			public override void Execute(IMenuItemContext context) {
				var ep = GetModuleCctor(documentTabService, context);
				if (ep != null)
					documentTabService.FollowReference(ep);
			}
		}

		[ExportMenuItem(Header = "res:GoToGlobalCctorCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_TOKENS, Order = 10)]
		sealed class DocumentsCommand : MenuItemBase {
			readonly IDocumentTabService documentTabService;

			[ImportingConstructor]
			DocumentsCommand(IDocumentTabService documentTabService) {
				this.documentTabService = documentTabService;
			}

			public override bool IsVisible(IMenuItemContext context) => GetModuleCctor(context) != null;

			static MethodDef GetModuleCctor(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID))
					return null;
				var nodes = context.Find<ITreeNodeData[]>();
				var node = nodes == null || nodes.Length == 0 ? null : nodes[0];
				var module = node.GetModule();
				if (module == null)
					return null;
				var gt = module.GlobalType;
				return gt == null ? null : gt.FindStaticConstructor();
			}

			public override void Execute(IMenuItemContext context) {
				var ep = GetModuleCctor(context);
				if (ep != null)
					documentTabService.FollowReference(ep);
			}
		}
	}
}
