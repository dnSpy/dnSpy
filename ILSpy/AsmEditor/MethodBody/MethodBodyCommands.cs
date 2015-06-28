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
using System.Collections.Generic;
using System.Diagnostics;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor.MethodBody
{
	sealed class MethodBodySettingsCommand : IUndoCommand
	{
		const string CMD_NAME = "Edit Method Body";
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "ILEditor",
								Category = "AsmEd",
								Order = 640)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "ILEditor",
							MenuCategory = "AsmEd",
							MenuOrder = 2440)]
		sealed class TheEditCommand : EditCommand
		{
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes)
			{
				return MethodBodySettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes)
			{
				MethodBodySettingsCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "ILEditor",
								Category = "AsmEd",
								Order = 640)]
		sealed class TheTextEditorCommand : TextEditorCommand
		{
			protected override bool CanExecute(Context ctx)
			{
				return ctx.ReferenceSegment.IsLocalTarget &&
					MethodBodySettingsCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx)
			{
				MethodBodySettingsCommand.Execute(ctx.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes)
		{
			return nodes.Length == 1 &&
				nodes[0] is MethodTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes)
		{
			if (!CanExecute(nodes))
				return;

			var methodNode = (MethodTreeNode)nodes[0];

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new MethodBodyVM(new MethodBodyOptions(methodNode.MethodDefinition), module, MainWindow.Instance.CurrentLanguage, methodNode.MethodDefinition.DeclaringType, methodNode.MethodDefinition);
			var win = new MethodBodyDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			win.Title = string.Format("{0} - {1}", win.Title, methodNode.ToString());
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new MethodBodySettingsCommand(methodNode, data.CreateMethodBodyOptions()));
		}

		readonly MethodTreeNode methodNode;
		readonly MethodBodyOptions newOptions;
		readonly MethodBodyOptions origOptions;
		bool isBodyModified;

		MethodBodySettingsCommand(MethodTreeNode methodNode, MethodBodyOptions options)
		{
			this.methodNode = methodNode;
			this.newOptions = options;
			this.origOptions = new MethodBodyOptions(methodNode.MethodDefinition);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute()
		{
			isBodyModified = MethodAnnotations.Instance.IsBodyModified(methodNode.MethodDefinition);
			MethodAnnotations.Instance.SetBodyModified(methodNode.MethodDefinition, true);
			newOptions.CopyTo(methodNode.MethodDefinition);
		}

		public void Undo()
		{
			origOptions.CopyTo(methodNode.MethodDefinition);
			MethodAnnotations.Instance.SetBodyModified(methodNode.MethodDefinition, isBodyModified);
		}

		public IEnumerable<ILSpyTreeNode> TreeNodes {
			get { yield return methodNode; }
		}

		public void Dispose()
		{
		}
	}
}
