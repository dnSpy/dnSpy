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
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Hex.Nodes;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.AsmEditor.Utilities;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.HexEditor;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;
using dnSpy.Decompiler.Shared;

namespace dnSpy.AsmEditor.Hex {
	[ExportAutoLoaded]
	sealed class HexCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		HexCommandLoader(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations) {
			OpenHexEditorCommand.Initialize(wpfCommandManager, fileTabManager, methodAnnotations);
			GoToMDTableRowHexEditorCommand.Initialize(wpfCommandManager, fileTabManager);
			GoToMDTableRowUIHexEditorCommand.Initialize(wpfCommandManager, fileTabManager);
		}
	}

	sealed class HexContext {
		public ITreeNodeData[] Nodes { get; }
		public bool IsDefinition { get; }
		public object Reference { get; }
		public int? Line { get; }
		public int? Column { get; }
		public GuidObject CreatorObject { get; }

		public HexContext() {
		}

		public HexContext(GuidObject creatorObject, ITreeNodeData[] nodes) {
			this.Nodes = nodes;
			this.CreatorObject = creatorObject;
		}

		public HexContext(IDocumentViewer documentViewer, int? line, int? col, object @ref, bool isDefinition) {
			this.Reference = @ref;
			this.IsDefinition = isDefinition;
			this.Line = line;
			this.Column = col;
			this.CreatorObject = new GuidObject(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID, documentViewer);
		}
	}

	abstract class HexTextEditorCommand : MenuItemBase<HexContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override HexContext CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID)) {
				var textRef = context.Find<TextReference>();
				bool isDefinition = false;
				object @ref = null;
				if (textRef != null) {
					@ref = textRef.Reference;
					isDefinition = textRef.IsDefinition;
				}
				var pos = context.Find<TextEditorLocation?>();
				return new HexContext(context.Find<IDocumentViewer>(), pos == null ? (int?)null : pos.Value.Line, pos == null ? (int?)null : pos.Value.Column, @ref, isDefinition);
			}

			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID)) {
				var nodes = context.Find<ITreeNodeData[]>();
				if (nodes == null)
					return null;
				return new HexContext(context.CreatorObject, nodes);
			}

			return null;
		}

		public override bool IsEnabled(HexContext context) => true;
	}

	abstract class HexMenuCommand : MenuItemBase<HexContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly IFileTabManager fileTabManager;

		protected HexMenuCommand(IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
		}

		protected sealed override HexContext CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.APP_MENU_EDIT_GUID))
				return null;
			return CreateContext(fileTabManager);
		}

		internal static HexContext CreateContext(IFileTabManager fileTabManager) {
			var documentViewer = fileTabManager.ActiveTab.TryGetDocumentViewer();
			if (documentViewer != null && documentViewer.UIObject.IsKeyboardFocusWithin)
				return CreateContext(documentViewer);

			if (((UIElement)fileTabManager.FileTreeView.TreeView.UIObject).IsKeyboardFocusWithin)
				return CreateContext(fileTabManager.FileTreeView);

			if (fileTabManager.FileTreeView.TreeView.SelectedItems.Length != 0) {
				bool teFocus = documentViewer != null;
				if (teFocus)
					return CreateContext(documentViewer);
				if (UIUtils.HasSelectedChildrenFocus(fileTabManager.FileTreeView.TreeView.UIObject as ListBox))
					return CreateContext(fileTabManager.FileTreeView);
			}

			return new HexContext();
		}

		static HexContext CreateContext(IDocumentViewer documentViewer) {
			var refInfo = documentViewer.SelectedReferenceInfo;
			bool isDefinition = false;
			object @ref = null;
			if (refInfo != null) {
				@ref = refInfo.Value.Data.Reference;
				isDefinition = refInfo.Value.Data.IsDefinition;
			}
			var pos = documentViewer.CaretLocation;
			return new HexContext(documentViewer, pos.Line, pos.Column, @ref, isDefinition);
		}

		static HexContext CreateContext(IFileTreeView fileTreeView) => new HexContext(new GuidObject(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID, fileTreeView), fileTreeView.TreeView.TopLevelSelection);
		public override bool IsEnabled(HexContext context) => true;
	}

	static class OpenHexEditorCommand {
		static readonly RoutedCommand OpenHexEditor = new RoutedCommand("OpenHexEditor", typeof(OpenHexEditorCommand));
		internal static void Initialize(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(OpenHexEditor,
				(s, e) => ExecuteCommand(fileTabManager, methodAnnotations),
				(s, e) => e.CanExecute = CanExecuteCommand(fileTabManager, methodAnnotations),
				ModifierKeys.Control, Key.X);
		}

		[ExportMenuItem(Header = "res:OpenHexEditorCommand", Icon = "Binary", InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 0)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			readonly IFileTabManager fileTabManager;
			readonly Lazy<IMethodAnnotations> methodAnnotations;

			[ImportingConstructor]
			TheHexTextEditorCommand(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations) {
				this.fileTabManager = fileTabManager;
				this.methodAnnotations = methodAnnotations;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, methodAnnotations, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(fileTabManager, methodAnnotations, context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:OpenHexEditorCommand", Icon = "Binary", InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 0)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			readonly Lazy<IMethodAnnotations> methodAnnotations;

			[ImportingConstructor]
			TheHexMenuCommand(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations)
				: base(fileTabManager) {
				this.methodAnnotations = methodAnnotations;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, methodAnnotations, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(fileTabManager, methodAnnotations, context);
		}

		static void ExecuteCommand(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations) {
			var context = HexMenuCommand.CreateContext(fileTabManager);
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				ShowAddressReferenceInHexEditorCommand.ExecuteInternal(fileTabManager, context);
			else if (ShowILRangeInHexEditorCommand.IsVisibleInternal(methodAnnotations, context))
				ShowILRangeInHexEditorCommand.ExecuteInternal(fileTabManager, methodAnnotations, context);
			else if (ShowHexNodeInHexEditorCommand.IsVisibleInternal(methodAnnotations, context))
				ShowHexNodeInHexEditorCommand.ExecuteInternal(fileTabManager, methodAnnotations, context);
			else if (IsVisibleInternal(fileTabManager, methodAnnotations, context))
				ExecuteInternal(fileTabManager, methodAnnotations, context);
		}

		static bool CanExecuteCommand(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations) {
			var context = HexMenuCommand.CreateContext(fileTabManager);
			return ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context) ||
				ShowILRangeInHexEditorCommand.IsVisibleInternal(methodAnnotations, context) ||
				ShowHexNodeInHexEditorCommand.IsVisibleInternal(methodAnnotations, context) ||
				IsVisibleInternal(fileTabManager, methodAnnotations, context);
		}

		internal static void ExecuteInternal(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations, HexContext context) {
			var node = GetNode(fileTabManager, methodAnnotations, context);
			if (node != null) {
				var tab = fileTabManager.ActiveTab;
				var uiContext = tab?.UIContext as HexBoxFileTabUIContext;
				if (uiContext == null)
					fileTabManager.FollowReference(new AddressReference(node.DnSpyFile.Filename, false, 0, 0));
			}
		}

		static bool IsVisibleInternal(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations, HexContext context) {
			var node = GetNode(fileTabManager, methodAnnotations, context);
			return node != null && !string.IsNullOrEmpty(node.DnSpyFile.Filename);
		}

		static IDnSpyFileNode GetDnSpyFileNode(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations, HexContext context) {
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (ShowILRangeInHexEditorCommand.IsVisibleInternal(methodAnnotations, context))
				return null;
			if (ShowHexNodeInHexEditorCommand.IsVisibleInternal(methodAnnotations, context))
				return null;
			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return GetActiveAssemblyTreeNode(fileTabManager);
			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID)) {
				return context.Nodes != null &&
					context.Nodes.Length == 1 ?
					context.Nodes[0] as IDnSpyFileNode : null;
			}
			return null;
		}

		static IDnSpyFileNode GetActiveAssemblyTreeNode(IFileTabManager fileTabManager) {
			var tab = fileTabManager.ActiveTab;
			if (tab == null)
				return null;
			var node = tab.Content.Nodes.FirstOrDefault();
			return node.GetDnSpyFileNode();
		}

		static IDnSpyFileNode GetNode(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations, HexContext context) => GetDnSpyFileNode(fileTabManager, methodAnnotations, context);
	}

	static class ShowAddressReferenceInHexEditorCommand {
		[ExportMenuItem(Header = "res:ShowInHexEditorCommand", Icon = "Binary", InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 10)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			readonly IFileTabManager fileTabManager;

			[ImportingConstructor]
			TheHexTextEditorCommand(IFileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ShowInHexEditorCommand", Icon = "Binary", InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 10)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			[ImportingConstructor]
			TheHexMenuCommand(IFileTabManager fileTabManager)
				: base(fileTabManager) {
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(context);
		}

		internal static void ExecuteInternal(IFileTabManager fileTabManager, HexContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				fileTabManager.FollowReference(@ref);
		}

		internal static bool IsVisibleInternal(HexContext context) => GetAddressReference(context) != null;

		static AddressReference GetAddressReference(HexContext context) {
			if (context.Reference == null)
				return null;

			var addr = context.Reference as AddressReference;
			if (addr != null && File.Exists(addr.Filename))
				return addr;

			var rsrc = context.Reference as IResourceDataProvider;
			if (rsrc != null && rsrc.FileOffset != 0) {
				var name = GetFilename((IFileTreeNodeData)rsrc);
				if (!string.IsNullOrEmpty(name))
					return new AddressReference(name, false, rsrc.FileOffset, rsrc.Length);
			}

			return null;
		}

		internal static string GetFilename(IFileTreeNodeData node) {
			var fileNode = node.GetDnSpyFileNode();
			if (fileNode == null)
				return null;
			var mod = fileNode.DnSpyFile.ModuleDef;
			if (mod != null && File.Exists(mod.Location))
				return mod.Location;
			var peImage = fileNode.DnSpyFile.PEImage;
			if (peImage != null && File.Exists(peImage.FileName))
				return peImage.FileName;
			return null;
		}
	}

	static class ShowILRangeInHexEditorCommand {
		[ExportMenuItem(Header = "res:ShowInstrsInHexEditorCommand", Icon = "Binary", InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 20)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			readonly IFileTabManager fileTabManager;
			readonly Lazy<IMethodAnnotations> methodAnnotations;

			[ImportingConstructor]
			TheHexTextEditorCommand(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations) {
				this.fileTabManager = fileTabManager;
				this.methodAnnotations = methodAnnotations;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, methodAnnotations, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(methodAnnotations, context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ShowInstrsInHexEditorCommand", Icon = "Binary", InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 20)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			readonly Lazy<IMethodAnnotations> methodAnnotations;

			[ImportingConstructor]
			TheHexMenuCommand(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations)
				: base(fileTabManager) {
				this.methodAnnotations = methodAnnotations;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, methodAnnotations, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(methodAnnotations, context);
		}

		internal static void ExecuteInternal(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations, HexContext context) {
			var @ref = GetAddressReference(methodAnnotations, context);
			if (@ref != null)
				fileTabManager.FollowReference(@ref);
		}

		internal static bool IsVisibleInternal(Lazy<IMethodAnnotations> methodAnnotations, HexContext context) => GetAddressReference(methodAnnotations, context) != null;

		static AddressReference GetAddressReference(Lazy<IMethodAnnotations> methodAnnotations, HexContext context) {
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (TVShowMethodInstructionsInHexEditorCommand.IsVisibleInternal(methodAnnotations, context))
				return null;

			var mappings = GetMappings(context);
			if (mappings == null || mappings.Count == 0)
				return null;

			var method = mappings[0].Mapping.Method;
			var mod = mappings[0].Mapping.Method.Module as ModuleDefMD;
			if (mod == null || string.IsNullOrEmpty(mod.Location))
				return null;

			ulong addr = (ulong)method.RVA;
			ulong len;
			if (methodAnnotations.Value.IsBodyModified(method))
				len = 0;
			else if (mappings.Count == 1) {
				addr += (ulong)method.Body.HeaderSize + mappings[0].ILRange.From;
				len = mappings[0].ILRange.To - mappings[0].ILRange.From;
			}
			else {
				addr += (ulong)method.Body.HeaderSize + mappings[0].ILRange.From;
				len = 0;
			}

			return new AddressReference(mod.Location, true, addr, len);
		}

		static IList<SourceCodeMapping> GetMappings(HexContext context) {
			if (context.Line == null || context.Column == null)
				return null;
			return MethodBody.BodyCommandUtils.GetMappings(context.CreatorObject.Object as IDocumentViewer, context.Line.Value, context.Column.Value);
		}
	}

	static class ShowHexNodeInHexEditorCommand {
		[ExportMenuItem(Header = "res:ShowInHexEditorCommand", Icon = "Binary", InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 30)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			readonly IFileTabManager fileTabManager;
			readonly Lazy<IMethodAnnotations> methodAnnotations;

			[ImportingConstructor]
			TheHexTextEditorCommand(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations) {
				this.fileTabManager = fileTabManager;
				this.methodAnnotations = methodAnnotations;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, methodAnnotations, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(methodAnnotations, context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ShowInHexEditorCommand", Icon = "Binary", InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 30)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			readonly Lazy<IMethodAnnotations> methodAnnotations;

			[ImportingConstructor]
			TheHexMenuCommand(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations)
				: base(fileTabManager) {
				this.methodAnnotations = methodAnnotations;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, methodAnnotations, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(methodAnnotations, context);
		}

		internal static void ExecuteInternal(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations, HexContext context) {
			var @ref = GetAddressReference(methodAnnotations, context);
			if (@ref != null)
				fileTabManager.FollowReference(@ref);
		}

		internal static bool IsVisibleInternal(Lazy<IMethodAnnotations> methodAnnotations, HexContext context) => GetAddressReference(methodAnnotations, context) != null;

		static AddressReference GetAddressReference(Lazy<IMethodAnnotations> methodAnnotations, HexContext context) {
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (ShowILRangeInHexEditorCommand.IsVisibleInternal(methodAnnotations, context))
				return null;

			if (context.Nodes == null || context.Nodes.Length != 1)
				return null;
			var hexNode = context.Nodes[0] as HexNode;
			if (hexNode == null)
				return null;

			var name = ShowAddressReferenceInHexEditorCommand.GetFilename(hexNode);
			if (string.IsNullOrEmpty(name))
				return null;

			return new AddressReference(name, false, hexNode.StartOffset, hexNode.StartOffset == 0 && hexNode.EndOffset == ulong.MaxValue ? ulong.MaxValue : hexNode.EndOffset - hexNode.StartOffset + 1);
		}
	}

	static class ShowStorageStreamDataInHexEditorCommand {
		[ExportMenuItem(Header = "res:ShowDataInHexEditorCommand", Icon = "Binary", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 40)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			readonly IFileTabManager fileTabManager;
			readonly Lazy<IMethodAnnotations> methodAnnotations;

			[ImportingConstructor]
			TheHexTextEditorCommand(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations) {
				this.fileTabManager = fileTabManager;
				this.methodAnnotations = methodAnnotations;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, methodAnnotations, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(methodAnnotations, context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ShowDataInHexEditorCommand", Icon = "Binary", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 40)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			readonly Lazy<IMethodAnnotations> methodAnnotations;

			[ImportingConstructor]
			TheHexMenuCommand(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations)
				: base(fileTabManager) {
				this.methodAnnotations = methodAnnotations;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, methodAnnotations, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(methodAnnotations, context);
		}

		internal static void ExecuteInternal(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations, HexContext context) {
			var @ref = GetAddressReference(methodAnnotations, context);
			if (@ref != null)
				fileTabManager.FollowReference(@ref);
		}

		internal static bool IsVisibleInternal(Lazy<IMethodAnnotations> methodAnnotations, HexContext context) => GetAddressReference(methodAnnotations, context) != null;

		static AddressReference GetAddressReference(Lazy<IMethodAnnotations> methodAnnotations, HexContext context) {
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (ShowILRangeInHexEditorCommand.IsVisibleInternal(methodAnnotations, context))
				return null;

			if (context.Nodes == null || context.Nodes.Length != 1)
				return null;
			if (!(context.Nodes[0] is HexNode))
				return null;

			var mod = context.Nodes[0].GetModule() as ModuleDefMD;
			if (mod == null)
				return null;
			var pe = mod.MetaData.PEImage;

			var sectNode = context.Nodes[0] as ImageSectionHeaderNode;
			if (sectNode != null) {
				if (sectNode.SectionNumber >= pe.ImageSectionHeaders.Count)
					return null;
				var sect = pe.ImageSectionHeaders[sectNode.SectionNumber];
				return new AddressReference(mod.Location, false, sect.PointerToRawData, sect.SizeOfRawData);
			}

			var stgNode = context.Nodes[0] as StorageStreamNode;
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
		[ExportMenuItem(Header = "res:ShowInstrsInHexEditorCommand", Icon = "Binary", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 50)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			readonly IFileTabManager fileTabManager;
			readonly Lazy<IMethodAnnotations> methodAnnotations;

			[ImportingConstructor]
			TheHexTextEditorCommand(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations) {
				this.fileTabManager = fileTabManager;
				this.methodAnnotations = methodAnnotations;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, methodAnnotations, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(methodAnnotations, context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ShowInstrsInHexEditorCommand", Icon = "Binary", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 50)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			readonly Lazy<IMethodAnnotations> methodAnnotations;

			[ImportingConstructor]
			TheHexMenuCommand(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations)
				: base(fileTabManager) {
				this.methodAnnotations = methodAnnotations;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, methodAnnotations, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(methodAnnotations, context);
		}

		static void ExecuteInternal(IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations, HexContext context) {
			var @ref = GetAddressReference(methodAnnotations, context);
			if (@ref != null)
				fileTabManager.FollowReference(@ref);
		}

		internal static bool IsVisibleInternal(Lazy<IMethodAnnotations> methodAnnotations, HexContext context) => GetAddressReference(methodAnnotations, context) != null;

		static IMemberDef ResolveDef(object mr) {
			if (mr is ITypeDefOrRef)
				return ((ITypeDefOrRef)mr).ResolveTypeDef();
			if (mr is IMethod && ((IMethod)mr).IsMethod)
				return ((IMethod)mr).ResolveMethodDef();
			if (mr is IField)
				return ((IField)mr).ResolveFieldDef();
			return mr as IMemberDef;
		}

		internal static IMemberDef GetMemberDef(HexContext context) {
			IMemberDef def = null;
			if (context.Nodes != null && context.Nodes.Length == 1 && context.Nodes[0] is IMDTokenNode)
				def = ResolveDef(((IMDTokenNode)context.Nodes[0]).Reference);
			else {
				// Only allow declarations of the defs, i.e., right-clicking a method call with a method
				// def as reference should return null, not the method def.
				if (context.Reference != null && context.IsDefinition && context.Reference is IMemberRef) {
					// Don't resolve it. It's confusing if we show the method body of a called method
					// instead of the current method.
					def = context.Reference as IMemberDef;
				}
			}
			var mod = def?.Module;
			return mod is ModuleDefMD ? def : null;
		}

		static AddressReference GetAddressReference(Lazy<IMethodAnnotations> methodAnnotations, HexContext context) {
			var md = GetMemberDef(context) as MethodDef;
			if (md == null)
				return null;
			var body = md.Body;
			if (body == null)
				return null;

			var mod = md.Module;
			bool modified = methodAnnotations.Value.IsBodyModified(md);
			return new AddressReference(mod?.Location, true, (ulong)md.RVA + body.HeaderSize, modified ? 0 : (ulong)body.GetCodeSize());
		}
	}

	static class TVShowMethodHeaderInHexEditorCommand {
		[ExportMenuItem(Header = "res:ShowMethodBodyInHexEditorCommand", Icon = "Binary", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 60)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			readonly IFileTabManager fileTabManager;

			[ImportingConstructor]
			TheHexTextEditorCommand(IFileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ShowMethodBodyInHexEditorCommand", Icon = "Binary", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 60)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			[ImportingConstructor]
			TheHexMenuCommand(IFileTabManager fileTabManager)
				: base(fileTabManager) {
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(context);
		}

		static void ExecuteInternal(IFileTabManager fileTabManager, HexContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				fileTabManager.FollowReference(@ref);
		}

		static bool IsVisibleInternal(HexContext context) => GetAddressReference(context) != null;

		static AddressReference GetAddressReference(HexContext context) {
			var info = TVChangeBodyHexEditorCommand.GetMethodLengthAndOffset(context);
			if (info != null)
				return new AddressReference(info.Value.Filename, false, info.Value.Offset, info.Value.Size);

			return null;
		}
	}

	static class TVShowFieldInitialValueInHexEditorCommand {
		[ExportMenuItem(Header = "res:ShowInitialValueInHexEditorCommand", Icon = "Binary", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 70)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			readonly IFileTabManager fileTabManager;

			[ImportingConstructor]
			TheHexTextEditorCommand(IFileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ShowInitialValueInHexEditorCommand", Icon = "Binary", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 70)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			[ImportingConstructor]
			TheHexMenuCommand(IFileTabManager fileTabManager)
				: base(fileTabManager) {
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(context);
		}

		static void ExecuteInternal(IFileTabManager fileTabManager, HexContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				fileTabManager.FollowReference(@ref);
		}

		static bool IsVisibleInternal(HexContext context) => GetAddressReference(context) != null;

		static AddressReference GetAddressReference(HexContext context) {
			var fd = TVShowMethodInstructionsInHexEditorCommand.GetMemberDef(context) as FieldDef;
			if (fd == null || fd.RVA == 0)
				return null;
			var iv = fd.InitialValue;
			if (iv == null)
				return null;

			var mod = fd.Module;
			return new AddressReference(mod?.Location, true, (ulong)fd.RVA, (ulong)iv.Length);
		}
	}

	static class TVShowResourceInHexEditorCommand {
		[ExportMenuItem(Header = "res:ShowInHexEditorCommand2", Icon = "Binary", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 80)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			readonly IFileTabManager fileTabManager;

			[ImportingConstructor]
			TheHexTextEditorCommand(IFileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ShowInHexEditorCommand2", Icon = "Binary", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 80)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			[ImportingConstructor]
			TheHexMenuCommand(IFileTabManager fileTabManager)
				: base(fileTabManager) {
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(context);
		}

		static void ExecuteInternal(IFileTabManager fileTabManager, HexContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				fileTabManager.FollowReference(@ref);
		}

		static bool IsVisibleInternal(HexContext context) => GetAddressReference(context) != null;

		static AddressReference GetAddressReference(HexContext context) {
			if (context.Nodes == null || context.Nodes.Length != 1)
				return null;

			var rsrc = context.Nodes[0] as IResourceDataProvider;
			if (rsrc != null && rsrc.FileOffset != 0) {
				var mod = (rsrc as IFileTreeNodeData).GetModule();
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

			protected TheHexMenuCommand(IFileTabManager fileTabManager)
				: base(fileTabManager) {
			}
		}

		internal static void ExecuteInternal(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IHexDocumentManager> hexDocumentManager, ITVChangeBodyHexEditorCommand cmd, HexContext context) {
			var data = GetData(cmd, context);
			if (data == null)
				return;
			var info = GetMethodLengthAndOffset(context);
			if (info == null || info.Value.Size < (ulong)data.Length)
				return;
			WriteHexUndoCommand.AddAndExecute(undoCommandManager.Value, hexDocumentManager.Value, info.Value.Filename, info.Value.Offset, data, cmd.GetDescription(data));
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
		[ExportMenuItem(Header = "res:HexWriteReturnTrueBodyCommand", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 90)]
		sealed class TheHexTextEditorCommand : TVChangeBodyHexEditorCommand.TheHexTextEditorCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IHexDocumentManager> hexDocumentManager;

			[ImportingConstructor]
			TheHexTextEditorCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IHexDocumentManager> hexDocumentManager) {
				this.undoCommandManager = undoCommandManager;
				this.hexDocumentManager = hexDocumentManager;
			}

			public override void Execute(HexContext context) =>
				TVChangeBodyHexEditorCommand.ExecuteInternal(undoCommandManager, hexDocumentManager, this, context);
			public override bool IsVisible(HexContext context) => TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			public override byte[] GetData(MethodDef method) => TVChangeBodyToReturnTrueHexEditorCommand.GetData(method);
			public override string GetDescription(byte[] data) => TVChangeBodyToReturnTrueHexEditorCommand.GetDescription(data);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:HexWriteReturnTrueBodyCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 90)]
		sealed class TheHexMenuCommand : TVChangeBodyHexEditorCommand.TheHexMenuCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IHexDocumentManager> hexDocumentManager;

			[ImportingConstructor]
			TheHexMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IHexDocumentManager> hexDocumentManager, IFileTabManager fileTabManager)
				: base(fileTabManager) {
				this.undoCommandManager = undoCommandManager;
				this.hexDocumentManager = hexDocumentManager;
			}

			public override void Execute(HexContext context) =>
				TVChangeBodyHexEditorCommand.ExecuteInternal(undoCommandManager, hexDocumentManager, this, context);
			public override bool IsVisible(HexContext context) => TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			public override byte[] GetData(MethodDef method) => TVChangeBodyToReturnTrueHexEditorCommand.GetData(method);
			public override string GetDescription(byte[] data) => TVChangeBodyToReturnTrueHexEditorCommand.GetDescription(data);
		}

		static string GetDescription(byte[] data) => dnSpy_AsmEditor_Resources.HexWriteReturnTrueBodyCommand;

		static byte[] GetData(MethodDef method) {
			if (method.MethodSig.GetRetType().RemovePinnedAndModifiers().GetElementType() != ElementType.Boolean)
				return null;
			return data;
		}
		static readonly byte[] data = new byte[] { 0x0A, 0x17, 0x2A };
	}

	static class TVChangeBodyToReturnFalseHexEditorCommand {
		[ExportMenuItem(Header = "res:HexWriteReturnFalseBodyCommand", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 100)]
		sealed class TheHexTextEditorCommand : TVChangeBodyHexEditorCommand.TheHexTextEditorCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IHexDocumentManager> hexDocumentManager;

			[ImportingConstructor]
			TheHexTextEditorCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IHexDocumentManager> hexDocumentManager) {
				this.undoCommandManager = undoCommandManager;
				this.hexDocumentManager = hexDocumentManager;
			}

			public override void Execute(HexContext context) =>
				TVChangeBodyHexEditorCommand.ExecuteInternal(undoCommandManager, hexDocumentManager, this, context);
			public override bool IsVisible(HexContext context) => TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			public override byte[] GetData(MethodDef method) => TVChangeBodyToReturnFalseHexEditorCommand.GetData(method);
			public override string GetDescription(byte[] data) => TVChangeBodyToReturnFalseHexEditorCommand.GetDescription(data);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:HexWriteReturnFalseBodyCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 100)]
		sealed class TheHexMenuCommand : TVChangeBodyHexEditorCommand.TheHexMenuCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IHexDocumentManager> hexDocumentManager;

			[ImportingConstructor]
			TheHexMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IHexDocumentManager> hexDocumentManager, IFileTabManager fileTabManager)
				: base(fileTabManager) {
				this.undoCommandManager = undoCommandManager;
				this.hexDocumentManager = hexDocumentManager;
			}

			public override void Execute(HexContext context) =>
				TVChangeBodyHexEditorCommand.ExecuteInternal(undoCommandManager, hexDocumentManager, this, context);
			public override bool IsVisible(HexContext context) => TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			public override byte[] GetData(MethodDef method) => TVChangeBodyToReturnFalseHexEditorCommand.GetData(method);
			public override string GetDescription(byte[] data) => TVChangeBodyToReturnFalseHexEditorCommand.GetDescription(data);
		}

		static string GetDescription(byte[] data) => dnSpy_AsmEditor_Resources.HexWriteReturnFalseBodyCommand;

		static byte[] GetData(MethodDef method) {
			if (method.MethodSig.GetRetType().RemovePinnedAndModifiers().GetElementType() != ElementType.Boolean)
				return null;
			return data;
		}
		static readonly byte[] data = new byte[] { 0x0A, 0x16, 0x2A };
	}

	static class TVWriteEmptyBodyHexEditorCommand {
		[ExportMenuItem(Header = "res:HexWriteEmptyMethodBodyCommand", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 110)]
		sealed class TheHexTextEditorCommand : TVChangeBodyHexEditorCommand.TheHexTextEditorCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IHexDocumentManager> hexDocumentManager;

			[ImportingConstructor]
			TheHexTextEditorCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IHexDocumentManager> hexDocumentManager) {
				this.undoCommandManager = undoCommandManager;
				this.hexDocumentManager = hexDocumentManager;
			}

			public override void Execute(HexContext context) =>
				TVChangeBodyHexEditorCommand.ExecuteInternal(undoCommandManager, hexDocumentManager, this, context);
			public override bool IsVisible(HexContext context) => TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			public override byte[] GetData(MethodDef method) => TVWriteEmptyBodyHexEditorCommand.GetData(method);
			public override string GetDescription(byte[] data) => TVWriteEmptyBodyHexEditorCommand.GetDescription(data);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:HexWriteEmptyMethodBodyCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 110)]
		sealed class TheHexMenuCommand : TVChangeBodyHexEditorCommand.TheHexMenuCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IHexDocumentManager> hexDocumentManager;

			[ImportingConstructor]
			TheHexMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IHexDocumentManager> hexDocumentManager, IFileTabManager fileTabManager)
				: base(fileTabManager) {
				this.undoCommandManager = undoCommandManager;
				this.hexDocumentManager = hexDocumentManager;
			}

			public override void Execute(HexContext context) =>
				TVChangeBodyHexEditorCommand.ExecuteInternal(undoCommandManager, hexDocumentManager, this, context);
			public override bool IsVisible(HexContext context) => TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			public override byte[] GetData(MethodDef method) => TVWriteEmptyBodyHexEditorCommand.GetData(method);
			public override string GetDescription(byte[] data) => TVWriteEmptyBodyHexEditorCommand.GetDescription(data);
		}

		static string GetDescription(byte[] data) => dnSpy_AsmEditor_Resources.HexWriteEmptyMethodBodyCommand;

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

		static readonly byte[] dataVoidReturnType = new byte[] { 0x06, 0x2A };					// ret
		static readonly byte[] dataInt32ReturnType = new byte[] { 0x0A, 0x16, 0x2A };			// ldc.i4.0, ret
		static readonly byte[] dataInt64ReturnType = new byte[] { 0x0E, 0x16, 0x6A, 0x2A };		// ldc.i4.0, conv.i8, ret
		static readonly byte[] dataSingleReturnType = new byte[] { 0x0E, 0x16, 0x6B, 0x2A };	// ldc.i4.0, conv.r4, ret
		static readonly byte[] dataDoubleReturnType = new byte[] { 0x0E, 0x16, 0x6C, 0x2A };	// ldc.i4.0, conv.r8, ret
		static readonly byte[] dataIntPtrReturnType = new byte[] { 0x0E, 0x16, 0xD3, 0x2A };	// ldc.i4.0, conv.i, ret
		static readonly byte[] dataUIntPtrReturnType = new byte[] { 0x0E, 0x16, 0xE0, 0x2A };	// ldc.i4.0, conv.u, ret
		static readonly byte[] dataRefTypeReturnType = new byte[] { 0x0A, 0x14, 0x2A };			// ldnull, ret
	}

	static class TVCopyMethodBodyHexEditorCommand {
		[ExportMenuItem(Header = "res:HexCopyMethodBodyCommand", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 120)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			readonly Lazy<IHexDocumentManager> hexDocumentManager;

			[ImportingConstructor]
			TheHexTextEditorCommand(Lazy<IHexDocumentManager> hexDocumentManager) {
				this.hexDocumentManager = hexDocumentManager;
			}

			public override void Execute(HexContext context) => ExecuteInternal(hexDocumentManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:HexCopyMethodBodyCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 120)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			readonly Lazy<IHexDocumentManager> hexDocumentManager;

			[ImportingConstructor]
			TheHexMenuCommand(Lazy<IHexDocumentManager> hexDocumentManager, IFileTabManager fileTabManager)
				: base(fileTabManager) {
				this.hexDocumentManager = hexDocumentManager;
			}

			public override void Execute(HexContext context) => ExecuteInternal(hexDocumentManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(context);
		}

		static void ExecuteInternal(Lazy<IHexDocumentManager> hexDocumentManager, HexContext context) {
			var data = GetMethodBodyBytes(hexDocumentManager, context);
			if (data == null)
				return;
			ClipboardUtils.SetText(ClipboardUtils.ToHexString(data));
		}

		static bool IsVisibleInternal(HexContext context) => TVChangeBodyHexEditorCommand.GetMethodLengthAndOffset(context) != null;

		static byte[] GetMethodBodyBytes(Lazy<IHexDocumentManager> hexDocumentManager, HexContext context) {
			var info = TVChangeBodyHexEditorCommand.GetMethodLengthAndOffset(context);
			if (info == null || info.Value.Size > int.MaxValue)
				return null;
			var doc = hexDocumentManager.Value.GetOrCreate(info.Value.Filename);
			if (doc == null)
				return null;
			return doc.ReadBytes(info.Value.Offset, (int)info.Value.Size);
		}
	}

	static class TVPasteMethodBodyHexEditorCommand {
		[ExportMenuItem(Header = "res:HexPasteMethodBodyCommand", Group = MenuConstants.GROUP_CTX_CODE_HEX, Order = 130)]
		sealed class TheHexTextEditorCommand : TVChangeBodyHexEditorCommand.TheHexTextEditorCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IHexDocumentManager> hexDocumentManager;

			[ImportingConstructor]
			TheHexTextEditorCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IHexDocumentManager> hexDocumentManager) {
				this.undoCommandManager = undoCommandManager;
				this.hexDocumentManager = hexDocumentManager;
			}

			public override void Execute(HexContext context) =>
				TVChangeBodyHexEditorCommand.ExecuteInternal(undoCommandManager, hexDocumentManager, this, context);
			public override bool IsVisible(HexContext context) => TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			public override byte[] GetData(MethodDef method) => TVPasteMethodBodyHexEditorCommand.GetData(method);
			public override string GetDescription(byte[] data) => TVPasteMethodBodyHexEditorCommand.GetDescription(data);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:HexPasteMethodBodyCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX, Order = 130)]
		sealed class TheHexMenuCommand : TVChangeBodyHexEditorCommand.TheHexMenuCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IHexDocumentManager> hexDocumentManager;

			[ImportingConstructor]
			TheHexMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IHexDocumentManager> hexDocumentManager, IFileTabManager fileTabManager)
				: base(fileTabManager) {
				this.undoCommandManager = undoCommandManager;
				this.hexDocumentManager = hexDocumentManager;
			}

			public override void Execute(HexContext context) =>
				TVChangeBodyHexEditorCommand.ExecuteInternal(undoCommandManager, hexDocumentManager, this, context);
			public override bool IsVisible(HexContext context) => TVChangeBodyHexEditorCommand.IsVisibleInternal(this, context);
			public override byte[] GetData(MethodDef method) => TVPasteMethodBodyHexEditorCommand.GetData(method);
			public override string GetDescription(byte[] data) => TVPasteMethodBodyHexEditorCommand.GetDescription(data);
		}

		static string GetDescription(byte[] data) => dnSpy_AsmEditor_Resources.HexPasteMethodBodyCommand;
		static byte[] GetData(MethodDef method) => ClipboardUtils.GetData();
	}

	static class GoToMDTableRowHexEditorCommand {
		static readonly RoutedCommand GoToMDTableRow = new RoutedCommand("GoToMDTableRow", typeof(GoToMDTableRowHexEditorCommand));
		internal static void Initialize(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(GoToMDTableRow,
				(s, e) => Execute(fileTabManager),
				(s, e) => e.CanExecute = CanExecute(fileTabManager),
				ModifierKeys.Shift | ModifierKeys.Alt, Key.R);
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_CODE_TOKENS, Order = 40)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			readonly IFileTabManager fileTabManager;

			[ImportingConstructor]
			TheHexTextEditorCommand(IFileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(fileTabManager, context);
			public override string GetHeader(HexContext context) => GetHeaderInternal(fileTabManager, context);
			public override string GetInputGestureText(HexContext context) => GetInputGestureTextInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_GOTO_MD, Order = 10)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			[ImportingConstructor]
			TheHexMenuCommand(IFileTabManager fileTabManager)
				: base(fileTabManager) {
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(fileTabManager, context);
			public override string GetHeader(HexContext context) => GetHeaderInternal(fileTabManager, context);
			public override string GetInputGestureText(HexContext context) => GetInputGestureTextInternal(context);
		}

		static void Execute(IFileTabManager fileTabManager) =>
			ExecuteInternal(fileTabManager, HexMenuCommand.CreateContext(fileTabManager));
		static bool CanExecute(IFileTabManager fileTabManager) => IsVisibleInternal(fileTabManager, HexMenuCommand.CreateContext(fileTabManager));

		static string GetHeaderInternal(IFileTabManager fileTabManager, HexContext context) {
			var tokRef = GetTokenReference(fileTabManager, context);
			return string.Format(dnSpy_AsmEditor_Resources.GoToMetaDataTableRowCommand, tokRef.Token);
		}

		static string GetInputGestureTextInternal(HexContext context) {
			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID) || context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return dnSpy_AsmEditor_Resources.ShortCutKeyShiftAltR;
			return null;
		}

		internal static void ExecuteInternal(IFileTabManager fileTabManager, HexContext context) {
			var @ref = GetTokenReference(fileTabManager, context);
			if (@ref != null)
				fileTabManager.FollowReference(@ref);
		}

		internal static bool IsVisibleInternal(IFileTabManager fileTabManager, HexContext context) => GetTokenReference(fileTabManager, context) != null;

		static TokenReference GetTokenReference(IFileTabManager fileTabManager, HexContext context) {
			var @ref = GetTokenReference2(context);
			if (@ref == null)
				return null;
			var node = fileTabManager.FileTreeView.FindNode(@ref.ModuleDef);
			return HasPENode(node) ? @ref : null;
		}

		internal static bool HasPENode(IModuleFileNode node) {
			if (node == null)
				return false;
			return PETreeNodeDataCreatorBase.HasPENode(node);
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
				var node = context.Nodes[0] as IMDTokenNode;
				if (node != null && node.Reference != null) {
					var mod = (node as ITreeNodeData).GetModule();
					if (mod != null)
						return new TokenReference(mod, node.Reference.MDToken.Raw);
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
		static readonly RoutedCommand GoToMDTableRowUI = new RoutedCommand("GoToMDTableRowUI", typeof(GoToMDTableRowUIHexEditorCommand));
		internal static void Initialize(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(GoToMDTableRowUI,
				(s, e) => Execute(fileTabManager),
				(s, e) => e.CanExecute = CanExecute(fileTabManager),
				ModifierKeys.Control | ModifierKeys.Shift, Key.D);
		}

		[ExportMenuItem(Header = "res:GoToMetaDataTableRowCommand2", InputGestureText = "res:ShortCutKeyCtrlShiftD", Group = MenuConstants.GROUP_CTX_CODE_TOKENS, Order = 30)]
		sealed class TheHexTextEditorCommand : HexTextEditorCommand {
			readonly IFileTabManager fileTabManager;

			[ImportingConstructor]
			TheHexTextEditorCommand(IFileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:GoToMetaDataTableRowCommand2", InputGestureText = "res:ShortCutKeyCtrlShiftD", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_GOTO_MD, Order = 0)]
		sealed class TheHexMenuCommand : HexMenuCommand {
			[ImportingConstructor]
			TheHexMenuCommand(IFileTabManager fileTabManager)
				: base(fileTabManager) {
			}

			public override void Execute(HexContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsVisible(HexContext context) => IsVisibleInternal(context);
		}

		static void Execute(IFileTabManager fileTabManager) =>
			Execute2(fileTabManager, HexMenuCommand.CreateContext(fileTabManager));
		static bool CanExecute(IFileTabManager fileTabManager) => CanExecute(HexMenuCommand.CreateContext(fileTabManager));
		static void ExecuteInternal(IFileTabManager fileTabManager, HexContext context) =>
			Execute2(fileTabManager, context);
		static bool IsVisibleInternal(HexContext context) => CanExecute(context);

		static bool CanExecute(HexContext context) {
			IFileTab tab;
			return GetModule(context, out tab) != null;
		}

		static ModuleDef GetModule(HexContext context, out IFileTab tab) {
			tab = null;
			if (context == null)
				return null;

			var uiContext = context.CreatorObject.Object as IDocumentViewer;
			if (uiContext != null) {
				tab = uiContext.FileTab;
				var content = uiContext.FileTab.Content;
				var node = content.Nodes.FirstOrDefault();
				if (node != null)
					return GetModule(GetModuleNode(node));
			}

			if (context.Nodes != null && context.Nodes.Length == 1)
				return GetModule(GetModuleNode(context.Nodes[0]));

			return null;
		}

		static IModuleFileNode GetModuleNode(ITreeNodeData node) {
			var modNode = node.GetModuleNode();
			if (modNode != null)
				return modNode;
			var asmNode = node as IAssemblyFileNode;
			if (asmNode != null) {
				asmNode.TreeNode.EnsureChildrenLoaded();
				return (IModuleFileNode)asmNode.TreeNode.DataChildren.FirstOrDefault(a => a is IModuleFileNode);
			}
			return null;
		}

		static ModuleDef GetModule(IModuleFileNode node) => GoToMDTableRowHexEditorCommand.HasPENode(node) ? node.DnSpyFile.ModuleDef : null;

		static void Execute2(IFileTabManager fileTabManager, HexContext context) {
			IFileTab tab;
			var module = GetModule(context, out tab);
			if (module == null)
				return;

			uint? token = AskForDef(dnSpy_AsmEditor_Resources.GoToMetaDataTableRowTitle, module);
			if (token == null)
				return;

			var tokRef = new TokenReference(module, token.Value);
			if (HexFileTreeNodeDataFinder.FindNode(fileTabManager.FileTreeView, tokRef) == null) {
				MsgBox.Instance.Show(string.Format(dnSpy_AsmEditor_Resources.GoToMetaDataTableRow_TokenDoesNotExist, token.Value));
				return;
			}

			if (tab != null)
				tab.FollowReference(tokRef, false);
			else
				fileTabManager.FollowReference(tokRef);
		}

		static uint? AskForDef(string title, ITokenResolver resolver) {
			return MsgBox.Instance.Ask(dnSpy_AsmEditor_Resources.GoToMetaDataTableRow_MetadataToken, null, title, s => {
				string error;
				uint token = SimpleTypeConverter.ParseUInt32(s, uint.MinValue, uint.MaxValue, out error);
				return string.IsNullOrEmpty(error) ? token : (uint?)null;
			}, s => {
				string error;
				uint token = SimpleTypeConverter.ParseUInt32(s, uint.MinValue, uint.MaxValue, out error);
				if (!string.IsNullOrEmpty(error))
					return error;
				var memberRef = resolver.ResolveToken(token);
				if (memberRef == null)
					return string.Format(dnSpy_AsmEditor_Resources.GoToMetaDataTableRow_InvalidMetadataToken, token);
				return string.Empty;
			});
		}
	}
}
