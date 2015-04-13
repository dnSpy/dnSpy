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

using System.Windows;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes {
	abstract class CopyTokenContextMenuEntryBase : IContextMenuEntry
	{
		public virtual bool IsVisible(TextViewContext context)
		{
			return context.Reference != null && context.Reference.Reference is IMemberRef;
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public abstract void Execute(TextViewContext context);

		protected void Execute(IMemberRef member)
		{
			if (member == null) {
				MessageBox.Show(MainWindow.Instance, "Could not resolve member definition");
				return;
			}

			Clipboard.SetText(string.Format("0x{0:X8}", member.MDToken.Raw));
		}
	}

	[ExportContextMenuEntryAttribute(Header = "_Copy Token", Order = 310, Category = "Tokens")]
	class CopyTokenContextMenuEntry : CopyTokenContextMenuEntryBase
	{
		public override bool IsVisible(TextViewContext context)
		{
			if (base.IsVisible(context))
				return true;

			return context.SelectedTreeNodes != null &&
				context.SelectedTreeNodes.Length != 0 &&
				context.SelectedTreeNodes[0] is IMemberTreeNode;
		}

		public override void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length != 0 && context.SelectedTreeNodes[0] is IMemberTreeNode)
				Execute(((IMemberTreeNode)context.SelectedTreeNodes[0]).Member);
			else
				Execute(context.Reference.Reference as IMemberRef);
		}
	}

	[ExportContextMenuEntryAttribute(Header = "Copy _Definition Token", Order = 320, Category = "Tokens")]
	class CopyDefinitionTokenContextMenuEntry : CopyTokenContextMenuEntryBase
	{
		public override bool IsVisible(TextViewContext context)
		{
			if (base.IsVisible(context))
				return true;

			return context.SelectedTreeNodes != null &&
				context.SelectedTreeNodes.Length != 0 &&
				context.SelectedTreeNodes[0] is IMemberTreeNode &&
				((IMemberTreeNode)context.SelectedTreeNodes[0]).Member is IMemberRef &&
				!(((IMemberTreeNode)context.SelectedTreeNodes[0]).Member is IMemberDef);
		}

		public override void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length != 0 && context.SelectedTreeNodes[0] is IMemberTreeNode)
				Execute(MainWindow.ResolveReference(((IMemberTreeNode)context.SelectedTreeNodes[0]).Member));
			else
				Execute(MainWindow.ResolveReference(context.Reference.Reference as IMemberRef));
		}
	}
}
