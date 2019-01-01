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
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.Disassembly {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, Lazy<DisassemblyOperations> disassemblyOperations) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(new RelayCommand(a => disassemblyOperations.Value.ShowDisassembly_CurrentFrame(), a => disassemblyOperations.Value.CanShowDisassembly_CurrentFrame), ModifierKeys.Alt, Key.D8);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:DisassemblyCommand", Icon = DsImagesAttribute.DisassemblyWindow, InputGestureText = "res:ShortCutKeyAlt8", Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_MEMORY, Order = 10)]
	sealed class DisassemblyCommand : MenuItemBase {
		readonly Lazy<DisassemblyOperations> disassemblyOperations;

		[ImportingConstructor]
		DisassemblyCommand(Lazy<DisassemblyOperations> disassemblyOperations) => this.disassemblyOperations = disassemblyOperations;

		public override void Execute(IMenuItemContext context) => disassemblyOperations.Value.ShowDisassembly_CurrentFrame();
		public override bool IsEnabled(IMenuItemContext context) => disassemblyOperations.Value.CanShowDisassembly_CurrentFrame;
		public override bool IsVisible(IMenuItemContext context) => disassemblyOperations.Value.IsDebugging;
	}
}
