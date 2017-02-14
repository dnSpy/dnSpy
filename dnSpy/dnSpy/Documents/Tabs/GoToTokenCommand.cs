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
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;

namespace dnSpy.Documents.Tabs {
	[ExportAutoLoaded]
	sealed class GoToTokenLoader : IAutoLoaded {
		static readonly RoutedCommand GoToToken = new RoutedCommand("GoToToken", typeof(GoToTokenLoader));
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		GoToTokenLoader(IWpfCommandService wpfCommandService, IDocumentTabService documentTabService) {
			this.documentTabService = documentTabService;
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENTVIEWER_UICONTEXT);
			cmds.Add(GoToToken,
				(s, e) => GoToTokenCommand.ExecuteInternal(documentTabService),
				(s, e) => e.CanExecute = GoToTokenCommand.CanExecuteInternal(documentTabService),
				ModifierKeys.Control, Key.D);
			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENT_TREEVIEW);
			cmds.Add(GoToToken,
				(s, e) => GoToTokenCommand.ExecuteInternal(documentTabService),
				(s, e) => e.CanExecute = GoToTokenCommand.CanExecuteInternal(documentTabService),
				ModifierKeys.Control, Key.D);
		}
	}

	static class GoToTokenCommand {
		static ITokenResolver GetResolver(IDocumentTabService documentTabService, out IDocumentTab tab) {
			tab = documentTabService.ActiveTab;
			if (tab == null)
				return null;
			return tab.Content.Nodes.FirstOrDefault().GetModule();
		}

		internal static bool CanExecuteInternal(IDocumentTabService documentTabService) => GetResolver(documentTabService, out var tab) != null;

		static object ResolveDef(object mr) {
			if (mr is ParamDef)
				return mr;
			if (mr is ITypeDefOrRef)
				return ((ITypeDefOrRef)mr).ResolveTypeDef();
			if (mr is IMethod && ((IMethod)mr).IsMethod)
				return ((IMethod)mr).ResolveMethodDef();
			if (mr is IField)
				return ((IField)mr).ResolveFieldDef();
			return mr as IDnlibDef;
		}

		internal static void ExecuteInternal(IDocumentTabService documentTabService) {
			var resolver = GetResolver(documentTabService, out var tab);
			if (resolver == null)
				return;

			var member = AskForDef(dnSpy_Resources.GoToToken_Title, resolver);
			if (member == null)
				return;

			tab.FollowReference(member, false);
		}

		static object AskForDef(string title, ITokenResolver resolver) => MsgBox.Instance.Ask(dnSpy_Resources.GoToToken_Label, null, title, s => {
			uint token = SimpleTypeConverter.ParseUInt32(s, uint.MinValue, uint.MaxValue, out string error);
			var memberRef = resolver.ResolveToken(token);
			var member = ResolveDef(memberRef);
			return member;
		}, s => {
			uint token = SimpleTypeConverter.ParseUInt32(s, uint.MinValue, uint.MaxValue, out string error);
			if (!string.IsNullOrEmpty(error))
				return error;
			var memberRef = resolver.ResolveToken(token);
			var member = ResolveDef(memberRef);
			if (memberRef == null)
				return string.Format(dnSpy_Resources.GoToToken_InvalidToken, token);
			else if (member == null)
				return string.Format(dnSpy_Resources.GoToToken_CouldNotResolve, token);
			return string.Empty;
		});

		[ExportMenuItem(Header = "res:GoToTokenCommand", InputGestureText = "res:GoToTokenKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_TOKENS, Order = 20)]
		public sealed class CodeCommand : MenuItemBase {
			readonly IDocumentTabService documentTabService;

			[ImportingConstructor]
			CodeCommand(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;

			public override bool IsVisible(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
					return false;
				if (!CanExecuteInternal(documentTabService))
					return false;
				return true;
			}

			public override void Execute(IMenuItemContext context) => ExecuteInternal(documentTabService);
		}

		[ExportMenuItem(Header = "res:GoToTokenCommand", InputGestureText = "res:GoToTokenKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_TOKENS, Order = 20)]
		public sealed class DocumentsCommand : MenuItemBase {
			readonly IDocumentTabService documentTabService;

			[ImportingConstructor]
			DocumentsCommand(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;

			public override bool IsVisible(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID))
					return false;
				if (!CanExecuteInternal(documentTabService))
					return false;
				var nodes = context.Find<TreeNodeData[]>();
				if (nodes == null || nodes.Length == 0)
					return false;
				var elem = nodes[0];
				return elem is DocumentTreeNodeData;
			}

			public override void Execute(IMenuItemContext context) => ExecuteInternal(documentTabService);
		}
	}
}
