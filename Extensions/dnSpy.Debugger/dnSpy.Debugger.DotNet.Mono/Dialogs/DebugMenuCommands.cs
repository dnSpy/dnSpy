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
using dnSpy.Contracts.Debugger.Attach;
using dnSpy.Contracts.Debugger.Attach.Dialogs;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Debugger.DotNet.Mono.Properties;

namespace dnSpy.Debugger.DotNet.Mono.Dialogs {
	static class DebugMenuCommands {
		abstract class AttachDebugMainMenuCommandBase : MenuItemBase {
			readonly Lazy<DbgManager> dbgManager;
			readonly Lazy<ShowAttachToProcessDialog> showAttachToProcessDialog;
			readonly bool? mustBeDebugging;

			protected AttachDebugMainMenuCommandBase(Lazy<DbgManager> dbgManager, Lazy<ShowAttachToProcessDialog> showAttachToProcessDialog, bool? mustBeDebugging) {
				this.dbgManager = dbgManager;
				this.showAttachToProcessDialog = showAttachToProcessDialog;
				this.mustBeDebugging = mustBeDebugging;
			}

			public override void Execute(IMenuItemContext context) {
				var options = new ShowAttachToProcessDialogOptions {
					ProcessType = "Unity",
					ProviderNames = new[] {
						PredefinedAttachProgramOptionsProviderNames.UnityEditor,
						PredefinedAttachProgramOptionsProviderNames.UnityPlayer,
					},
					Message = dnSpy_Debugger_DotNet_Mono_Resources.AttachToUnityProcess_DebugBuildsOnlyMessage,
					InfoLink = new AttachToProcessLinkInfo {
						ToolTipMessage = DebuggingUnityGamesHelper.DebuggingUnityGamesText,
						Url = DebuggingUnityGamesHelper.DebuggingUnityGamesUrl,
					},
				};
				showAttachToProcessDialog.Value.Attach(options);
			}
			public override bool IsVisible(IMenuItemContext context) => mustBeDebugging is null || dbgManager.Value.IsDebugging == mustBeDebugging;
			public override string? GetHeader(IMenuItemContext context) => string.Format(dnSpy_Debugger_DotNet_Mono_Resources.AttachToProcessXCommand, "_Unity");
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Icon = DsImagesAttribute.Metadata, Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 40)]
		sealed class Attach1DebugMainMenuCommand : AttachDebugMainMenuCommandBase {
			[ImportingConstructor]
			public Attach1DebugMainMenuCommand(Lazy<DbgManager> dbgManager, Lazy<ShowAttachToProcessDialog> showAttachToProcessDialog) : base(dbgManager, showAttachToProcessDialog, false) { }
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Icon = DsImagesAttribute.Metadata, Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 80)]
		sealed class Attach2DebugMainMenuCommand : AttachDebugMainMenuCommandBase {
			[ImportingConstructor]
			public Attach2DebugMainMenuCommand(Lazy<DbgManager> dbgManager, Lazy<ShowAttachToProcessDialog> showAttachToProcessDialog) : base(dbgManager, showAttachToProcessDialog, true) { }
		}
	}
}
