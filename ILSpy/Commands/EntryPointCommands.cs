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

using dnlib.DotNet;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.Commands
{
	[ExportContextMenuEntryAttribute(Header = "Go to _Entry Point", Order = 450, Category = "EP")]
	sealed class GoToEntryPointCommand : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return TreeView_IsVisible(context) ||
				TextEditor_IsVisible(context);
		}

		static bool TreeView_IsVisible(TextViewContext context)
		{
			ModuleDef module;
			return context.TreeView == MainWindow.Instance.treeView &&
				((module = ILSpyTreeNode.GetModule(context.SelectedTreeNodes)) != null) &&
				module.EntryPoint is MethodDef;
		}

		static bool TextEditor_IsVisible(TextViewContext context)
		{
			ModuleDef module;
			return context.TextView != null &&
				(module = GetModule()) != null &&
				module.EntryPoint is MethodDef;
		}

		internal static ModuleDef GetModule()
		{
			var tabState = MainWindow.Instance.GetActiveDecompileTabState();
			if (tabState == null)
				return null;
			return ILSpyTreeNode.GetModule(tabState.DecompiledNodes) as ModuleDef;
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			if (TreeView_IsVisible(context))
				MainWindow.Instance.JumpToReference(ILSpyTreeNode.GetModule(context.SelectedTreeNodes).EntryPoint);
			else if (TextEditor_IsVisible(context))
				MainWindow.Instance.JumpToReference(GetModule().EntryPoint);
		}
	}

	[ExportContextMenuEntryAttribute(Header = "Go to <Module> .ccto_r", Order = 460, Category = "EP")]
	sealed class GoToGlobalTypeCctorCommand : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return TreeView_IsVisible(context) ||
				TextEditor_IsVisible(context);
		}

		static bool TreeView_IsVisible(TextViewContext context)
		{
			ModuleDef module;
			return context.TreeView == MainWindow.Instance.treeView &&
				((module = ILSpyTreeNode.GetModule(context.SelectedTreeNodes)) != null) &&
				module.GlobalType != null &&
				module.GlobalType.FindStaticConstructor() != null;
		}

		static bool TextEditor_IsVisible(TextViewContext context)
		{
			ModuleDef module;
			return context.TextView != null &&
				(module = GoToEntryPointCommand.GetModule()) != null &&
				module.GlobalType != null &&
				module.GlobalType.FindStaticConstructor() != null;
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			if (TreeView_IsVisible(context))
				MainWindow.Instance.JumpToReference(ILSpyTreeNode.GetModule(context.SelectedTreeNodes).GlobalType.FindStaticConstructor());
			else if (TextEditor_IsVisible(context))
				MainWindow.Instance.JumpToReference(GoToEntryPointCommand.GetModule().GlobalType.FindStaticConstructor());
		}
	}
}
