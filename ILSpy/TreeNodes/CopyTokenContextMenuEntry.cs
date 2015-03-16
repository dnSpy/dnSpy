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
