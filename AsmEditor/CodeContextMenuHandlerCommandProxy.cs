/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Windows.Input;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor {
	sealed class CodeContextMenuHandlerCommandProxy : ICommand {
		readonly CodeContextMenuHandler command;

		public CodeContextMenuHandlerCommandProxy(CodeContextMenuHandler command) {
			this.command = command;
		}

		CodeContext CreateContext() {
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView == null || !textView.IsKeyboardFocusWithin)
				return null;

			var position = textView.TextEditor.TextArea.Caret.Position;
			var refSeg = textView.GetReferenceSegmentAt(position);
			if (refSeg == null)
				return null;

			var node = MainWindow.Instance.FindTreeNode(refSeg.Reference);
			var nodes = node == null ? new ILSpyTreeNode[0] : new ILSpyTreeNode[] { node };
			return new CodeContext(nodes, refSeg.IsLocalTarget);
		}

		event EventHandler ICommand.CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		bool ICommand.CanExecute(object parameter) {
			var ctx = CreateContext();
			return command.IsVisible(ctx) && command.IsEnabled(ctx);
		}

		void ICommand.Execute(object parameter) {
			var ctx = CreateContext();
			command.Execute(ctx);
		}
	}
}
