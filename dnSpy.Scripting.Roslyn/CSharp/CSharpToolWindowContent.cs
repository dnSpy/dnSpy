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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Scripting.Roslyn.Common;
using dnSpy.Scripting.Roslyn.Properties;
using dnSpy.Shared.Menus;

namespace dnSpy.Scripting.Roslyn.CSharp {
	[Export(typeof(IMainToolWindowContentCreator))]
	sealed class CSharpToolWindowContentCreator : ScriptToolWindowContentCreator {
		readonly Lazy<ICSharpContent> csharpContent;

		[ImportingConstructor]
		CSharpToolWindowContentCreator(Lazy<ICSharpContent> csharpContent)
			: base(CSharpToolWindowContent.THE_GUID) {
			this.csharpContent = csharpContent;
		}

		public override IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(CSharpToolWindowContent.THE_GUID, CSharpToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_SCRIPTING_CSHARP, false); }
		}

		protected override ScriptToolWindowContent CreateContent() => new CSharpToolWindowContent(csharpContent);
	}

	sealed class CSharpToolWindowContent : ScriptToolWindowContent {
		public static readonly Guid THE_GUID = new Guid("FFD1F456-DC69-46D7-8C96-858B36C57BC3");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.Default;

		public override string Title => dnSpy_Scripting_Roslyn_Resources.Window_CSharp;
		protected override IScriptContent ScriptContent => csharpContent.Value;
		readonly Lazy<ICSharpContent> csharpContent;

		public CSharpToolWindowContent(Lazy<ICSharpContent> csharpContent)
			: base(THE_GUID) {
			this.csharpContent = csharpContent;
		}
	}

	[ExportAutoLoaded]
	sealed class ShowCSharpInteractiveCommandLoader : IAutoLoaded {
		public static readonly RoutedCommand ShowCSharpInteractiveRoutedCommand = new RoutedCommand("ShowCSharpInteractiveRoutedCommand", typeof(ShowCSharpInteractiveCommandLoader));

		[ImportingConstructor]
		ShowCSharpInteractiveCommandLoader(IWpfCommandManager wpfCommandManager, IMainToolWindowManager mainToolWindowManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(ShowCSharpInteractiveRoutedCommand,
				(s, e) => mainToolWindowManager.Show(CSharpToolWindowContent.THE_GUID),
				(s, e) => e.CanExecute = true);
			cmds.Add(ShowCSharpInteractiveRoutedCommand, ModifierKeys.Control | ModifierKeys.Alt, Key.N);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:Window_CSharp", InputGestureText = "res:ShortCutKeyCtrlAltN", Icon = "CSInteractiveWindow", Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS, Order = 20)]
	sealed class ShowCSharpInteractiveCommand : MenuItemCommand {
		ShowCSharpInteractiveCommand()
			: base(ShowCSharpInteractiveCommandLoader.ShowCSharpInteractiveRoutedCommand) {
		}
	}
}
