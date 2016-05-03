using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.Menus;

// Adds a couple of commands to the file treeview context menu.
// Since there are several commands using the same state, MenuItemBase<TContext> is used
// as the base class so the context is created once and shared by all commands.

namespace Example1.Plugin {
	sealed class TVContext {
		public bool SomeValue { get; }
		public IFileTreeNodeData[] Nodes { get; }

		public TVContext(bool someValue, IEnumerable<IFileTreeNodeData> nodes) {
			this.SomeValue = someValue;
			this.Nodes = nodes.ToArray();
		}
	}

	abstract class TVCtxMenuCommand : MenuItemBase<TVContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override TVContext CreateContext(IMenuItemContext context) {
			// Make sure it's the file treeview
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID))
				return null;

			// Extract the data needed by the context
			var nodes = context.Find<ITreeNodeData[]>();
			if (nodes == null)
				return null;
			var newNodes = nodes.OfType<IFileTreeNodeData>();

			bool someValue = true;
			return new TVContext(someValue, newNodes);
		}
	}

	[ExportMenuItem(Header = "Command #1", Group = Constants.GROUP_TREEVIEW, Order = 0)]
	sealed class TVCommand1 : TVCtxMenuCommand {
		public override void Execute(TVContext context) => dnSpy.Shared.App.MsgBox.Instance.Show("Command #1");
		public override bool IsEnabled(TVContext context) => context.Nodes.Length > 1;
	}

	[ExportMenuItem(Header = "Command #2", Group = Constants.GROUP_TREEVIEW, Order = 10)]
	sealed class TVCommand2 : TVCtxMenuCommand {
		public override void Execute(TVContext context) => dnSpy.Shared.App.MsgBox.Instance.Show("Command #2");
		public override bool IsVisible(TVContext context) => context.Nodes.Length > 0;
	}

	[ExportMenuItem(Header = "Command #3", Group = Constants.GROUP_TREEVIEW, Order = 20)]
	sealed class TVCommand3 : TVCtxMenuCommand {
		public override void Execute(TVContext context) {
			int secretNum = new Random().Next() % 10;
			dnSpy.Shared.App.MsgBox.Instance.Ask<int?>("Number", null, "Guess a number", null, s => {
				int num;
				if (string.IsNullOrWhiteSpace(s))
					return "Enter a number";
				if (!int.TryParse(s, out num))
					return "Not an integer";
				if (num == 42)
					return "Nope!";
				if (num != secretNum)
					return "WRONG!!!";
				return string.Empty;
			});
		}
	}

	[ExportMenuItem(Header = "Command #4", Group = Constants.GROUP_TREEVIEW, Order = 30)]
	sealed class TVCommand4 : TVCtxMenuCommand {
		public override void Execute(TVContext context) => dnSpy.Shared.App.MsgBox.Instance.Show("Command #4");
		public override bool IsEnabled(TVContext context) => context.Nodes.Length == 1 && context.Nodes[0] is IModuleFileNode;
	}

	[ExportMenuItem(Group = Constants.GROUP_TREEVIEW, Order = 40)]
	sealed class TVCommand5 : TVCtxMenuCommand {
		public override void Execute(TVContext context) {
			var node = GetTokenNode(context);
			if (node != null) {
				try {
					Clipboard.SetText(string.Format("{0:X8}", node.Reference.MDToken.Raw));
				}
				catch (ExternalException) { }
			}
		}

		IMDTokenNode GetTokenNode(TVContext context) {
			if (context.Nodes.Length == 0)
				return null;
			return context.Nodes[0] as IMDTokenNode;
		}

		public override string GetHeader(TVContext context) {
			var node = GetTokenNode(context);
			if (node == null)
				return string.Empty;
			return string.Format("Copy token {0:X8}", node.Reference.MDToken.Raw);
		}

		public override bool IsVisible(TVContext context) => GetTokenNode(context) != null;
	}

	[ExportMenuItem(Header = "Copy Second Instruction", Group = Constants.GROUP_TREEVIEW, Order = 50)]
	sealed class TVCommand6 : TVCtxMenuCommand {
		public override void Execute(TVContext context) {
			var instr = GetSecondInstruction(context);
			if (instr != null) {
				try {
					Clipboard.SetText(string.Format("Second instruction: {0}", instr));
				}
				catch (ExternalException) { }
			}
		}

		Instruction GetSecondInstruction(TVContext context) {
			if (context.Nodes.Length == 0)
				return null;
			var methNode = context.Nodes[0] as IMethodNode;
			if (methNode == null)
				return null;
			var body = methNode.MethodDef.Body;
			if (body == null || body.Instructions.Count < 2)
				return null;
			return body.Instructions[1];
		}

		public override bool IsEnabled(TVContext context) => GetSecondInstruction(context) != null;
	}
}
