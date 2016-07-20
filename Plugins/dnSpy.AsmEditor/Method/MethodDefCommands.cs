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
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Method {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager, DeleteMethodDefCommand.EditMenuCommand removeCmd, DeleteMethodDefCommand.CodeCommand removeCmd2, MethodDefSettingsCommand.EditMenuCommand settingsCmd, MethodDefSettingsCommand.CodeCommand settingsCmd2) {
			wpfCommandManager.AddRemoveCommand(removeCmd);
			wpfCommandManager.AddRemoveCommand(removeCmd2, fileTabManager);
			wpfCommandManager.AddSettingsCommand(fileTabManager, settingsCmd, settingsCmd2);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteMethodDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:DeleteMethodCommand", Icon = "Delete", InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_FILES_ASMED_DELETE, Order = 30)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsVisible(AsmEditorContext context) => DeleteMethodDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteMethodDefCommand.Execute(undoCommandManager, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => DeleteMethodDefCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:DeleteMethodCommand", Icon = "Delete", InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 30)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeView fileTreeView)
				: base(fileTreeView) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsVisible(AsmEditorContext context) => DeleteMethodDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteMethodDefCommand.Execute(undoCommandManager, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => DeleteMethodDefCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:DeleteMethodCommand", Icon = "Delete", InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_CODE_ASMED_DELETE, Order = 30)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeView fileTreeView)
				: base(fileTreeView) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && DeleteMethodDefCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => DeleteMethodDefCommand.Execute(undoCommandManager, context.Nodes);
			public override string GetHeader(CodeContext context) => DeleteMethodDefCommand.GetHeader(context.Nodes);
		}

		static string GetHeader(IFileTreeNodeData[] nodes) {
			if (nodes.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.DeleteX, UIUtilities.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format(dnSpy_AsmEditor_Resources.DeleteMethodsCommand, nodes.Length);
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) => nodes.Length > 0 && nodes.All(n => n is IMethodNode);

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			if (!AskDeleteDef(dnSpy_AsmEditor_Resources.AskDeleteMethod))
				return;

			var methodNodes = nodes.Cast<IMethodNode>().ToArray();
			undoCommandManager.Value.Add(new DeleteMethodDefCommand(methodNodes));
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
						this.PropOrEvent = propOrEvt;
						this.PropEventType = propEventType;
						this.Index = index;
					}
				}

				public ModelInfo(MethodDef method) {
					this.OwnerType = method.DeclaringType;
					this.MethodIndex = this.OwnerType.Methods.IndexOf(method);
					Debug.Assert(this.MethodIndex >= 0);

					this.PropEventInfos = new List<PropEventInfo>();
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

			public void Delete(IMethodNode[] nodes) {
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

			public void Restore(IMethodNode[] nodes) {
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

		DeletableNodes<IMethodNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteMethodDefCommand(IMethodNode[] methodNodes) {
			this.nodes = new DeletableNodes<IMethodNode>(methodNodes);
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
		[ExportMenuItem(Header = "res:CreateMethodCommand", Icon = "NewMethod", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 60)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateMethodDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateMethodDefCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateMethodCommand", Icon = "NewMethod", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 60)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateMethodDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateMethodDefCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateMethodCommand", Icon = "NewMethod", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 60)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsEnabled(CodeContext context) =>
				context.IsDefinition &&
				context.Nodes.Length == 1 &&
				context.Nodes[0] is ITypeNode;

			public override void Execute(CodeContext context) => CreateMethodDefCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) =>
			nodes.Length == 1 &&
			(nodes[0] is ITypeNode || (nodes[0].TreeNode.Parent != null && nodes[0].TreeNode.Parent.Data is ITypeNode));

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

			bool isInstance = !(typeNode.TypeDef.IsAbstract && typeNode.TypeDef.IsSealed);
			var sig = isInstance ? MethodSig.CreateInstance(module.CorLibTypes.Void) : MethodSig.CreateStatic(module.CorLibTypes.Void);
			var options = MethodDefOptions.Create("MyMethod", sig);
			if (typeNode.TypeDef.IsInterface)
				options.Attributes |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;

			var data = new MethodOptionsVM(options, module, appWindow.LanguageManager, typeNode.TypeDef, null);
			var win = new MethodOptionsDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateMethodCommand2;
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateMethodDefCommand(typeNode, data.CreateMethodDefOptions());
			undoCommandManager.Value.Add(cmd);
			appWindow.FileTabManager.FollowReference(cmd.methodNode);
		}

		readonly ITypeNode ownerNode;
		readonly IMethodNode methodNode;

		CreateMethodDefCommand(ITypeNode ownerNode, MethodDefOptions options) {
			this.ownerNode = ownerNode;
			this.methodNode = ownerNode.Create(options.CreateMethodDef(ownerNode.TypeDef.Module));
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
		[ExportMenuItem(Header = "res:EditMethodCommand", Icon = "Settings", InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 30)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => MethodDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => MethodDefSettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditMethodCommand", Icon = "Settings", InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 30)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => MethodDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => MethodDefSettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:EditMethodCommand", Icon = "Settings", InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_CODE_ASMED_SETTINGS, Order = 30)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsEnabled(CodeContext context) => MethodDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => MethodDefSettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) => nodes.Length == 1 && nodes[0] is IMethodNode;

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var methodNode = (IMethodNode)nodes[0];

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new MethodOptionsVM(new MethodDefOptions(methodNode.MethodDef), module, appWindow.LanguageManager, methodNode.MethodDef.DeclaringType, methodNode.MethodDef);
			var win = new MethodOptionsDlg();
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandManager.Value.Add(new MethodDefSettingsCommand(methodNode, data.CreateMethodDefOptions()));
		}

		readonly IMethodNode methodNode;
		readonly MethodDefOptions newOptions;
		readonly MethodDefOptions origOptions;
		readonly IFileTreeNodeData origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;
		readonly Field.MemberRefInfo[] memberRefInfos;

		MethodDefSettingsCommand(IMethodNode methodNode, MethodDefOptions options) {
			this.methodNode = methodNode;
			this.newOptions = options;
			this.origOptions = new MethodDefOptions(methodNode.MethodDef);

			this.origParentNode = (IFileTreeNodeData)methodNode.TreeNode.Parent.Data;
			this.origParentChildIndex = this.origParentNode.TreeNode.Children.IndexOf(methodNode.TreeNode);
			Debug.Assert(this.origParentChildIndex >= 0);
			if (this.origParentChildIndex < 0)
				throw new InvalidOperationException();

			this.nameChanged = origOptions.Name != newOptions.Name;
			if (this.nameChanged)
				this.memberRefInfos = RefFinder.FindMemberRefsToThisModule(methodNode.GetModule()).Where(a => RefFinder.MethodEqualityComparerInstance.Equals(a, methodNode.MethodDef)).Select(a => new Field.MemberRefInfo(a)).ToArray();
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
