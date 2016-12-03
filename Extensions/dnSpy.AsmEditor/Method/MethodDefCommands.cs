/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Method {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, IDocumentTabService documentTabService, DeleteMethodDefCommand.EditMenuCommand removeCmd, DeleteMethodDefCommand.CodeCommand removeCmd2, MethodDefSettingsCommand.EditMenuCommand settingsCmd, MethodDefSettingsCommand.CodeCommand settingsCmd2) {
			wpfCommandService.AddRemoveCommand(removeCmd);
			wpfCommandService.AddRemoveCommand(removeCmd2, documentTabService);
			wpfCommandService.AddSettingsCommand(documentTabService, settingsCmd, settingsCmd2);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteMethodDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:DeleteMethodCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_DELETE, Order = 30)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService) {
				this.undoCommandService = undoCommandService;
			}

			public override bool IsVisible(AsmEditorContext context) => DeleteMethodDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteMethodDefCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => DeleteMethodDefCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:DeleteMethodCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 30)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) {
				this.undoCommandService = undoCommandService;
			}

			public override bool IsVisible(AsmEditorContext context) => DeleteMethodDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteMethodDefCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => DeleteMethodDefCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:DeleteMethodCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_DELETE, Order = 30)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) {
				this.undoCommandService = undoCommandService;
			}

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && DeleteMethodDefCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => DeleteMethodDefCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(CodeContext context) => DeleteMethodDefCommand.GetHeader(context.Nodes);
		}

		static string GetHeader(DocumentTreeNodeData[] nodes) {
			if (nodes.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.DeleteX, UIUtilities.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format(dnSpy_AsmEditor_Resources.DeleteMethodsCommand, nodes.Length);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length > 0 && nodes.All(n => n is MethodNode);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			if (!AskDeleteDef(dnSpy_AsmEditor_Resources.AskDeleteMethod))
				return;

			var methodNodes = nodes.Cast<MethodNode>().ToArray();
			undoCommandService.Value.Add(new DeleteMethodDefCommand(methodNodes));
		}

		internal static bool AskDeleteDef(string msg) {
			var res = MsgBox.Instance.ShowIgnorableMessage(new Guid("DA7D935C-F5ED-44A4-BFA8-CC794AD0F105"), msg, MsgBoxButton.Yes | MsgBoxButton.No);
			return res == null || res == MsgBoxButton.Yes;
		}

		struct DeleteModelNodes {
			ModelInfo[] infos;

			struct ModelInfo {
				public readonly TypeDef OwnerType;
				public readonly int MethodIndex;
				public readonly List<PropEventInfo> PropEventInfos;

				public enum PropEventType {
					PropertyGetter,
					PropertySetter,
					PropertyOther,
					EventAdd,
					EventInvoke,
					EventRemove,
					EventOther,
				}

				public struct PropEventInfo {
					public readonly ICodedToken PropOrEvent;
					public readonly PropEventType PropEventType;
					public readonly int Index;

					public PropEventInfo(ICodedToken propOrEvt, PropEventType propEventType, int index) {
						PropOrEvent = propOrEvt;
						PropEventType = propEventType;
						Index = index;
					}
				}

				public ModelInfo(MethodDef method) {
					OwnerType = method.DeclaringType;
					MethodIndex = OwnerType.Methods.IndexOf(method);
					Debug.Assert(MethodIndex >= 0);

					PropEventInfos = new List<PropEventInfo>();
				}

				public void AddMethods(ICodedToken propOrEvent, PropEventType propEvtType, IList<MethodDef> propEvtMethods, MethodDef method) {
					while (true) {
						int index = propEvtMethods.IndexOf(method);
						if (index < 0)
							break;
						propEvtMethods.RemoveAt(index);
						PropEventInfos.Add(new PropEventInfo(propOrEvent, propEvtType, index));
					}
				}
			}

			public void Delete(MethodNode[] nodes) {
				Debug.Assert(infos == null);
				if (infos != null)
					throw new InvalidOperationException();

				infos = new ModelInfo[nodes.Length];

				for (int i = 0; i < infos.Length; i++) {
					var node = nodes[i];

					var info = new ModelInfo(node.MethodDef);
					infos[i] = info;

					foreach (var prop in info.OwnerType.Properties) {
						info.AddMethods(prop, ModelInfo.PropEventType.PropertyGetter, prop.GetMethods, node.MethodDef);
						info.AddMethods(prop, ModelInfo.PropEventType.PropertySetter, prop.SetMethods, node.MethodDef);
						info.AddMethods(prop, ModelInfo.PropEventType.PropertyOther, prop.OtherMethods, node.MethodDef);
					}

					foreach (var evt in info.OwnerType.Events) {
						if (evt.AddMethod == node.MethodDef) {
							evt.AddMethod = null;
							info.PropEventInfos.Add(new ModelInfo.PropEventInfo(evt, ModelInfo.PropEventType.EventAdd, -1));
						}
						if (evt.InvokeMethod == node.MethodDef) {
							evt.InvokeMethod = null;
							info.PropEventInfos.Add(new ModelInfo.PropEventInfo(evt, ModelInfo.PropEventType.EventInvoke, -1));
						}
						if (evt.RemoveMethod == node.MethodDef) {
							evt.RemoveMethod = null;
							info.PropEventInfos.Add(new ModelInfo.PropEventInfo(evt, ModelInfo.PropEventType.EventRemove, -1));
						}
						info.AddMethods(evt, ModelInfo.PropEventType.EventOther, evt.OtherMethods, node.MethodDef);
					}

					info.OwnerType.Methods.RemoveAt(info.MethodIndex);
				}
			}

			public void Restore(MethodNode[] nodes) {
				Debug.Assert(infos != null);
				if (infos == null)
					throw new InvalidOperationException();
				Debug.Assert(infos.Length == nodes.Length);
				if (infos.Length != nodes.Length)
					throw new InvalidOperationException();

				for (int i = infos.Length - 1; i >= 0; i--) {
					var node = nodes[i];
					var info = infos[i];

					info.OwnerType.Methods.Insert(info.MethodIndex, node.MethodDef);

					for (int j = info.PropEventInfos.Count - 1; j >= 0; j--) {
						var pinfo = info.PropEventInfos[i];
						EventDef evt;
						switch (pinfo.PropEventType) {
						case ModelInfo.PropEventType.PropertyGetter:
							((PropertyDef)pinfo.PropOrEvent).GetMethods.Insert(pinfo.Index, node.MethodDef);
							break;

						case ModelInfo.PropEventType.PropertySetter:
							((PropertyDef)pinfo.PropOrEvent).SetMethods.Insert(pinfo.Index, node.MethodDef);
							break;

						case ModelInfo.PropEventType.PropertyOther:
							((PropertyDef)pinfo.PropOrEvent).OtherMethods.Insert(pinfo.Index, node.MethodDef);
							break;

						case ModelInfo.PropEventType.EventAdd:
							evt = (EventDef)pinfo.PropOrEvent;
							Debug.Assert(evt.AddMethod == null);
							if (evt.AddMethod != null)
								throw new InvalidOperationException();
							evt.AddMethod = node.MethodDef;
							break;

						case ModelInfo.PropEventType.EventInvoke:
							evt = (EventDef)pinfo.PropOrEvent;
							Debug.Assert(evt.InvokeMethod == null);
							if (evt.InvokeMethod != null)
								throw new InvalidOperationException();
							evt.InvokeMethod = node.MethodDef;
							break;

						case ModelInfo.PropEventType.EventRemove:
							evt = (EventDef)pinfo.PropOrEvent;
							Debug.Assert(evt.RemoveMethod == null);
							if (evt.RemoveMethod != null)
								throw new InvalidOperationException();
							evt.RemoveMethod = node.MethodDef;
							break;

						case ModelInfo.PropEventType.EventOther:
							((EventDef)pinfo.PropOrEvent).OtherMethods.Insert(pinfo.Index, node.MethodDef);
							break;

						default:
							throw new InvalidOperationException();
						}
					}
				}

				infos = null;
			}
		}

		DeletableNodes<MethodNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteMethodDefCommand(MethodNode[] methodNodes) {
			nodes = new DeletableNodes<MethodNode>(methodNodes);
		}

		public string Description => dnSpy_AsmEditor_Resources.DeleteMethodCommand;

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
	sealed class CreateMethodDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:CreateMethodCommand", Icon = DsImagesAttribute.NewMethod, Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 60)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateMethodDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateMethodDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateMethodCommand", Icon = DsImagesAttribute.NewMethod, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 60)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateMethodDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateMethodDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateMethodCommand", Icon = DsImagesAttribute.NewMethod, Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 60)]
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

			public override void Execute(CodeContext context) => CreateMethodDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 &&
			(nodes[0] is TypeNode || (nodes[0].TreeNode.Parent != null && nodes[0].TreeNode.Parent.Data is TypeNode));

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var ownerNode = nodes[0];
			if (!(ownerNode is TypeNode))
				ownerNode = (DocumentTreeNodeData)ownerNode.TreeNode.Parent.Data;
			var typeNode = ownerNode as TypeNode;
			Debug.Assert(typeNode != null);
			if (typeNode == null)
				throw new InvalidOperationException();

			var module = typeNode.GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			bool isInstance = !(typeNode.TypeDef.IsAbstract && typeNode.TypeDef.IsSealed);
			var sig = isInstance ? MethodSig.CreateInstance(module.CorLibTypes.Void) : MethodSig.CreateStatic(module.CorLibTypes.Void);
			var options = MethodDefOptions.Create("MyMethod", sig);
			if (typeNode.TypeDef.IsInterface)
				options.Attributes |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;

			var data = new MethodOptionsVM(options, module, appService.DecompilerService, typeNode.TypeDef, null);
			var win = new MethodOptionsDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateMethodCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateMethodDefCommand(typeNode, data.CreateMethodDefOptions());
			undoCommandService.Value.Add(cmd);
			appService.DocumentTabService.FollowReference(cmd.methodNode);
		}

		readonly TypeNode ownerNode;
		readonly MethodNode methodNode;

		CreateMethodDefCommand(TypeNode ownerNode, MethodDefOptions options) {
			this.ownerNode = ownerNode;
			methodNode = ownerNode.Create(options.CreateMethodDef(ownerNode.TypeDef.Module));
		}

		public string Description => dnSpy_AsmEditor_Resources.CreateMethodCommand2;

		public void Execute() {
			ownerNode.TreeNode.EnsureChildrenLoaded();
			ownerNode.TypeDef.Methods.Add(methodNode.MethodDef);
			ownerNode.TreeNode.AddChild(methodNode.TreeNode);
		}

		public void Undo() {
			bool b = ownerNode.TreeNode.Children.Remove(methodNode.TreeNode) &&
					ownerNode.TypeDef.Methods.Remove(methodNode.MethodDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return ownerNode; }
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class MethodDefSettingsCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:EditMethodCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 30)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => MethodDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => MethodDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditMethodCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 30)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => MethodDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => MethodDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:EditMethodCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SETTINGS, Order = 30)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) => MethodDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => MethodDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length == 1 && nodes[0] is MethodNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var methodNode = (MethodNode)nodes[0];

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new MethodOptionsVM(new MethodDefOptions(methodNode.MethodDef), module, appService.DecompilerService, methodNode.MethodDef.DeclaringType, methodNode.MethodDef);
			var win = new MethodOptionsDlg();
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandService.Value.Add(new MethodDefSettingsCommand(methodNode, data.CreateMethodDefOptions()));
		}

		readonly MethodNode methodNode;
		readonly MethodDefOptions newOptions;
		readonly MethodDefOptions origOptions;
		readonly DocumentTreeNodeData origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;
		readonly Field.MemberRefInfo[] memberRefInfos;

		MethodDefSettingsCommand(MethodNode methodNode, MethodDefOptions options) {
			this.methodNode = methodNode;
			newOptions = options;
			origOptions = new MethodDefOptions(methodNode.MethodDef);

			origParentNode = (DocumentTreeNodeData)methodNode.TreeNode.Parent.Data;
			origParentChildIndex = origParentNode.TreeNode.Children.IndexOf(methodNode.TreeNode);
			Debug.Assert(origParentChildIndex >= 0);
			if (origParentChildIndex < 0)
				throw new InvalidOperationException();

			nameChanged = origOptions.Name != newOptions.Name;
			if (nameChanged)
				memberRefInfos = RefFinder.FindMemberRefsToThisModule(methodNode.GetModule()).Where(a => RefFinder.MethodEqualityComparerInstance.Equals(a, methodNode.MethodDef)).Select(a => new Field.MemberRefInfo(a)).ToArray();
		}

		public string Description => dnSpy_AsmEditor_Resources.EditMethodCommand2;

		public void Execute() {
			if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.TreeNode.Children.Count && origParentNode.TreeNode.Children[origParentChildIndex] == methodNode.TreeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.TreeNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(methodNode.MethodDef);

				origParentNode.TreeNode.AddChild(methodNode.TreeNode);
			}
			else
				newOptions.CopyTo(methodNode.MethodDef);
			if (memberRefInfos != null) {
				foreach (var info in memberRefInfos)
					info.MemberRef.Name = methodNode.MethodDef.Name;
			}
			methodNode.TreeNode.RefreshUI();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = origParentNode.TreeNode.Children.Remove(methodNode.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(methodNode.MethodDef);
				origParentNode.TreeNode.Children.Insert(origParentChildIndex, methodNode.TreeNode);
			}
			else
				origOptions.CopyTo(methodNode.MethodDef);
			if (memberRefInfos != null) {
				foreach (var info in memberRefInfos)
					info.MemberRef.Name = info.OrigName;
			}
			methodNode.TreeNode.RefreshUI();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return methodNode; }
		}
	}
}
