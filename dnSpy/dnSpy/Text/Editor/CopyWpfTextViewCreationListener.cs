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
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewCreationListener))]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	sealed class CopyWpfTextViewCreationListener : IWpfTextViewCreationListener {
		readonly IEditorOperationsFactoryService editorOperationsFactoryService;

		[ImportingConstructor]
		CopyWpfTextViewCreationListener(IEditorOperationsFactoryService editorOperationsFactoryService) {
			this.editorOperationsFactoryService = editorOperationsFactoryService;
		}

		public void TextViewCreated(IWpfTextView textView) {
			var editorOperations = editorOperationsFactoryService.GetEditorOperations(textView);
			textView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, (s, e) => editorOperations.CutSelection(), (s, e) => e.CanExecute = editorOperations.CanCut));
			textView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (s, e) => editorOperations.CopySelection(), (s, e) => e.CanExecute = true));
			textView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (s, e) => editorOperations.Paste(), (s, e) => e.CanExecute = editorOperations.CanPaste));
			textView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, (s, e) => editorOperations.SelectAll(), (s, e) => e.CanExecute = true));
		}
	}
}
