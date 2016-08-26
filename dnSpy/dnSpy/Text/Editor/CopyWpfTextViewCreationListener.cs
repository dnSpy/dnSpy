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

using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewCreationListener))]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	sealed class CopyWpfTextViewCreationListener : IWpfTextViewCreationListener {
		public void TextViewCreated(IWpfTextView textView) {
			var dnSpyTextView = textView as IDnSpyWpfTextView;
			if (dnSpyTextView == null)
				return;
			textView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, (s, e) => dnSpyTextView.CommandTarget.Execute(CommandConstants.StandardGroup, (int)StandardIds.Cut), (s, e) => e.CanExecute = dnSpyTextView.CommandTarget.CanExecute(CommandConstants.StandardGroup, (int)StandardIds.Cut) == CommandTargetStatus.Handled));
			textView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (s, e) => dnSpyTextView.CommandTarget.Execute(CommandConstants.StandardGroup, (int)StandardIds.Copy), (s, e) => e.CanExecute = dnSpyTextView.CommandTarget.CanExecute(CommandConstants.StandardGroup, (int)StandardIds.Copy) == CommandTargetStatus.Handled));
			textView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (s, e) => dnSpyTextView.CommandTarget.Execute(CommandConstants.StandardGroup, (int)StandardIds.Paste), (s, e) => e.CanExecute = dnSpyTextView.CommandTarget.CanExecute(CommandConstants.StandardGroup, (int)StandardIds.Paste) == CommandTargetStatus.Handled));
			textView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, (s, e) => dnSpyTextView.CommandTarget.Execute(CommandConstants.TextEditorGroup, (int)TextEditorIds.SELECTALL), (s, e) => e.CanExecute = dnSpyTextView.CommandTarget.CanExecute(CommandConstants.TextEditorGroup, (int)TextEditorIds.SELECTALL) == CommandTargetStatus.Handled));
		}
	}
}
