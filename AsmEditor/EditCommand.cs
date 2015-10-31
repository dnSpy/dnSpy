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

using System.Linq;
using System.Windows.Controls;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace dnSpy.AsmEditor {
	public abstract class EditCommand : TreeNodeCommand, IMainMenuCommand, IMainMenuCommandInitialize, IContextMenuEntry2 {
		static EditCommand() {
			MainWindow.Instance.SetMenuAlwaysRegenerate("_Edit");
		}

		protected EditCommand()
			: this(false) {
		}

		protected EditCommand(bool canAcceptEmptySelectedNodes)
			: base(canAcceptEmptySelectedNodes) {
		}

		protected virtual bool IsVisible(ILSpyTreeNode[] nodes) {
			return CanExecute(nodes);
		}

		protected virtual void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
		}

		bool IMainMenuCommand.IsVisible {
			get { return IsVisible(GetSelectedNodes()); }
		}

		void IMainMenuCommandInitialize.Initialize(MenuItem menuItem) {
			Initialize(GetSelectedNodes(), menuItem);
		}

		static ILSpyTreeNode[] GetILSpyTreeNodes(SharpTreeNode[] nodes) {
			return nodes.OfType<ILSpyTreeNode>().ToArray();
		}

		bool IContextMenuEntry<ContextMenuEntryContext>.IsVisible(ContextMenuEntryContext context) {
			return context.Element == MainWindow.Instance.TreeView &&
				context.SelectedTreeNodes != null &&
				IsVisible(GetILSpyTreeNodes(context.SelectedTreeNodes));
		}

		bool IContextMenuEntry<ContextMenuEntryContext>.IsEnabled(ContextMenuEntryContext context) {
			return context.Element == MainWindow.Instance.TreeView &&
				context.SelectedTreeNodes != null &&
				CanExecute(GetILSpyTreeNodes(context.SelectedTreeNodes));
		}

		void IContextMenuEntry<ContextMenuEntryContext>.Execute(ContextMenuEntryContext context) {
			if (context.Element == MainWindow.Instance.TreeView && context.SelectedTreeNodes != null)
				Execute(GetILSpyTreeNodes(context.SelectedTreeNodes));
		}

		void IContextMenuEntry2<ContextMenuEntryContext>.Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			if (context.Element == MainWindow.Instance.TreeView && context.SelectedTreeNodes != null)
				Initialize(GetILSpyTreeNodes(context.SelectedTreeNodes), menuItem);
		}
	}
}
