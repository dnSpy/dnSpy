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

namespace dnSpy.Scripting.Roslyn.VisualBasic {
	[Export(typeof(IMainToolWindowContentCreator))]
	sealed class VisualBasicToolWindowContentCreator : ScriptToolWindowContentCreator {
		readonly Lazy<IVisualBasicContent> visualBasicContent;

		[ImportingConstructor]
		VisualBasicToolWindowContentCreator(Lazy<IVisualBasicContent> visualBasicContent)
			: base(VisualBasicToolWindowContent.THE_GUID) {
			this.visualBasicContent = visualBasicContent;
		}

		public override IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(VisualBasicToolWindowContent.THE_GUID, VisualBasicToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_SCRIPTING_VISUAL_BASIC, false); }
		}

		protected override ScriptToolWindowContent CreateContent() => new VisualBasicToolWindowContent(visualBasicContent);
	}

	sealed class VisualBasicToolWindowContent : ScriptToolWindowContent {
		public static readonly Guid THE_GUID = new Guid("C0D88168-7750-4B15-AE72-215BFE214DA5");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override string Title => dnSpy_Scripting_Roslyn_Resources.Window_VisualBasic;
		protected override IScriptContent ScriptContent => visualBasicContent.Value;
		readonly Lazy<IVisualBasicContent> visualBasicContent;

		public VisualBasicToolWindowContent(Lazy<IVisualBasicContent> visualBasicContent)
			: base(THE_GUID) {
			this.visualBasicContent = visualBasicContent;
		}
	}

	[ExportAutoLoaded]
	sealed class ShowVisualBasicInteractiveCommandLoader : IAutoLoaded {
		public static readonly RoutedCommand ShowVisualBasicInteractiveRoutedCommand = new RoutedCommand("ShowVisualBasicInteractiveRoutedCommand", typeof(ShowVisualBasicInteractiveCommandLoader));

		[ImportingConstructor]
		ShowVisualBasicInteractiveCommandLoader(IWpfCommandManager wpfCommandManager, IMainToolWindowManager mainToolWindowManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(ShowVisualBasicInteractiveRoutedCommand,
				(s, e) => mainToolWindowManager.Show(VisualBasicToolWindowContent.THE_GUID),
				(s, e) => e.CanExecute = true);
			cmds.Add(ShowVisualBasicInteractiveRoutedCommand, ModifierKeys.Control | ModifierKeys.Alt, Key.I);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:Window_VisualBasic", InputGestureText = "res:ShortCutKeyCtrlAltI", Icon = "VBInteractiveWindow", Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS, Order = 30)]
	sealed class ShowVisualBasicInteractiveCommand : MenuItemCommand {
		ShowVisualBasicInteractiveCommand()
			: base(ShowVisualBasicInteractiveCommandLoader.ShowVisualBasicInteractiveRoutedCommand) {
		}
	}
}
