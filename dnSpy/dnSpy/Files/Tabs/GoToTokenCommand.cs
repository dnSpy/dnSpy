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
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;

namespace dnSpy.Files.Tabs {
	[ExportAutoLoaded]
	sealed class GoToTokenLoader : IAutoLoaded {
		static readonly RoutedCommand GoToToken = new RoutedCommand("GoToToken", typeof(GoToTokenLoader));
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		GoToTokenLoader(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_TEXTEDITOR_UICONTEXT);
			cmds.Add(GoToToken,
				(s, e) => GoToTokenCommand.ExecuteInternal(fileTabManager),
				(s, e) => e.CanExecute = GoToTokenCommand.CanExecuteInternal(fileTabManager),
				ModifierKeys.Control, Key.D);
			cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_FILE_TREEVIEW);
			cmds.Add(GoToToken,
				(s, e) => GoToTokenCommand.ExecuteInternal(fileTabManager),
				(s, e) => e.CanExecute = GoToTokenCommand.CanExecuteInternal(fileTabManager),
				ModifierKeys.Control, Key.D);
		}
	}

	static class GoToTokenCommand {
		static ITokenResolver GetResolver(IFileTabManager fileTabManager, out IFileTab tab) {
			tab = fileTabManager.ActiveTab;
			if (tab == null)
				return null;
			return tab.Content.Nodes.FirstOrDefault().GetModule();
		}

		internal static bool CanExecuteInternal(IFileTabManager fileTabManager) {
			IFileTab tab;
			return GetResolver(fileTabManager, out tab) != null;
		}

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

		internal static void ExecuteInternal(IFileTabManager fileTabManager) {
			IFileTab tab;
			var resolver = GetResolver(fileTabManager, out tab);
			if (resolver == null)
				return;

			var member = AskForDef(dnSpy_Resources.GoToToken_Title, resolver);
			if (member == null)
				return;

			tab.FollowReference(member, false);
		}

		static object AskForDef(string title, ITokenResolver resolver) => MsgBox.Instance.Ask(dnSpy_Resources.GoToToken_Label, null, title, s => {
			string error;
			uint token = SimpleTypeConverter.ParseUInt32(s, uint.MinValue, uint.MaxValue, out error);
			var memberRef = resolver.ResolveToken(token);
			var member = ResolveDef(memberRef);
			return member;
		}, s => {
			string error;
			uint token = SimpleTypeConverter.ParseUInt32(s, uint.MinValue, uint.MaxValue, out error);
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

		[ExportMenuItem(Header = "res:GoToTokenCommand", InputGestureText = "res:GoToTokenKey", Group = MenuConstants.GROUP_CTX_CODE_TOKENS, Order = 20)]
		public sealed class CodeCommand : MenuItemBase {
			readonly IFileTabManager fileTabManager;

			[ImportingConstructor]
			CodeCommand(IFileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
			}

			public override bool IsVisible(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
					return false;
				if (!CanExecuteInternal(fileTabManager))
					return false;
				return true;
			}

			public override void Execute(IMenuItemContext context) => ExecuteInternal(fileTabManager);
		}

		[ExportMenuItem(Header = "res:GoToTokenCommand", InputGestureText = "res:GoToTokenKey", Group = MenuConstants.GROUP_CTX_FILES_TOKENS, Order = 20)]
		public sealed class FilesCommand : MenuItemBase {
			readonly IFileTabManager fileTabManager;

			[ImportingConstructor]
			FilesCommand(IFileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
			}

			public override bool IsVisible(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID))
					return false;
				if (!CanExecuteInternal(fileTabManager))
					return false;
				var nodes = context.Find<ITreeNodeData[]>();
				if (nodes == null || nodes.Length == 0)
					return false;
				var elem = nodes[0];
				return elem is IFileTreeNodeData;
			}

			public override void Execute(IMenuItemContext context) => ExecuteInternal(fileTabManager);
		}
	}
}
