/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Event {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, IDocumentTabService documentTabService, DeleteEventDefCommand.EditMenuCommand removeCmd, DeleteEventDefCommand.CodeCommand removeCmd2, EventDefSettingsCommand.EditMenuCommand settingsCmd, EventDefSettingsCommand.CodeCommand settingsCmd2) {
			wpfCommandService.AddRemoveCommand(removeCmd);
			wpfCommandService.AddRemoveCommand(removeCmd2, documentTabService);
			wpfCommandService.AddSettingsCommand(documentTabService, settingsCmd, settingsCmd2);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteEventDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:DeleteEventCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_DELETE, Order = 60)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => DeleteEventDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteEventDefCommand.Execute(undoCommandService, context.Nodes);
			public override string? GetHeader(AsmEditorContext context) => DeleteEventDefCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:DeleteEventCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 60)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => DeleteEventDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteEventDefCommand.Execute(undoCommandService, context.Nodes);
			public override string? GetHeader(AsmEditorContext context) => DeleteEventDefCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:DeleteEventCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_DELETE, Order = 60)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && DeleteEventDefCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => DeleteEventDefCommand.Execute(undoCommandService, context.Nodes);
			public override string? GetHeader(CodeContext context) => DeleteEventDefCommand.GetHeader(context.Nodes);
		}

		static string GetHeader(DocumentTreeNodeData[] nodes) {
			if (nodes.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.DeleteX, UIUtilities.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format(dnSpy_AsmEditor_Resources.DeleteEvents, nodes.Length);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length > 0 && nodes.All(n => n is EventNode);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var eventNodes = nodes.Cast<EventNode>().ToArray();
			undoCommandService.Value.Add(new DeleteEventDefCommand(eventNodes));
		}

		struct DeleteModelNodes {
			ModelInfo[]? infos;

			readonly struct ModelInfo {
				public readonly TypeDef OwnerType;
				public readonly int EventIndex;
				public readonly int[] MethodIndexes;
				public readonly MethodDef[] Methods;

				public ModelInfo(EventDef evt) {
					OwnerType = evt.DeclaringType;
					EventIndex = OwnerType.Events.IndexOf(evt);
					Debug.Assert(EventIndex >= 0);
					Methods = new HashSet<MethodDef>(GetMethods(evt)).ToArray();
					MethodIndexes = new int[Methods.Length];
				}

				static IEnumerable<MethodDef> GetMethods(EventDef evt) {
					if (evt.AddMethod is not null)
						yield return evt.AddMethod;
					if (evt.InvokeMethod is not null)
						yield return evt.InvokeMethod;
					if (evt.RemoveMethod is not null)
						yield return evt.RemoveMethod;
					foreach (var m in evt.OtherMethods)
						yield return m;
				}
			}

			public void Delete(EventNode[] nodes) {
				Debug2.Assert(infos is null);
				if (infos is not null)
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

			public void Restore(EventNode[] nodes) {
				Debug2.Assert(infos is not null);
				if (infos is null)
					throw new InvalidOperationException();
				Debug.Assert(infos.Length == nodes.Length);
				if (infos.Length != nodes.Length)
					throw new InvalidOperationException();

				for (int i = infos.Length - 1; i >= 0; i--) {
					var node = nodes[i];
					ref readonly var info = ref infos[i];
					info.OwnerType.Events.Insert(info.EventIndex, node.EventDef);

					for (int j = info.Methods.Length - 1; j >= 0; j--)
						info.OwnerType.Methods.Insert(info.MethodIndexes[j], info.Methods[j]);
				}

				infos = null;
			}
		}

		DeletableNodes<EventNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteEventDefCommand(EventNode[] eventNodes) => nodes = new DeletableNodes<EventNode>(eventNodes);

		public string Description => dnSpy_AsmEditor_Resources.DeleteEventCommand;

		public void Execute() {
			nodes.Delete();
			modelNodes.Delete(nodes.Nodes);
		}

		public void Undo() {
			modelNodes.Restore(nodes.Nodes);
			nodes.Restore();
		}

		public IEnumerable<object> ModifiedObjects => nodes.Nodes;
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateEventDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:CreateEventCommand", Icon = DsImagesAttribute.NewEvent, Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 90)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateEventDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateEventDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateEventCommand", Icon = DsImagesAttribute.NewEvent, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 90)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateEventDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateEventDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateEventCommand", Icon = DsImagesAttribute.NewEvent, Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 90)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) =>
				context.IsDefinition &&
				context.Nodes.Length == 1 &&
				context.Nodes[0] is TypeNode;

			public override void Execute(CodeContext context) => CreateEventDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 &&
			(nodes[0] is TypeNode || (nodes[0].TreeNode.Parent is not null && nodes[0].TreeNode.Parent!.Data is TypeNode));

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var ownerNode = nodes[0];
			if (!(ownerNode is TypeNode))
				ownerNode = (DocumentTreeNodeData)ownerNode.TreeNode.Parent!.Data;
			var typeNode = ownerNode as TypeNode;
			Debug2.Assert(typeNode is not null);
			if (typeNode is null)
				throw new InvalidOperationException();

			var module = typeNode.GetModule();
			Debug2.Assert(module is not null);
			if (module is null)
				throw new InvalidOperationException();
			var options = EventDefOptions.Create("MyEvent", module.CorLibTypes.GetTypeRef("System", "EventHandler"));

			var data = new EventOptionsVM(options, module, appService.DecompilerService, typeNode.TypeDef);
			var win = new EventOptionsDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateEventCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateEventDefCommand(typeNode, data.CreateEventDefOptions());
			undoCommandService.Value.Add(cmd);
			appService.DocumentTabService.FollowReference(cmd.eventNode);
		}

		readonly TypeNode ownerNode;
		readonly EventNode eventNode;

		CreateEventDefCommand(TypeNode ownerNode, EventDefOptions options) {
			this.ownerNode = ownerNode;
			eventNode = ownerNode.Create(options.CreateEventDef(ownerNode.TypeDef.Module));
		}

		public string Description => dnSpy_AsmEditor_Resources.CreateEventCommand2;

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
		[ExportMenuItem(Header = "res:EditEventCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 70)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => EventDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => EventDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditEventCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 70)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => EventDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => EventDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:EditEventCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SETTINGS, Order = 70)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) => EventDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => EventDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length == 1 && nodes[0] is EventNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var eventNode = (EventNode)nodes[0];

			var module = nodes[0].GetModule();
			Debug2.Assert(module is not null);
			if (module is null)
				throw new InvalidOperationException();

			var data = new EventOptionsVM(new EventDefOptions(eventNode.EventDef), module, appService.DecompilerService, eventNode.EventDef.DeclaringType);
			var win = new EventOptionsDlg();
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandService.Value.Add(new EventDefSettingsCommand(eventNode, data.CreateEventDefOptions()));
		}

		readonly EventNode eventNode;
		readonly EventDefOptions newOptions;
		readonly EventDefOptions origOptions;
		readonly DocumentTreeNodeData origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;

		EventDefSettingsCommand(EventNode eventNode, EventDefOptions options) {
			this.eventNode = eventNode;
			newOptions = options;
			origOptions = new EventDefOptions(eventNode.EventDef);

			origParentNode = (DocumentTreeNodeData)eventNode.TreeNode.Parent!.Data;
			origParentChildIndex = origParentNode.TreeNode.Children.IndexOf(eventNode.TreeNode);
			Debug.Assert(origParentChildIndex >= 0);
			if (origParentChildIndex < 0)
				throw new InvalidOperationException();

			nameChanged = origOptions.Name != newOptions.Name;
		}

		public string Description => dnSpy_AsmEditor_Resources.EditEventCommand2;

		public void Execute() {
			if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.TreeNode.Children.Count && origParentNode.TreeNode.Children[origParentChildIndex] == eventNode.TreeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				var isNodeSelected = eventNode.TreeNode.TreeView.SelectedItem == eventNode;

				origParentNode.TreeNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(eventNode.EventDef);
				origParentNode.TreeNode.AddChild(eventNode.TreeNode);

				if (isNodeSelected)
					origParentNode.TreeNode.TreeView.SelectItems(new[] { eventNode });
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
				origParentNode.TreeNode.TreeView.SelectItems(new[] { eventNode });
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
