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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.Event {
	[Export(typeof(IPlugin))]
	sealed class AssemblyPlugin : IPlugin {
		public void OnLoaded() {
			MainWindow.Instance.treeView.AddCommandBinding(ApplicationCommands.Delete, new TreeViewCommandProxy(new DeleteEventDefCommand.TheEditCommand()));
			MainWindow.Instance.CodeBindings.Add(EditingCommands.Delete, new TextEditorCommandProxy(new DeleteEventDefCommand.TheTextEditorCommand()), ModifierKeys.None, Key.Delete);
			Utils.InstallSettingsCommand(new EventDefSettingsCommand.TheEditCommand(), new EventDefSettingsCommand.TheTextEditorCommand());
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteEventDefCommand : IUndoCommand {
		const string CMD_NAME = "Delete Event";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Delete",
								InputGestureText = "Del",
								Category = "AsmEd",
								Order = 360)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							Menu = "_Edit",
							MenuIcon = "Delete",
							MenuInputGestureText = "Del",
							MenuCategory = "AsmEd",
							MenuOrder = 2160)]
		internal sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return DeleteEventDefCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				DeleteEventDefCommand.Execute(nodes);
			}

			protected override void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
				DeleteEventDefCommand.Initialize(nodes, menuItem);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Delete",
								InputGestureText = "Del",
								Category = "AsmEd",
								Order = 360)]
		internal sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					DeleteEventDefCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				DeleteEventDefCommand.Execute(ctx.Nodes);
			}

			protected override void Initialize(Context ctx, MenuItem menuItem) {
				DeleteEventDefCommand.Initialize(ctx.Nodes, menuItem);
			}
		}

		static void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
			if (nodes.Length == 1)
				menuItem.Header = string.Format("Delete {0}", UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
			else
				menuItem.Header = string.Format("Delete {0} events", nodes.Length);
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is EventTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var eventNodes = nodes.Select(a => (EventTreeNode)a).ToArray();
			UndoCommandManager.Instance.Add(new DeleteEventDefCommand(eventNodes));
		}

		public struct DeleteModelNodes {
			ModelInfo[] infos;

			struct ModelInfo {
				public readonly TypeDef OwnerType;
				public readonly int EventIndex;
				public readonly int[] MethodIndexes;
				public readonly MethodDef[] Methods;

				public ModelInfo(EventDef evt) {
					this.OwnerType = evt.DeclaringType;
					this.EventIndex = this.OwnerType.Events.IndexOf(evt);
					Debug.Assert(this.EventIndex >= 0);
					this.Methods = new HashSet<MethodDef>(GetMethods(evt)).ToArray();
					this.MethodIndexes = new int[this.Methods.Length];
				}

				static IEnumerable<MethodDef> GetMethods(EventDef evt) {
					if (evt.AddMethod != null)
						yield return evt.AddMethod;
					if (evt.InvokeMethod != null)
						yield return evt.InvokeMethod;
					if (evt.RemoveMethod != null)
						yield return evt.RemoveMethod;
					foreach (var m in evt.OtherMethods)
						yield return m;
				}
			}

			public void Delete(EventTreeNode[] nodes) {
				Debug.Assert(infos == null);
				if (infos != null)
					throw new InvalidOperationException();

				infos = new ModelInfo[nodes.Length];

				for (int i = 0; i < infos.Length; i++) {
					var node = nodes[i];

					var info = new ModelInfo(node.EventDef);
					infos[i] = info;
					info.OwnerType.Events.RemoveAt(info.EventIndex);

					for (int j = 0; j < info.Methods.Length; j++) {
						int index = info.OwnerType.Methods.IndexOf(info.Methods[j]);
						Debug.Assert(index >= 0);
						if (index < 0)
							throw new InvalidOperationException();
						info.OwnerType.Methods.RemoveAt(index);
						info.MethodIndexes[j] = index;
					}
				}
			}

			public void Restore(EventTreeNode[] nodes) {
				Debug.Assert(infos != null);
				if (infos == null)
					throw new InvalidOperationException();
				Debug.Assert(infos.Length == nodes.Length);
				if (infos.Length != nodes.Length)
					throw new InvalidOperationException();

				for (int i = infos.Length - 1; i >= 0; i--) {
					var node = nodes[i];
					var info = infos[i];
					info.OwnerType.Events.Insert(info.EventIndex, node.EventDef);

					for (int j = info.Methods.Length - 1; j >= 0; j--)
						info.OwnerType.Methods.Insert(info.MethodIndexes[j], info.Methods[j]);
				}

				infos = null;
			}
		}

		DeletableNodes<EventTreeNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteEventDefCommand(EventTreeNode[] eventNodes) {
			this.nodes = new DeletableNodes<EventTreeNode>(eventNodes);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			nodes.Delete();
			modelNodes.Delete(nodes.Nodes);
		}

		public void Undo() {
			modelNodes.Restore(nodes.Nodes);
			nodes.Restore();
		}

		public IEnumerable<object> ModifiedObjects {
			get { return nodes.Nodes; }
		}

		public void Dispose() {
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateEventDefCommand : IUndoCommand {
		const string CMD_NAME = "Create Event";
		[ExportContextMenuEntry(Header = CMD_NAME + "...",
								Icon = "NewEvent",
								Category = "AsmEd",
								Order = 590)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "...",
							Menu = "_Edit",
							MenuIcon = "NewEvent",
							MenuCategory = "AsmEd",
							MenuOrder = 2390)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateEventDefCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateEventDefCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "...",
								Icon = "NewEvent",
								Category = "AsmEd",
								Order = 590)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					ctx.Nodes.Length == 1 &&
					ctx.Nodes[0] is TypeTreeNode;
			}

			protected override void Execute(Context ctx) {
				CreateEventDefCommand.Execute(ctx.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				(nodes[0] is TypeTreeNode || nodes[0].Parent is TypeTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var ownerNode = nodes[0];
			if (!(ownerNode is TypeTreeNode))
				ownerNode = (ILSpyTreeNode)ownerNode.Parent;
			var typeNode = ownerNode as TypeTreeNode;
			Debug.Assert(typeNode != null);
			if (typeNode == null)
				throw new InvalidOperationException();

			var module = ILSpyTreeNode.GetModule(typeNode);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();
			var options = EventDefOptions.Create("MyEvent", module.CorLibTypes.GetTypeRef("System", "EventHandler"));

			var data = new EventOptionsVM(options, module, MainWindow.Instance.CurrentLanguage, typeNode.TypeDef);
			var win = new EventOptionsDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateEventDefCommand(typeNode, data.CreateEventDefOptions());
			UndoCommandManager.Instance.Add(cmd);
			MainWindow.Instance.JumpToReference(cmd.eventNode);
		}

		readonly TypeTreeNode ownerNode;
		readonly EventTreeNode eventNode;

		CreateEventDefCommand(TypeTreeNode ownerNode, EventDefOptions options) {
			this.ownerNode = ownerNode;
			this.eventNode = new EventTreeNode(options.CreateEventDef(ownerNode.TypeDef.Module));
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			ownerNode.EnsureChildrenFiltered();
			ownerNode.TypeDef.Events.Add(eventNode.EventDef);
			ownerNode.AddToChildren(eventNode);
		}

		public void Undo() {
			bool b = ownerNode.Children.Remove(eventNode) &&
					ownerNode.TypeDef.Events.Remove(eventNode.EventDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return ownerNode; }
		}

		public void Dispose() {
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class EventDefSettingsCommand : IUndoCommand {
		const string CMD_NAME = "Edit Event";
		[ExportContextMenuEntry(Header = CMD_NAME + "...",
								Icon = "Settings",
								InputGestureText = "Alt+Enter",
								Category = "AsmEd",
								Order = 670)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "...",
							Menu = "_Edit",
							MenuIcon = "Settings",
							MenuInputGestureText = "Alt+Enter",
							MenuCategory = "AsmEd",
							MenuOrder = 2470)]
		internal sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return EventDefSettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				EventDefSettingsCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "...",
								Icon = "Settings",
								InputGestureText = "Alt+Enter",
								Category = "AsmEd",
								Order = 670)]
		internal sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return EventDefSettingsCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				EventDefSettingsCommand.Execute(ctx.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is EventTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var eventNode = (EventTreeNode)nodes[0];

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new EventOptionsVM(new EventDefOptions(eventNode.EventDef), module, MainWindow.Instance.CurrentLanguage, eventNode.EventDef.DeclaringType);
			var win = new EventOptionsDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new EventDefSettingsCommand(eventNode, data.CreateEventDefOptions()));
		}

		readonly EventTreeNode eventNode;
		readonly EventDefOptions newOptions;
		readonly EventDefOptions origOptions;
		readonly ILSpyTreeNode origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;

		EventDefSettingsCommand(EventTreeNode eventNode, EventDefOptions options) {
			this.eventNode = eventNode;
			this.newOptions = options;
			this.origOptions = new EventDefOptions(eventNode.EventDef);

			this.origParentNode = (ILSpyTreeNode)eventNode.Parent;
			this.origParentChildIndex = this.origParentNode.Children.IndexOf(eventNode);
			Debug.Assert(this.origParentChildIndex >= 0);
			if (this.origParentChildIndex < 0)
				throw new InvalidOperationException();

			this.nameChanged = origOptions.Name != newOptions.Name;
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.Children.Count && origParentNode.Children[origParentChildIndex] == eventNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(eventNode.EventDef);

				origParentNode.AddToChildren(eventNode);
			}
			else
				newOptions.CopyTo(eventNode.EventDef);
			eventNode.RaiseUIPropsChanged();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = origParentNode.Children.Remove(eventNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(eventNode.EventDef);
				origParentNode.Children.Insert(origParentChildIndex, eventNode);
			}
			else
				origOptions.CopyTo(eventNode.EventDef);
			eventNode.RaiseUIPropsChanged();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return eventNode; }
		}

		public void Dispose() {
		}
	}
}
