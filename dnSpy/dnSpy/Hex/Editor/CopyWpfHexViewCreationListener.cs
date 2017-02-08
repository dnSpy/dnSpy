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

using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Hex.Editor;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.Editor {
	[Export(typeof(WpfHexViewCreationListener))]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Interactive)]
	sealed class CopyWpfHexViewCreationListener : WpfHexViewCreationListener {
		public override void HexViewCreated(WpfHexView hexView) {
			hexView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, (s, e) => hexView.CommandTarget.Execute(CommandConstants.StandardGroup, (int)StandardIds.Cut), (s, e) => e.CanExecute = hexView.CommandTarget.CanExecute(CommandConstants.StandardGroup, (int)StandardIds.Cut) == CommandTargetStatus.Handled));
			hexView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (s, e) => hexView.CommandTarget.Execute(CommandConstants.StandardGroup, (int)StandardIds.Copy), (s, e) => e.CanExecute = hexView.CommandTarget.CanExecute(CommandConstants.StandardGroup, (int)StandardIds.Copy) == CommandTargetStatus.Handled));
			hexView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (s, e) => hexView.CommandTarget.Execute(CommandConstants.StandardGroup, (int)StandardIds.Paste), (s, e) => e.CanExecute = hexView.CommandTarget.CanExecute(CommandConstants.StandardGroup, (int)StandardIds.Paste) == CommandTargetStatus.Handled));
			hexView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, (s, e) => hexView.CommandTarget.Execute(CommandConstants.HexEditorGroup, (int)HexEditorIds.SELECTALL), (s, e) => e.CanExecute = hexView.CommandTarget.CanExecute(CommandConstants.HexEditorGroup, (int)HexEditorIds.SELECTALL) == CommandTargetStatus.Handled));
		}
	}
}
