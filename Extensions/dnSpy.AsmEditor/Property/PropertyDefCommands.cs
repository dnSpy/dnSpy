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

namespace dnSpy.AsmEditor.Property {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, IDocumentTabService documentTabService, DeletePropertyDefCommand.EditMenuCommand removeCmd, DeletePropertyDefCommand.CodeCommand removeCmd2, PropertyDefSettingsCommand.EditMenuCommand settingsCmd, PropertyDefSettingsCommand.CodeCommand settingsCmd2) {
			wpfCommandService.AddRemoveCommand(removeCmd);
			wpfCommandService.AddRemoveCommand(removeCmd2, documentTabService);
			wpfCommandService.AddSettingsCommand(documentTabService, settingsCmd, settingsCmd2);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeletePropertyDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:DeletePropertyCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_DELETE, Order = 50)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => DeletePropertyDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeletePropertyDefCommand.Execute(undoCommandService, context.Nodes);
			public override string? GetHeader(AsmEditorContext context) => DeletePropertyDefCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:DeletePropertyCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 50)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => DeletePropertyDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeletePropertyDefCommand.Execute(undoCommandService, context.Nodes);
			public override string? GetHeader(AsmEditorContext context) => DeletePropertyDefCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:DeletePropertyCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_DELETE, Order = 50)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && DeletePropertyDefCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => DeletePropertyDefCommand.Execute(undoCommandService, context.Nodes);
			public override string? GetHeader(CodeContext context) => DeletePropertyDefCommand.GetHeader(context.Nodes);
		}

		static string GetHeader(DocumentTreeNodeData[] nodes) {
			if (nodes.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.DeleteX, UIUtilities.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format(dnSpy_AsmEditor_Resources.DeletePropertiesCommand, nodes.Length);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length > 0 && nodes.All(n => n is PropertyNode);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var propNodes = nodes.Cast<PropertyNode>().ToArray();
			undoCommandService.Value.Add(new DeletePropertyDefCommand(propNodes));
		}

		struct DeleteModelNodes {
			ModelInfo[]? infos;

			readonly struct ModelInfo {
				public readonly TypeDef OwnerType;
				public readonly int PropertyIndex;
				public readonly int[] MethodIndexes;
				public readonly MethodDef[] Methods;

				public ModelInfo(PropertyDef evt) {
					OwnerType = evt.DeclaringType;
					PropertyIndex = OwnerType.Properties.IndexOf(evt);
					Debug.Assert(PropertyIndex >= 0);
					Methods = new HashSet<MethodDef>(GetMethods(evt)).ToArray();
					MethodIndexes = new int[Methods.Length];
				}

				static IEnumerable<MethodDef> GetMethods(PropertyDef evt) {
					foreach (var m in evt.GetMethods) yield return m;
					foreach (var m in evt.SetMethods) yield return m;
					foreach (var m in evt.OtherMethods) yield return m;
				}
			}

			public void Delete(PropertyNode[] nodes) {
				Debug2.Assert(infos is null);
				if (!(infos is null))
					throw new InvalidOperationException();

				infos = new ModelInfo[nodes.Length];

				for (int i = 0; i < infos.Length; i++) {
					var node = nodes[i];

					var info = new ModelInfo(node.PropertyDef);
					infos[i] = info;
					info.OwnerType.Properties.RemoveAt(info.PropertyIndex);

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

			public void Restore(PropertyNode[] nodes) {
				Debug2.Assert(!(infos is null));
				if (infos is null)
					throw new InvalidOperationException();
				Debug.Assert(infos.Length == nodes.Length);
				if (infos.Length != nodes.Length)
					throw new InvalidOperationException();

				for (int i = infos.Length - 1; i >= 0; i--) {
					var node = nodes[i];
					ref readonly var info = ref infos[i];
					info.OwnerType.Properties.Insert(info.PropertyIndex, node.PropertyDef);

					for (int j = info.Methods.Length - 1; j >= 0; j--)
						info.OwnerType.Methods.Insert(info.MethodIndexes[j], info.Methods[j]);
				}

				infos = null;
			}
		}

		DeletableNodes<PropertyNode> nodes;
		DeleteModelNodes modelNodes;

		DeletePropertyDefCommand(PropertyNode[] propNodes) => nodes = new DeletableNodes<PropertyNode>(propNodes);

		public string Description => dnSpy_AsmEditor_Resources.DeletePropertyCommand;

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
	sealed class CreatePropertyDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:CreatePropertyCommand", Icon = DsImagesAttribute.NewProperty, Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 80)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreatePropertyDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreatePropertyDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreatePropertyCommand", Icon = DsImagesAttribute.NewProperty, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 80)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreatePropertyDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreatePropertyDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreatePropertyCommand", Icon = DsImagesAttribute.NewProperty, Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 80)]
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

			public override void Execute(CodeContext context) => CreatePropertyDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 &&
			(nodes[0] is TypeNode || (!(nodes[0].TreeNode.Parent is null) && nodes[0].TreeNode.Parent!.Data is TypeNode));

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var ownerNode = nodes[0];
			if (!(ownerNode is TypeNode))
				ownerNode = (DocumentTreeNodeData)ownerNode.TreeNode.Parent!.Data;
			var typeNode = ownerNode as TypeNode;
			Debug2.Assert(!(typeNode is null));
			if (typeNode is null)
				throw new InvalidOperationException();

			var module = typeNode.GetModule();
			Debug2.Assert(!(module is null));
			if (module is null)
				throw new InvalidOperationException();

			bool isInstance = !(typeNode.TypeDef.IsAbstract && typeNode.TypeDef.IsSealed);
			var options = PropertyDefOptions.Create(module, "MyProperty", isInstance);

			var data = new PropertyOptionsVM(options, module, appService.DecompilerService, typeNode.TypeDef);
			var win = new PropertyOptionsDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreatePropertyCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreatePropertyDefCommand(typeNode, data.CreatePropertyDefOptions());
			undoCommandService.Value.Add(cmd);
			appService.DocumentTabService.FollowReference(cmd.propNode);
		}

		readonly TypeNode ownerNode;
		readonly PropertyNode propNode;

		CreatePropertyDefCommand(TypeNode ownerNode, PropertyDefOptions options) {
			this.ownerNode = ownerNode;
			propNode = ownerNode.Create(options.CreatePropertyDef(ownerNode.TypeDef.Module));
		}

		public string Description => dnSpy_AsmEditor_Resources.CreatePropertyCommand2;

		public void Execute() {
			ownerNode.TreeNode.EnsureChildrenLoaded();
			ownerNode.TypeDef.Properties.Add(propNode.PropertyDef);
			ownerNode.TreeNode.AddChild(propNode.TreeNode);
		}

		public void Undo() {
			bool b = ownerNode.TreeNode.Children.Remove(propNode.TreeNode) &&
					ownerNode.TypeDef.Properties.Remove(propNode.PropertyDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return ownerNode; }
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class PropertyDefSettingsCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:EditPropertyCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 60)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => PropertyDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => PropertyDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditPropertyCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 60)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => PropertyDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => PropertyDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:EditPropertyCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SETTINGS, Order = 60)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) => PropertyDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => PropertyDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length == 1 && nodes[0] is PropertyNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var propNode = (PropertyNode)nodes[0];

			var module = nodes[0].GetModule();
			Debug2.Assert(!(module is null));
			if (module is null)
				throw new InvalidOperationException();

			var data = new PropertyOptionsVM(new PropertyDefOptions(propNode.PropertyDef), module, appService.DecompilerService, propNode.PropertyDef.DeclaringType);
			var win = new PropertyOptionsDlg();
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandService.Value.Add(new PropertyDefSettingsCommand(propNode, data.CreatePropertyDefOptions()));
		}

		readonly PropertyNode propNode;
		readonly PropertyDefOptions newOptions;
		readonly PropertyDefOptions origOptions;
		readonly DocumentTreeNodeData origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;

		PropertyDefSettingsCommand(PropertyNode propNode, PropertyDefOptions options) {
			this.propNode = propNode;
			newOptions = options;
			origOptions = new PropertyDefOptions(propNode.PropertyDef);

			origParentNode = (DocumentTreeNodeData)propNode.TreeNode.Parent!.Data;
			origParentChildIndex = origParentNode.TreeNode.Children.IndexOf(propNode.TreeNode);
			Debug.Assert(origParentChildIndex >= 0);
			if (origParentChildIndex < 0)
				throw new InvalidOperationException();

			nameChanged = origOptions.Name != newOptions.Name;
		}

		public string Description => dnSpy_AsmEditor_Resources.EditPropertyCommand2;

		public void Execute() {
			if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.TreeNode.Children.Count && origParentNode.TreeNode.Children[origParentChildIndex] == propNode.TreeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.TreeNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(propNode.PropertyDef);

				origParentNode.TreeNode.AddChild(propNode.TreeNode);
				origParentNode.TreeNode.TreeView.SelectItems(new[] { propNode });
			}
			else
				newOptions.CopyTo(propNode.PropertyDef);
			propNode.TreeNode.RefreshUI();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = origParentNode.TreeNode.Children.Remove(propNode.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(propNode.PropertyDef);
				origParentNode.TreeNode.Children.Insert(origParentChildIndex, propNode.TreeNode);
				origParentNode.TreeNode.TreeView.SelectItems(new[] { propNode });
			}
			else
				origOptions.CopyTo(propNode.PropertyDef);
			propNode.TreeNode.RefreshUI();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return propNode; }
		}
	}
}
