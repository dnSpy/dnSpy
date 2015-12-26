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
using dnlib.DotNet;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.AsmEditor.Event {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager, DeleteEventDefCommand.EditMenuCommand removeCmd, DeleteEventDefCommand.CodeCommand removeCmd2, EventDefSettingsCommand.EditMenuCommand settingsCmd, EventDefSettingsCommand.CodeCommand settingsCmd2) {
			wpfCommandManager.AddRemoveCommand(removeCmd);
			wpfCommandManager.AddRemoveCommand(removeCmd2, fileTabManager);
			wpfCommandManager.AddSettingsCommand(fileTabManager, settingsCmd, settingsCmd2);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteEventDefCommand : IUndoCommand {
		const string CMD_NAME = "Delete Event";
		[ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_FILES_ASMED_DELETE, Order = 60)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsVisible(AsmEditorContext context) {
				return DeleteEventDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeleteEventDefCommand.Execute(undoCommandManager, context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeleteEventDefCommand.GetHeader(context.Nodes);
			}
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 60)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeView fileTreeView)
				: base(fileTreeView) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsVisible(AsmEditorContext context) {
				return DeleteEventDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeleteEventDefCommand.Execute(undoCommandManager, context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeleteEventDefCommand.GetHeader(context.Nodes);
			}
		}

		[Export, ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_CODE_ASMED_DELTE, Order = 60)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeView fileTreeView)
				: base(fileTreeView) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					DeleteEventDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				DeleteEventDefCommand.Execute(undoCommandManager, context.Nodes);
			}

			public override string GetHeader(CodeContext context) {
				return DeleteEventDefCommand.GetHeader(context.Nodes);
			}
		}

		static string GetHeader(IFileTreeNodeData[] nodes) {
			if (nodes.Length == 1)
				return string.Format("Delete {0}", UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format("Delete {0} events", nodes.Length);
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is IEventNode);
		}

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var eventNodes = nodes.Cast<IEventNode>().ToArray();
			undoCommandManager.Value.Add(new DeleteEventDefCommand(eventNodes));
		}

		struct DeleteModelNodes {
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

			public void Delete(IEventNode[] nodes) {
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

			public void Restore(IEventNode[] nodes) {
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

		DeletableNodes<IEventNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteEventDefCommand(IEventNode[] eventNodes) {
			this.nodes = new DeletableNodes<IEventNode>(eventNodes);
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
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateEventDefCommand : IUndoCommand {
		const string CMD_NAME = "Create Event";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewEvent", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 90)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) {
				return CreateEventDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateEventDefCommand.Execute(undoCommandManager, appWindow, context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewEvent", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 90)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) {
				return CreateEventDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateEventDefCommand.Execute(undoCommandManager, appWindow, context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewEvent", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 90)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					context.Nodes.Length == 1 &&
					context.Nodes[0] is ITypeNode;
			}

			public override void Execute(CodeContext context) {
				CreateEventDefCommand.Execute(undoCommandManager, appWindow, context.Nodes);
			}
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) {
			return nodes.Length == 1 &&
				(nodes[0] is ITypeNode || (nodes[0].TreeNode.Parent != null && nodes[0].TreeNode.Parent.Data is ITypeNode));
		}

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var ownerNode = nodes[0];
			if (!(ownerNode is ITypeNode))
				ownerNode = (IFileTreeNodeData)ownerNode.TreeNode.Parent.Data;
			var typeNode = ownerNode as ITypeNode;
			Debug.Assert(typeNode != null);
			if (typeNode == null)
				throw new InvalidOperationException();

			var module = typeNode.GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();
			var options = EventDefOptions.Create("MyEvent", module.CorLibTypes.GetTypeRef("System", "EventHandler"));

			var data = new EventOptionsVM(options, module, appWindow.LanguageManager, typeNode.TypeDef);
			var win = new EventOptionsDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateEventDefCommand(typeNode, data.CreateEventDefOptions());
			undoCommandManager.Value.Add(cmd);
			appWindow.FileTabManager.FollowReference(cmd.eventNode);
		}

		readonly ITypeNode ownerNode;
		readonly IEventNode eventNode;

		CreateEventDefCommand(ITypeNode ownerNode, EventDefOptions options) {
			this.ownerNode = ownerNode;
			this.eventNode = ownerNode.Create(options.CreateEventDef(ownerNode.TypeDef.Module));
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			ownerNode.TreeNode.EnsureChildrenLoaded();
			ownerNode.TypeDef.Events.Add(eventNode.EventDef);
			ownerNode.TreeNode.AddChild(eventNode.TreeNode);
		}

		public void Undo() {
			bool b = ownerNode.TreeNode.Children.Remove(eventNode.TreeNode) &&
					ownerNode.TypeDef.Events.Remove(eventNode.EventDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return ownerNode; }
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class EventDefSettingsCommand : IUndoCommand {
		const string CMD_NAME = "Edit Event";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 70)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) {
				return EventDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				EventDefSettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
			}
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 70)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) {
				return EventDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				EventDefSettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
			}
		}

		[Export, ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_CODE_ASMED_SETTINGS, Order = 70)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsEnabled(CodeContext context) {
				return EventDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				EventDefSettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
			}
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is IEventNode;
		}

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var eventNode = (IEventNode)nodes[0];

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new EventOptionsVM(new EventDefOptions(eventNode.EventDef), module, appWindow.LanguageManager, eventNode.EventDef.DeclaringType);
			var win = new EventOptionsDlg();
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandManager.Value.Add(new EventDefSettingsCommand(eventNode, data.CreateEventDefOptions()));
		}

		readonly IEventNode eventNode;
		readonly EventDefOptions newOptions;
		readonly EventDefOptions origOptions;
		readonly IFileTreeNodeData origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;

		EventDefSettingsCommand(IEventNode eventNode, EventDefOptions options) {
			this.eventNode = eventNode;
			this.newOptions = options;
			this.origOptions = new EventDefOptions(eventNode.EventDef);

			this.origParentNode = (IFileTreeNodeData)eventNode.TreeNode.Parent.Data;
			this.origParentChildIndex = this.origParentNode.TreeNode.Children.IndexOf(eventNode.TreeNode);
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
				bool b = origParentChildIndex < origParentNode.TreeNode.Children.Count && origParentNode.TreeNode.Children[origParentChildIndex] == eventNode.TreeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.TreeNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(eventNode.EventDef);

				origParentNode.TreeNode.AddChild(eventNode.TreeNode);
			}
			else
				newOptions.CopyTo(eventNode.EventDef);
			eventNode.TreeNode.RefreshUI();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = origParentNode.TreeNode.Children.Remove(eventNode.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(eventNode.EventDef);
				origParentNode.TreeNode.Children.Insert(origParentChildIndex, eventNode.TreeNode);
			}
			else
				origOptions.CopyTo(eventNode.EventDef);
			eventNode.TreeNode.RefreshUI();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return eventNode; }
		}
	}
}
