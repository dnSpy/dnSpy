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

namespace dnSpy.AsmEditor.Method {
	[Export(typeof(IPlugin))]
	sealed class AssemblyPlugin : IPlugin {
		void IPlugin.EarlyInit() {
		}

		public void OnLoaded() {
			MainWindow.Instance.TreeView.AddCommandBinding(ApplicationCommands.Delete, new TreeViewCommandProxy(new DeleteMethodDefCommand.TheEditCommand()));
			MainWindow.Instance.CodeBindings.Add(EditingCommands.Delete, new TextEditorCommandProxy(new DeleteMethodDefCommand.TheTextEditorCommand()), ModifierKeys.None, Key.Delete);
			Utils.InstallSettingsCommand(new MethodDefSettingsCommand.TheEditCommand(), new MethodDefSettingsCommand.TheTextEditorCommand());
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteMethodDefCommand : IUndoCommand {
		const string CMD_NAME = "Delete Method";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Delete",
								InputGestureText = "Del",
								Category = "AsmEd",
								Order = 330)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							Menu = "_Edit",
							MenuIcon = "Delete",
							MenuInputGestureText = "Del",
							MenuCategory = "AsmEd",
							MenuOrder = 2130)]
		internal sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return DeleteMethodDefCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				DeleteMethodDefCommand.Execute(nodes);
			}

			protected override void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
				DeleteMethodDefCommand.Initialize(nodes, menuItem);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Delete",
								InputGestureText = "Del",
								Category = "AsmEd",
								Order = 330)]
		internal sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					DeleteMethodDefCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				DeleteMethodDefCommand.Execute(ctx.Nodes);
			}

			protected override void Initialize(Context ctx, MenuItem menuItem) {
				DeleteMethodDefCommand.Initialize(ctx.Nodes, menuItem);
			}
		}

		static void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
			if (nodes.Length == 1)
				menuItem.Header = string.Format("Delete {0}", UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
			else
				menuItem.Header = string.Format("Delete {0} methods", nodes.Length);
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is MethodTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			if (!AskDeleteDef("method"))
				return;

			var methodNodes = nodes.Select(a => (MethodTreeNode)a).ToArray();
			UndoCommandManager.Instance.Add(new DeleteMethodDefCommand(methodNodes));
		}

		internal static bool AskDeleteDef(string defName) {
			var msg = string.Format("There could be code in some assembly that references this {0}. Are you sure you want to delete the {0}?", defName);
			var res = MainWindow.Instance.ShowIgnorableMessageBox("delete def", msg, System.Windows.MessageBoxButton.YesNo);
			return res == null || res == MsgBoxButton.OK;
		}

		public struct DeleteModelNodes {
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

			public void Delete(MethodTreeNode[] nodes) {
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

			public void Restore(MethodTreeNode[] nodes) {
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

		DeletableNodes<MethodTreeNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteMethodDefCommand(MethodTreeNode[] methodNodes) {
			this.nodes = new DeletableNodes<MethodTreeNode>(methodNodes);
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
	sealed class CreateMethodDefCommand : IUndoCommand {
		const string CMD_NAME = "Create Method";
		[ExportContextMenuEntry(Header = CMD_NAME + "...",
								Icon = "NewMethod",
								Category = "AsmEd",
								Order = 560)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "...",
							Menu = "_Edit",
							MenuIcon = "NewMethod",
							MenuCategory = "AsmEd",
							MenuOrder = 2360)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateMethodDefCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateMethodDefCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "...",
								Icon = "NewMethod",
								Category = "AsmEd",
								Order = 560)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					ctx.Nodes.Length == 1 &&
					ctx.Nodes[0] is TypeTreeNode;
			}

			protected override void Execute(Context ctx) {
				CreateMethodDefCommand.Execute(ctx.Nodes);
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

			bool isInstance = !(typeNode.TypeDef.IsAbstract && typeNode.TypeDef.IsSealed);
			var sig = isInstance ? MethodSig.CreateInstance(module.CorLibTypes.Void) : MethodSig.CreateStatic(module.CorLibTypes.Void);
			var options = MethodDefOptions.Create("MyMethod", sig);
			if (typeNode.TypeDef.IsInterface)
				options.Attributes |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;

			var data = new MethodOptionsVM(options, module, MainWindow.Instance.CurrentLanguage, typeNode.TypeDef, null);
			var win = new MethodOptionsDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateMethodDefCommand(typeNode, data.CreateMethodDefOptions());
			UndoCommandManager.Instance.Add(cmd);
			MainWindow.Instance.JumpToReference(cmd.methodNode);
		}

		readonly TypeTreeNode ownerNode;
		readonly MethodTreeNode methodNode;

		CreateMethodDefCommand(TypeTreeNode ownerNode, MethodDefOptions options) {
			this.ownerNode = ownerNode;
			this.methodNode = new MethodTreeNode(options.CreateMethodDef(ownerNode.TypeDef.Module));
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			ownerNode.EnsureChildrenFiltered();
			ownerNode.TypeDef.Methods.Add(methodNode.MethodDef);
			ownerNode.AddToChildren(methodNode);
		}

		public void Undo() {
			bool b = ownerNode.Children.Remove(methodNode) &&
					ownerNode.TypeDef.Methods.Remove(methodNode.MethodDef);
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
	sealed class MethodDefSettingsCommand : IUndoCommand {
		const string CMD_NAME = "Edit Method";
		[ExportContextMenuEntry(Header = CMD_NAME + "...",
								Icon = "Settings",
								InputGestureText = "Alt+Enter",
								Category = "AsmEd",
								Order = 630)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "...",
							Menu = "_Edit",
							MenuIcon = "Settings",
							MenuInputGestureText = "Alt+Enter",
							MenuCategory = "AsmEd",
							MenuOrder = 2430)]
		internal sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return MethodDefSettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				MethodDefSettingsCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "...",
								Icon = "Settings",
								InputGestureText = "Alt+Enter",
								Category = "AsmEd",
								Order = 630)]
		internal sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return MethodDefSettingsCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				MethodDefSettingsCommand.Execute(ctx.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is MethodTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var methodNode = (MethodTreeNode)nodes[0];

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new MethodOptionsVM(new MethodDefOptions(methodNode.MethodDef), module, MainWindow.Instance.CurrentLanguage, methodNode.MethodDef.DeclaringType, methodNode.MethodDef);
			var win = new MethodOptionsDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new MethodDefSettingsCommand(methodNode, data.CreateMethodDefOptions()));
		}

		readonly MethodTreeNode methodNode;
		readonly MethodDefOptions newOptions;
		readonly MethodDefOptions origOptions;
		readonly ILSpyTreeNode origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;
		readonly Field.MemberRefInfo[] memberRefInfos;

		MethodDefSettingsCommand(MethodTreeNode methodNode, MethodDefOptions options) {
			this.methodNode = methodNode;
			this.newOptions = options;
			this.origOptions = new MethodDefOptions(methodNode.MethodDef);

			this.origParentNode = (ILSpyTreeNode)methodNode.Parent;
			this.origParentChildIndex = this.origParentNode.Children.IndexOf(methodNode);
			Debug.Assert(this.origParentChildIndex >= 0);
			if (this.origParentChildIndex < 0)
				throw new InvalidOperationException();

			this.nameChanged = origOptions.Name != newOptions.Name;
			if (this.nameChanged)
				this.memberRefInfos = RefFinder.FindMemberRefsToThisModule(ILSpyTreeNode.GetModule(methodNode)).Where(a => RefFinder.MethodEqualityComparerInstance.Equals(a, methodNode.MethodDef)).Select(a => new Field.MemberRefInfo(a)).ToArray();
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.Children.Count && origParentNode.Children[origParentChildIndex] == methodNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(methodNode.MethodDef);

				origParentNode.AddToChildren(methodNode);
			}
			else
				newOptions.CopyTo(methodNode.MethodDef);
			if (memberRefInfos != null) {
				foreach (var info in memberRefInfos)
					info.MemberRef.Name = methodNode.MethodDef.Name;
			}
			methodNode.RaiseUIPropsChanged();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = origParentNode.Children.Remove(methodNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(methodNode.MethodDef);
				origParentNode.Children.Insert(origParentChildIndex, methodNode);
			}
			else
				origOptions.CopyTo(methodNode.MethodDef);
			if (memberRefInfos != null) {
				foreach (var info in memberRefInfos)
					info.MemberRef.Name = info.OrigName;
			}
			methodNode.RaiseUIPropsChanged();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return methodNode; }
		}

		public void Dispose() {
		}
	}
}
