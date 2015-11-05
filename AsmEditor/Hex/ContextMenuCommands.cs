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
using System.IO;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Menus;
using dnSpy.Decompiler;
using dnSpy.HexEditor;
using dnSpy.Shared.UI.Menus;
using dnSpy.Tabs;
using dnSpy.TreeNodes;
using dnSpy.TreeNodes.Hex;
using ICSharpCode.AvalonEdit;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IPlugin))]
	sealed class HexContextMenuPlugin : IPlugin {
		void IPlugin.EarlyInit() {
		}

		public void OnLoaded() {
			OpenHexEditorCommand.OnLoaded();
			GoToMDTableRowHexEditorCommand.OnLoaded();
			GoToMDTableRowUIHexEditorCommand.OnLoaded();
		}
	}

	sealed class HexContext {
		public SharpTreeNode[] Nodes;
		public bool IsLocalTarget;
		public object Reference;
		public int? Line;
		public int? Column;
		public GuidObject CreatorObject;

		public HexContext() {
		}

		public HexContext(GuidObject creatorObject, SharpTreeNode[] nodes) {
			this.Nodes = nodes;
			this.CreatorObject = creatorObject;
		}

		public HexContext(DecompilerTextView textView, int? line, int? col, object @ref, bool isLocalTarget) {
			this.Reference = @ref;
			this.IsLocalTarget = isLocalTarget;
			this.Line = line;
			this.Column = col;
			this.CreatorObject = new GuidObject(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID, textView);
		}
	}

	abstract class HexTextEditorCommand : MenuItemBase<HexContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override HexContext CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID)) {
				var refSeg = context.FindByType<CodeReferenceSegment>();
				bool isLocalTarget = false;
				object @ref = null;
				if (@ref != null) {
					@ref = refSeg.Reference;
					isLocalTarget = refSeg.IsLocalTarget;
				}
				var pos = context.FindByType<TextViewPosition?>();
				return new HexContext(context.CreatorObject.Object as DecompilerTextView, pos == null ? (int?)null : pos.Value.Line, pos == null ? (int?)null : pos.Value.Column, @ref, isLocalTarget);
			}

			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID)) {
				var nodes = context.FindByType<SharpTreeNode[]>();
				if (nodes == null)
					return null;
				return new HexContext(context.CreatorObject, nodes);
			}

			return null;
		}

		public override bool IsEnabled(HexContext context) {
			return true;
		}
	}

	abstract class HexMenuCommand : MenuItemBase<HexContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override HexContext CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.APP_MENU_EDIT_GUID))
				return null;
			return CreateContext();
		}

		internal static HexContext CreateContext() {
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView != null && textView.IsKeyboardFocusWithin)
				return CreateContext(textView);

			if (MainWindow.Instance.TreeView.IsKeyboardFocusWithin)
				return CreateContext(MainWindow.Instance.TreeView);

			if (MainWindow.Instance.TreeView.SelectedItems.Count != 0) {
				bool teFocus = textView != null && textView.TextEditor.TextArea.IsFocused;
				if (teFocus)
					return CreateContext(textView);
				if (UIUtils.HasSelectedChildrenFocus(MainWindow.Instance.TreeView))
					return CreateContext(MainWindow.Instance.TreeView);
			}

			return new HexContext();
		}

		static HexContext CreateContext(DecompilerTextView textView) {
			var position = textView.TextEditor.TextArea.Caret.Position;
			var @ref = textView.GetReferenceSegmentAt(position);
			if (@ref == null)
				return new HexContext();
			var pos = textView.TextEditor.TextArea.Caret.Position;
			return new HexContext(textView, pos.Line, pos.Column, @ref.Reference, @ref.IsLocalTarget);
		}

		static HexContext CreateContext(SharpTreeView treeView) {
			return new HexContext(new GuidObject(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID, treeView), treeView.GetTopLevelSelection().ToArray());
		}

		public override bool IsEnabled(HexContext context) {
			return true;
		}
	}

	static class OpenHexEditorCommand {
		internal static void OnLoaded() {
			MainWindow.Instance.CodeBindings.Add(new RoutedCommand("OpenHexEditor", typeof(OpenHexEditorCommand)),
				(s, e) => ExecuteCommand(),
				(s, e) => e.CanExecute = CanExecuteCommand(),
				ModifierKeys.Control, Key.X);
		}

		[ExportMenuItem(Header = "Open He_x Editor", Icon = "Binary", InputGestureText = "Ctrl+X", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 0)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}

			public override string GetHeader(HexContext context) {
				return GetHeaderInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Open He_x Editor", Icon = "Binary", InputGestureText = "Ctrl+X", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 0)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}

			public override string GetHeader(HexContext context) {
				return GetHeaderInternal(context);
			}
		}

		static string GetHeaderInternal(HexContext context) {
			return MainWindow.Instance.GetHexTabState(GetAssemblyTreeNode(context)) == null ? "Open Hex Editor" : "Show Hex Editor";
		}

		static void ExecuteCommand() {
			var context = HexMenuCommand.CreateContext();
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				ShowAddressReferenceInHexEditorCommand.ExecuteInternal(context);
			else if (ShowILRangeInHexEditorCommand.IsVisibleInternal(context))
				ShowILRangeInHexEditorCommand.ExecuteInternal(context);
			else if (ShowHexNodeInHexEditorCommand.IsVisibleInternal(context))
				ShowHexNodeInHexEditorCommand.ExecuteInternal(context);
			else if (IsVisibleInternal(context))
				ExecuteInternal(context);
		}

		static bool CanExecuteCommand() {
			var context = HexMenuCommand.CreateContext();
			return ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context) ||
				ShowILRangeInHexEditorCommand.IsVisibleInternal(context) ||
				ShowHexNodeInHexEditorCommand.IsVisibleInternal(context) ||
				IsVisibleInternal(context);
		}

		internal static void ExecuteInternal(HexContext context) {
			var node = GetNode(context);
			if (node != null)
				MainWindow.Instance.OpenOrShowHexBox(node.DnSpyFile.Filename);
		}

		static bool IsVisibleInternal(HexContext context) {
			var node = GetNode(context);
			return node != null && !string.IsNullOrEmpty(node.DnSpyFile.Filename);
		}

		static AssemblyTreeNode GetAssemblyTreeNode(HexContext context) {
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (ShowILRangeInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (ShowHexNodeInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID))
				return GetActiveAssemblyTreeNode();
			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID)) {
				return context.Nodes != null &&
					context.Nodes.Length == 1 ?
					context.Nodes[0] as AssemblyTreeNode : null;
			}
			return null;
		}

		static AssemblyTreeNode GetActiveAssemblyTreeNode() {
			var tabState = MainWindow.Instance.GetActiveDecompileTabState();
			if (tabState == null || tabState.DecompiledNodes.Length == 0)
				return null;
			return ILSpyTreeNode.GetNode<AssemblyTreeNode>(tabState.DecompiledNodes[0]);
		}

		static AssemblyTreeNode GetNode(HexContext context) {
			return GetAssemblyTreeNode(context);
		}
	}

	static class ShowAddressReferenceInHexEditorCommand {
		[ExportMenuItem(Header = "Show in He_x Editor", Icon = "Binary", InputGestureText = "Ctrl+X", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 10)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Show in He_x Editor", Icon = "Binary", InputGestureText = "Ctrl+X", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 10)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		internal static void ExecuteInternal(HexContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		internal static bool IsVisibleInternal(HexContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(HexContext context) {
			if (context.Reference == null)
				return null;

			var addr = context.Reference as AddressReference;
			if (addr != null && File.Exists(addr.Filename))
				return addr;

			var rsrc = context.Reference as IResourceNode;
			if (rsrc != null && rsrc.FileOffset != 0) {
				var name = GetFilename((ILSpyTreeNode)rsrc);
				if (!string.IsNullOrEmpty(name))
					return new AddressReference(name, false, rsrc.FileOffset, rsrc.Length);
			}

			return null;
		}

		internal static string GetFilename(ILSpyTreeNode node) {
			var asmNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(node);
			if (asmNode == null)
				return null;
			var mod = asmNode.DnSpyFile.ModuleDef;
			if (mod != null && File.Exists(mod.Location))
				return mod.Location;
			var peImage = asmNode.DnSpyFile.PEImage;
			if (peImage != null && File.Exists(peImage.FileName))
				return peImage.FileName;
			return null;
		}
	}

	static class ShowILRangeInHexEditorCommand {
		[ExportMenuItem(Header = "Show Instructions in He_x Editor", Icon = "Binary", InputGestureText = "Ctrl+X", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 20)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Show Instructions in He_x Editor", Icon = "Binary", InputGestureText = "Ctrl+X", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 20)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		internal static void ExecuteInternal(HexContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		internal static bool IsVisibleInternal(HexContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(HexContext context) {
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (TVShowMethodInstructionsInHexEditorCommand.IsVisibleInternal(context))
				return null;

			var mappings = GetMappings(context);
			if (mappings == null || mappings.Count == 0)
				return null;

			var method = mappings[0].MemberMapping.MethodDef;
			var mod = mappings[0].MemberMapping.MethodDef.Module as ModuleDefMD;
			if (mod == null || string.IsNullOrEmpty(mod.Location))
				return null;

			ulong addr = (ulong)method.RVA;
			ulong len;
			if (MethodAnnotations.Instance.IsBodyModified(method))
				len = 0;
			else if (mappings.Count == 1) {
				addr += (ulong)method.Body.HeaderSize + mappings[0].ILInstructionOffset.From;
				len = mappings[0].ILInstructionOffset.To - mappings[0].ILInstructionOffset.From;
			}
			else {
				addr += (ulong)method.Body.HeaderSize + mappings[0].ILInstructionOffset.From;
				len = 0;
			}

			return new AddressReference(mod.Location, true, addr, len);
		}

		static IList<SourceCodeMapping> GetMappings(HexContext context) {
			if (context.Line == null || context.Column == null)
				return null;
			return MethodBody.EditILInstructionsCommand.GetMappings(context.CreatorObject.Object as DecompilerTextView, context.Line.Value, context.Column.Value);
		}
	}

	static class ShowHexNodeInHexEditorCommand {
		[ExportMenuItem(Header = "Show in He_x Editor", Icon = "Binary", InputGestureText = "Ctrl+X", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 30)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Show in He_x Editor", Icon = "Binary", InputGestureText = "Ctrl+X", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 30)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		internal static void ExecuteInternal(HexContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		internal static bool IsVisibleInternal(HexContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(HexContext context) {
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (ShowILRangeInHexEditorCommand.IsVisibleInternal(context))
				return null;

			if (context.Nodes == null || context.Nodes.Length != 1)
				return null;
			var hexNode = context.Nodes[0] as HexTreeNode;
			if (hexNode == null)
				return null;

			var name = ShowAddressReferenceInHexEditorCommand.GetFilename(hexNode);
			if (string.IsNullOrEmpty(name))
				return null;

			return new AddressReference(name, false, hexNode.StartOffset, hexNode.StartOffset == 0 && hexNode.EndOffset == ulong.MaxValue ? ulong.MaxValue : hexNode.EndOffset - hexNode.StartOffset + 1);
		}
	}

	static class ShowStorageStreamDataInHexEditorCommand {
		[ExportMenuItem(Header = "Show Data in He_x Editor", Icon = "Binary", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 40)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Show Data in He_x Editor", Icon = "Binary", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 40)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		internal static void ExecuteInternal(HexContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		internal static bool IsVisibleInternal(HexContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(HexContext context) {
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (ShowILRangeInHexEditorCommand.IsVisibleInternal(context))
				return null;

			if (context.Nodes == null || context.Nodes.Length != 1)
				return null;
			if (!(context.Nodes[0] is HexTreeNode))
				return null;

			var mod = ILSpyTreeNode.GetModule(context.Nodes[0]) as ModuleDefMD;
			if (mod == null)
				return null;
			var pe = mod.MetaData.PEImage;

			var sectNode = context.Nodes[0] as ImageSectionHeaderTreeNode;
			if (sectNode != null) {
				if (sectNode.SectionNumber >= pe.ImageSectionHeaders.Count)
					return null;
				var sect = pe.ImageSectionHeaders[sectNode.SectionNumber];
				return new AddressReference(mod.Location, false, sect.PointerToRawData, sect.SizeOfRawData);
			}

			var stgNode = context.Nodes[0] as StorageStreamTreeNode;
			if (stgNode != null) {
				if (stgNode.StreamNumber >= mod.MetaData.MetaDataHeader.StreamHeaders.Count)
					return null;
				var sh = mod.MetaData.MetaDataHeader.StreamHeaders[stgNode.StreamNumber];

				return new AddressReference(mod.Location, false, (ulong)mod.MetaData.MetaDataHeader.StartOffset + sh.Offset, sh.StreamSize);
			}

			return null;
		}
	}

	static class TVShowMethodInstructionsInHexEditorCommand {
		[ExportMenuItem(Header = "Show Instructions in He_x Editor", Icon = "Binary", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 50)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Show Instructions in He_x Editor", Icon = "Binary", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 50)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		static void ExecuteInternal(HexContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		internal static bool IsVisibleInternal(HexContext context) {
			return GetAddressReference(context) != null;
		}

		internal static IMemberDef GetMemberDef(HexContext context) {
			IMemberDef def = null;
			if (context.Nodes != null && context.Nodes.Length == 1 && context.Nodes[0] is IMemberTreeNode)
				def = MainWindow.ResolveReference(((IMemberTreeNode)context.Nodes[0]).Member);
			else {
				// Only allow declarations of the defs, i.e., right-clicking a method call with a method
				// def as reference should return null, not the method def.
				if (context.Reference != null && context.IsLocalTarget && context.Reference is IMemberRef) {
					// Don't resolve it. It's confusing if we show the method body of a called method
					// instead of the current method.
					def = context.Reference as IMemberDef;
				}
			}
			var mod = def == null ? null : def.Module;
			return mod is ModuleDefMD ? def : null;
		}

		static AddressReference GetAddressReference(HexContext context) {
			var md = GetMemberDef(context) as MethodDef;
			if (md == null)
				return null;
			var body = md.Body;
			if (body == null)
				return null;

			var mod = md.Module;
			bool modified = MethodAnnotations.Instance.IsBodyModified(md);
			return new AddressReference(mod == null ? null : mod.Location, true, (ulong)md.RVA + body.HeaderSize, modified ? 0 : (ulong)body.GetCodeSize());
		}
	}

	static class TVShowMethodHeaderInHexEditorCommand {
		[ExportMenuItem(Header = "Show Method Body in Hex Editor", Icon = "Binary", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 60)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Show Method Body in Hex Editor", Icon = "Binary", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 60)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		static void ExecuteInternal(HexContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		static bool IsVisibleInternal(HexContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(HexContext context) {
			var info = TVChangeBodyHexEditorCommand.GetMethodLengthAndOffset(context);
			if (info != null)
				return new AddressReference(info.Value.Filename, false, info.Value.Offset, info.Value.Size);

			return null;
		}
	}

	static class TVShowFieldInitialValueInHexEditorCommand {
		[ExportMenuItem(Header = "Show Initial Value in Hex Editor", Icon = "Binary", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 70)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Show Initial Value in Hex Editor", Icon = "Binary", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 70)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		static void ExecuteInternal(HexContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		static bool IsVisibleInternal(HexContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(HexContext context) {
			var fd = TVShowMethodInstructionsInHexEditorCommand.GetMemberDef(context) as FieldDef;
			if (fd == null || fd.RVA == 0)
				return null;
			var iv = fd.InitialValue;
			if (iv == null)
				return null;

			var mod = fd.Module;
			return new AddressReference(mod == null ? null : mod.Location, true, (ulong)fd.RVA, (ulong)iv.Length);
		}
	}

	static class TVShowResourceInHexEditorCommand {
		[ExportMenuItem(Header = "Show in Hex Editor", Icon = "Binary", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 80)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Show in Hex Editor", Icon = "Binary", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 80)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		static void ExecuteInternal(HexContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		static bool IsVisibleInternal(HexContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(HexContext context) {
			if (context.Nodes == null || context.Nodes.Length != 1)
				return null;

			var rsrc = context.Nodes[0] as IResourceNode;
			if (rsrc != null && rsrc.FileOffset != 0) {
				var mod = ILSpyTreeNode.GetModule((ILSpyTreeNode)rsrc);
				if (mod != null && File.Exists(mod.Location))
					return new AddressReference(mod.Location, false, rsrc.FileOffset, rsrc.Length);
			}

			return null;
		}
	}

	struct LengthAndOffset {
		public string Filename;
		public ulong Offset;
		public ulong Size;

		public LengthAndOffset(string filename, ulong offs, ulong size) {
			this.Filename = filename;
			this.Offset = offs;
			this.Size = size;
		}
	}

	interface ITVChangeBodyHexEditorCommand {
		string GetDescription(byte[] data);
		byte[] GetData(MethodDef method);
	}

	static class TVChangeBodyHexEditorCommand {
		internal abstract class TheHexTextEditorCommand : HexTextEditorCommand, ITVChangeBodyHexEditorCommand {
			public abstract byte[] GetData(MethodDef method);
			public abstract string GetDescription(byte[] data);
		}

		internal abstract class TheHexMenuCommand : HexMenuCommand, ITVChangeBodyHexEditorCommand {
			public abstract byte[] GetData(MethodDef method);
			public abstract string GetDescription(byte[] data);
		}

		internal static void ExecuteInternal(ITVChangeBodyHexEditorCommand cmd, HexContext context) {
			var data = GetData(cmd, context);
			if (data == null)
				return;
			var info = GetMethodLengthAndOffset(context);
			if (info == null || info.Value.Size < (ulong)data.Length)
				return;
			WriteHexUndoCommand.AddAndExecute(info.Value.Filename, info.Value.Offset, data, cmd.GetDescription(data));
		}

		internal static bool IsVisibleInternal(ITVChangeBodyHexEditorCommand cmd, HexContext context) {
			var data = GetData(cmd, context);
			if (data == null)
				return false;
			var info = GetMethodLengthAndOffset(context);
			return info != null && info.Value.Size >= (ulong)data.Length;
		}

		static byte[] GetData(ITVChangeBodyHexEditorCommand cmd, HexContext context) {
			var md = TVShowMethodInstructionsInHexEditorCommand.GetMemberDef(context) as MethodDef;
			if (md == null)
				return null;
			return cmd.GetData(md);
		}

		internal static LengthAndOffset? GetMethodLengthAndOffset(HexContext context) {
			var md = TVShowMethodInstructionsInHexEditorCommand.GetMemberDef(context) as MethodDef;
			if (md == null)
				return null;
			var mod = md.Module;
			if (mod == null || !File.Exists(mod.Location))
				return null;
			uint rva;
			long fileOffset;
			if (!md.GetRVA(out rva, out fileOffset))
				return null;

			return new LengthAndOffset(mod.Location, (ulong)fileOffset, InstructionUtils.GetTotalMethodBodyLength(md));
		}
	}

	
	
	static class TVChangeBodyToReturnTrueHexEditorCommand {
		[ExportMenuItem(Header = "Hex Write 'return true' Body", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 90)]
		sealed class TheHexTextEditorCommand : TVChangeBodyHexEditorCommand.TheHexTextEditorCommand {
			public override void Execute(HexContext context) {
				TVChangeBodyHexEditorCommand.ExecuteInternal(this, context);
			}

			public override bool IsVisible(HexContext context) {
				return TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			}

			public override byte[] GetData(MethodDef method) {
				return TVChangeBodyToReturnTrueHexEditorCommand.GetData(method);
			}

			public override string GetDescription(byte[] data) {
				return TVChangeBodyToReturnTrueHexEditorCommand.GetDescription(data);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Hex Write 'return true' Body", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 90)]
		sealed class TheHexMenuCommand : TVChangeBodyHexEditorCommand.TheHexMenuCommand {
			public override void Execute(HexContext context) {
				TVChangeBodyHexEditorCommand.ExecuteInternal(this, context);
			}

			public override bool IsVisible(HexContext context) {
				return TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			}

			public override byte[] GetData(MethodDef method) {
				return TVChangeBodyToReturnTrueHexEditorCommand.GetData(method);
			}

			public override string GetDescription(byte[] data) {
				return TVChangeBodyToReturnTrueHexEditorCommand.GetDescription(data);
			}
		}

		static string GetDescription(byte[] data) {
			return "Hex Write 'return true' Body";
		}

		static byte[] GetData(MethodDef method) {
			if (method.MethodSig.GetRetType().RemovePinnedAndModifiers().GetElementType() != ElementType.Boolean)
				return null;
			return data;
		}
		static readonly byte[] data = new byte[] { 0x0A, 0x17, 0x2A };
	}

	static class TVChangeBodyToReturnFalseHexEditorCommand {
		[ExportMenuItem(Header = "Hex Write 'return false' Body", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 100)]
		sealed class TheHexTextEditorCommand : TVChangeBodyHexEditorCommand.TheHexTextEditorCommand {
			public override void Execute(HexContext context) {
				TVChangeBodyHexEditorCommand.ExecuteInternal(this, context);
			}

			public override bool IsVisible(HexContext context) {
				return TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			}

			public override byte[] GetData(MethodDef method) {
				return TVChangeBodyToReturnFalseHexEditorCommand.GetData(method);
			}

			public override string GetDescription(byte[] data) {
				return TVChangeBodyToReturnFalseHexEditorCommand.GetDescription(data);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Hex Write 'return false' Body", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 100)]
		sealed class TheHexMenuCommand : TVChangeBodyHexEditorCommand.TheHexMenuCommand {
			public override void Execute(HexContext context) {
				TVChangeBodyHexEditorCommand.ExecuteInternal(this, context);
			}

			public override bool IsVisible(HexContext context) {
				return TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			}

			public override byte[] GetData(MethodDef method) {
				return TVChangeBodyToReturnFalseHexEditorCommand.GetData(method);
			}

			public override string GetDescription(byte[] data) {
				return TVChangeBodyToReturnFalseHexEditorCommand.GetDescription(data);
			}
		}

		static string GetDescription(byte[] data) {
			return "Hex Write 'return false' Body";
		}

		static byte[] GetData(MethodDef method) {
			if (method.MethodSig.GetRetType().RemovePinnedAndModifiers().GetElementType() != ElementType.Boolean)
				return null;
			return data;
		}
		static readonly byte[] data = new byte[] { 0x0A, 0x16, 0x2A };
	}

	static class TVWriteEmptyBodyHexEditorCommand {
		[ExportMenuItem(Header = "Hex Write Empty Body", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 110)]
		sealed class TheHexTextEditorCommand : TVChangeBodyHexEditorCommand.TheHexTextEditorCommand {
			public override void Execute(HexContext context) {
				TVChangeBodyHexEditorCommand.ExecuteInternal(this, context);
			}

			public override bool IsVisible(HexContext context) {
				return TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			}

			public override byte[] GetData(MethodDef method) {
				return TVWriteEmptyBodyHexEditorCommand.GetData(method);
			}

			public override string GetDescription(byte[] data) {
				return TVWriteEmptyBodyHexEditorCommand.GetDescription(data);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Hex Write Empty Body", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 110)]
		sealed class TheHexMenuCommand : TVChangeBodyHexEditorCommand.TheHexMenuCommand {
			public override void Execute(HexContext context) {
				TVChangeBodyHexEditorCommand.ExecuteInternal(this, context);
			}

			public override bool IsVisible(HexContext context) {
				return TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			}

			public override byte[] GetData(MethodDef method) {
				return TVWriteEmptyBodyHexEditorCommand.GetData(method);
			}

			public override string GetDescription(byte[] data) {
				return TVWriteEmptyBodyHexEditorCommand.GetDescription(data);
			}
		}

		static string GetDescription(byte[] data) {
			return "Hex Write Empty Body";
		}

		static byte[] GetData(MethodDef method) {
			var sig = method.MethodSig.GetRetType().RemovePinnedAndModifiers();

			// This is taken care of by the write 'return true/false' commands
			if (sig.GetElementType() == ElementType.Boolean)
				return null;

			return GetData(sig, 0);
		}

		static byte[] GetData(TypeSig typeSig, int level) {
			if (level >= 10)
				return null;
			var retType = typeSig.RemovePinnedAndModifiers();
			if (retType == null)
				return null;

			switch (retType.ElementType) {
			case ElementType.Void:
				return dataVoidReturnType;

			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
				return dataInt32ReturnType;

			case ElementType.I8:
			case ElementType.U8:
				return dataInt64ReturnType;

			case ElementType.R4:
				return dataSingleReturnType;

			case ElementType.R8:
				return dataDoubleReturnType;

			case ElementType.I:
				return dataIntPtrReturnType;

			case ElementType.U:
			case ElementType.Ptr:
			case ElementType.FnPtr:
				return dataUIntPtrReturnType;

			case ElementType.ValueType:
				var td = ((ValueTypeSig)retType).TypeDefOrRef.ResolveTypeDef();
				if (td != null && td.IsEnum) {
					var undType = td.GetEnumUnderlyingType().RemovePinnedAndModifiers();
					var et = undType.GetElementType();
					if ((ElementType.Boolean <= et && et <= ElementType.R8) || et == ElementType.I || et == ElementType.U)
						return GetData(undType, level + 1);
				}
				goto case ElementType.TypedByRef;

			case ElementType.TypedByRef:
			case ElementType.Var:
			case ElementType.MVar:
				// Need ldloca, initobj, ldloc and a local variable
				return null;

			case ElementType.GenericInst:
				if (((GenericInstSig)retType).GenericType is ValueTypeSig)
					goto case ElementType.TypedByRef;
				goto case ElementType.Class;

			case ElementType.End:
			case ElementType.String:
			case ElementType.ByRef:
			case ElementType.Class:
			case ElementType.Array:
			case ElementType.ValueArray:
			case ElementType.R:
			case ElementType.Object:
			case ElementType.SZArray:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Internal:
			case ElementType.Module:
			case ElementType.Sentinel:
			case ElementType.Pinned:
			default:
				return dataRefTypeReturnType;
			}
		}

		static readonly byte[] dataVoidReturnType = new byte[] { 0x06, 0x2A };	// ret
		static readonly byte[] dataInt32ReturnType = new byte[] { 0x0A, 0x16, 0x2A };	// ldc.i4.0, ret
		static readonly byte[] dataInt64ReturnType = new byte[] { 0x0E, 0x16, 0x6A, 0x2A };	// ldc.i4.0, conv.i8, ret
		static readonly byte[] dataSingleReturnType = new byte[] { 0x0E, 0x16, 0x6B, 0x2A };	// ldc.i4.0, conv.r4, ret
		static readonly byte[] dataDoubleReturnType = new byte[] { 0x0E, 0x16, 0x6C, 0x2A };	// ldc.i4.0, conv.r8, ret
		static readonly byte[] dataIntPtrReturnType = new byte[] { 0x0E, 0x16, 0xD3, 0x2A };    // ldc.i4.0, conv.i, ret
		static readonly byte[] dataUIntPtrReturnType = new byte[] { 0x0E, 0x16, 0xE0, 0x2A };    // ldc.i4.0, conv.u, ret
		static readonly byte[] dataRefTypeReturnType = new byte[] { 0x0A, 0x14, 0x2A };	// ldnull, ret
	}

	static class TVCopyMethodBodyHexEditorCommand {
		[ExportMenuItem(Header = "Hex Copy Method Body", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 120)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Hex Copy Method Body", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 120)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		static void ExecuteInternal(HexContext context) {
			var data = GetMethodBodyBytes(context);
			if (data == null)
				return;
			ClipboardUtils.SetText(ClipboardUtils.ToHexString(data));
		}

		static bool IsVisibleInternal(HexContext context) {
			return TVChangeBodyHexEditorCommand.GetMethodLengthAndOffset(context) != null;
		}

		static byte[] GetMethodBodyBytes(HexContext context) {
			var info = TVChangeBodyHexEditorCommand.GetMethodLengthAndOffset(context);
			if (info == null || info.Value.Size > int.MaxValue)
				return null;
			var doc = HexDocumentManager.Instance.GetOrCreate(info.Value.Filename);
			if (doc == null)
				return null;
			return doc.ReadBytes(info.Value.Offset, (int)info.Value.Size);
		}
	}

	static class TVPasteMethodBodyHexEditorCommand {
		[ExportMenuItem(Header = "Hex Paste Method Body", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 130)]
		sealed class TheHexTextEditorCommand : TVChangeBodyHexEditorCommand.TheHexTextEditorCommand {
			public override void Execute(HexContext context) {
				TVChangeBodyHexEditorCommand.ExecuteInternal(this, context);
			}

			public override bool IsVisible(HexContext context) {
				return TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			}

			public override byte[] GetData(MethodDef method) {
				return TVPasteMethodBodyHexEditorCommand.GetData(method);
			}

			public override string GetDescription(byte[] data) {
				return TVPasteMethodBodyHexEditorCommand.GetDescription(data);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Hex Paste Method Body", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 130)]
		sealed class TheHexMenuCommand : TVChangeBodyHexEditorCommand.TheHexMenuCommand {
			public override void Execute(HexContext context) {
				TVChangeBodyHexEditorCommand.ExecuteInternal(this, context);
			}

			public override bool IsVisible(HexContext context) {
				return TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			}

			public override byte[] GetData(MethodDef method) {
				return TVPasteMethodBodyHexEditorCommand.GetData(method);
			}

			public override string GetDescription(byte[] data) {
				return TVPasteMethodBodyHexEditorCommand.GetDescription(data);
			}
		}

		static string GetDescription(byte[] data) {
			return "Hex Paste Method Body";
		}

		static byte[] GetData(MethodDef method) {
			return ClipboardUtils.GetData();
		}
	}

	static class GoToMDTableRowHexEditorCommand {
		internal static void OnLoaded() {
			MainWindow.Instance.CodeBindings.Add(new RoutedCommand("GoToMDTableRow", typeof(GoToMDTableRowHexEditorCommand)),
				(s, e) => Execute(),
				(s, e) => e.CanExecute = CanExecute(),
				ModifierKeys.Shift | ModifierKeys.Alt, Key.R);
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_CODE_HEX_GOTO_MD, Order = 0)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}

			public override string GetHeader(HexContext context) {
				return GetHeaderInternal(context);
			}

			public override string GetInputGestureText(HexContext context) {
				return GetInputGestureTextInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_GOTO_MD, Order = 0)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}

			public override string GetHeader(HexContext context) {
				return GetHeaderInternal(context);
			}

			public override string GetInputGestureText(HexContext context) {
				return GetInputGestureTextInternal(context);
			}
		}

		static void Execute() {
			ExecuteInternal(HexMenuCommand.CreateContext());
		}

		static bool CanExecute() {
			return IsVisibleInternal(HexMenuCommand.CreateContext());
		}

		static string GetHeaderInternal(HexContext context) {
			var tokRef = GetTokenReference(context);
			return string.Format("Go to MD Table Row ({0:X8})", tokRef.Token);
		}

		static string GetInputGestureTextInternal(HexContext context) {
			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID) || context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID))
				return "Shift+Alt+R";
			return null;
		}

		internal static void ExecuteInternal(HexContext context) {
			var @ref = GetTokenReference(context);
			if (@ref != null)
				MainWindow.Instance.JumpToReference(@ref);
		}

		internal static bool IsVisibleInternal(HexContext context) {
			return GetTokenReference(context) != null;
		}

		static TokenReference GetTokenReference(HexContext context) {
			var @ref = GetTokenReference2(context);
			if (@ref == null)
				return null;
			var node = MainWindow.Instance.DnSpyFileListTreeNode.FindModuleNode(@ref.ModuleDef);
			return HasPENode(node) ? @ref : null;
		}

		internal static bool HasPENode(AssemblyTreeNode node) {
			if (node == null)
				return false;
			// Currently only nodes loaded from files on disk have a PE node
			return node.DnSpyFile.PEImage != null && node.DnSpyFile.LoadedFromFile;
		}

		static TokenReference GetTokenReference2(HexContext context) {
			if (context == null)
				return null;
			if (context.Reference != null) {
				var tokRef = context.Reference as TokenReference;
				if (tokRef != null)
					return tokRef;

				var mr = context.Reference as IMemberRef;
				if (mr != null)
					return CreateTokenReference(mr.Module, mr);

				var p = context.Reference as Parameter;
				if (p != null) {
					var pd = p.ParamDef;
					if (pd != null && pd.DeclaringMethod != null)
						return CreateTokenReference(pd.DeclaringMethod.Module, pd);
				}
			}
			if (context.Nodes != null && context.Nodes.Length == 1) {
				var node = context.Nodes[0] as ITokenTreeNode;
				if (node != null && node.MDTokenProvider != null) {
					var mod = ILSpyTreeNode.GetModule((SharpTreeNode)node);
					if (mod != null)
						return new TokenReference(mod, node.MDTokenProvider.MDToken.Raw);
				}
			}

			return null;
		}

		static TokenReference CreateTokenReference(ModuleDef module, IMDTokenProvider @ref) {
			if (module == null || @ref == null)
				return null;
			// Make sure it's not a created method/field/etc
			var res = module.ResolveToken(@ref.MDToken.Raw);
			if (res == null)
				return null;
			return new TokenReference(module, @ref.MDToken.Raw);
		}
	}

	static class GoToMDTableRowUIHexEditorCommand {
		internal static void OnLoaded() {
			MainWindow.Instance.CodeBindings.Add(new RoutedCommand("GoToMDTableRowUI", typeof(GoToMDTableRowUIHexEditorCommand)),
				(s, e) => Execute(),
				(s, e) => e.CanExecute = CanExecute(),
				ModifierKeys.Control | ModifierKeys.Shift, Key.D);
		}

		[ExportMenuItem(Header = "Go to MD Table Row...", InputGestureText = "Ctrl+Shift+D", Group = MenuConstants.GROUP_CTX_CODE_HEX_GOTO_MD, Order = 10)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Go to MD Table Row...", InputGestureText = "Ctrl+Shift+D", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_GOTO_MD, Order = 10)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			public override void Execute(HexContext context) {
				ExecuteInternal(context);
			}

			public override bool IsVisible(HexContext context) {
				return IsVisibleInternal(context);
			}
		}

		static void Execute() {
			Execute2(HexMenuCommand.CreateContext());
		}

		static bool CanExecute() {
			return CanExecute(HexMenuCommand.CreateContext());
		}

		static void ExecuteInternal(HexContext context) {
			Execute2(context);
		}

		static bool IsVisibleInternal(HexContext context) {
			return CanExecute(context);
		}

		static bool CanExecute(HexContext context) {
			DecompileTabState tabState;
			return GetModule(context, out tabState) != null;
		}

		static ModuleDef GetModule(HexContext context, out DecompileTabState tabState) {
			tabState = null;
			if (context == null)
				return null;

			var textView = context.CreatorObject.Object as DecompilerTextView;
			if (textView != null) {
				tabState = DecompileTabState.GetDecompileTabState(textView);
				if (tabState != null && tabState.DecompiledNodes != null && tabState.DecompiledNodes.Length > 0)
					return GetModule(ILSpyTreeNode.GetNode<AssemblyTreeNode>(tabState.DecompiledNodes[0]));
			}

			if (context.Nodes != null && context.Nodes.Length == 1)
				return GetModule(ILSpyTreeNode.GetNode<AssemblyTreeNode>(context.Nodes[0]));

			return null;
		}

		static ModuleDef GetModule(AssemblyTreeNode node) {
			return GoToMDTableRowHexEditorCommand.HasPENode(node) ? node.DnSpyFile.ModuleDef : null;
		}

		static void Execute2(HexContext context) {
			DecompileTabState tabState;
			var module = GetModule(context, out tabState);
			if (module == null)
				return;

			uint? token = GoToTokenCommand.AskForToken("Go to MD Table Row");
			if (token == null)
				return;

			var tokRef = new TokenReference(module, token.Value);
			if (MainWindow.Instance.DnSpyFileListTreeNode.FindTokenNode(tokRef) == null) {
				MainWindow.Instance.ShowMessageBox(string.Format("Token {0:X8} doesn't exist in the metadata", token.Value));
				return;
			}

			if (tabState != null)
				MainWindow.Instance.JumpToReference(tabState.TextView, tokRef);
			else
				MainWindow.Instance.JumpToReference(tokRef);
		}
	}
}
