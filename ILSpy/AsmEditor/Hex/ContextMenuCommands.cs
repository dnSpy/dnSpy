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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using dnlib.DotNet;
using dnSpy.Decompiler;
using dnSpy.HexEditor;
using dnSpy.MVVM;
using dnSpy.Tabs;
using dnSpy.TreeNodes;
using dnSpy.TreeNodes.Hex;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;
using WF = System.Windows.Forms;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IPlugin))]
	sealed class HexContextMenuPlugin : IPlugin {
		public void OnLoaded() {
			GoToOffsetHexBoxContextMenuEntry.OnLoaded();
			OpenHexEditorCommand.OnLoaded();
			PasteBlobDataHexBoxContextMenuEntry.OnLoaded();
			GoToMDTableRowHexEditorCommand.OnLoaded();
			GoToMDTableRowUIHexEditorCommand.OnLoaded();
			SelectRangeHexBoxContextMenuEntry.OnLoaded();
		}
	}

	abstract class HexCommand : ICommand, IContextMenuEntry2, IMainMenuCommand, IMainMenuCommandInitialize {
		protected static ContextMenuEntryContext CreateContext() {
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView != null && textView.IsKeyboardFocusWithin)
				return ContextMenuEntryContext.Create(textView);

			if (MainWindow.Instance.treeView.IsKeyboardFocusWithin)
				return ContextMenuEntryContext.Create(MainWindow.Instance.treeView);

			if (MainWindow.Instance.treeView.SelectedItems.Count != 0) {
				bool teFocus = textView != null && textView.TextEditor.TextArea.IsFocused;
				if (teFocus)
					return ContextMenuEntryContext.Create(textView);
				if (UIUtils.HasSelectedChildrenFocus(MainWindow.Instance.treeView))
					return ContextMenuEntryContext.Create(MainWindow.Instance.treeView);
			}

			return ContextMenuEntryContext.Create(null);
		}

		event EventHandler ICommand.CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		bool ICommand.CanExecute(object parameter) {
			var ctx = CreateContext();
			return IsVisible(ctx) && IsEnabled(ctx);
		}

		void ICommand.Execute(object parameter) {
			Execute(CreateContext());
		}

		public abstract void Execute(ContextMenuEntryContext context);

		public virtual void Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
		}

		public virtual bool IsEnabled(ContextMenuEntryContext context) {
			return true;
		}

		public abstract bool IsVisible(ContextMenuEntryContext context);

		bool IMainMenuCommand.IsVisible {
			get { return IsVisible(CreateContext()); }
		}

		void IMainMenuCommandInitialize.Initialize(MenuItem menuItem) {
			Initialize(CreateContext(), menuItem);
		}
	}

	[ExportContextMenuEntry(Header = "Open He_x Editor", Order = 500, Category = "Hex", Icon = "Binary", InputGestureText = "Ctrl+X")]
	[ExportMainMenuCommand(MenuHeader = "Open He_x Editor", Menu = "_Edit", MenuOrder = 3500, MenuCategory = "Hex", MenuIcon = "Binary", MenuInputGestureText = "Ctrl+X")]
	sealed class OpenHexEditorCommand : HexCommand {
		internal static void OnLoaded() {
			MainWindow.Instance.CodeBindings.Add(new RoutedCommand("OpenHexEditor", typeof(OpenHexEditorCommand)),
				(s, e) => ExecuteCommand(),
				(s, e) => e.CanExecute = CanExecuteCommand(),
				ModifierKeys.Control, Key.X);
		}

		public override void Execute(ContextMenuEntryContext context) {
			ExecuteInternal(context);
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			return IsVisibleInternal(context);
		}

		public override void Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			menuItem.Header = MainWindow.Instance.GetHexTabState(GetAssemblyTreeNode(context)) == null ? "Open Hex Editor" : "Show Hex Editor";
		}

		static void ExecuteCommand() {
			var context = CreateContext();
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
			var context = CreateContext();
			return ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context) ||
				ShowILRangeInHexEditorCommand.IsVisibleInternal(context) ||
				ShowHexNodeInHexEditorCommand.IsVisibleInternal(context) ||
				IsVisibleInternal(context);
		}

		internal static void ExecuteInternal(ContextMenuEntryContext context) {
			var node = GetNode(context);
			if (node != null)
				MainWindow.Instance.OpenOrShowHexBox(node.LoadedAssembly.FileName);
		}

		static bool IsVisibleInternal(ContextMenuEntryContext context) {
			var node = GetNode(context);
			return node != null && !string.IsNullOrEmpty(node.LoadedAssembly.FileName);
		}

		static AssemblyTreeNode GetAssemblyTreeNode(ContextMenuEntryContext context) {
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (ShowILRangeInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (ShowHexNodeInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (context.Element is DecompilerTextView)
				return GetActiveAssemblyTreeNode();
			if (context.Element == MainWindow.Instance.treeView) {
				return context.SelectedTreeNodes != null &&
					context.SelectedTreeNodes.Length == 1 ?
					context.SelectedTreeNodes[0] as AssemblyTreeNode : null;
			}
			return null;
		}

		static AssemblyTreeNode GetActiveAssemblyTreeNode() {
			var tabState = MainWindow.Instance.GetActiveDecompileTabState();
			if (tabState == null || tabState.DecompiledNodes.Length == 0)
				return null;
			return ILSpyTreeNode.GetNode<AssemblyTreeNode>(tabState.DecompiledNodes[0]);
		}

		static AssemblyTreeNode GetNode(ContextMenuEntryContext context) {
			return GetAssemblyTreeNode(context);
		}
	}

	[ExportContextMenuEntry(Header = "Show in He_x Editor", Order = 500.1, Category = "Hex", Icon = "Binary", InputGestureText = "Ctrl+X")]
	[ExportMainMenuCommand(MenuHeader = "Show in He_x Editor", Menu = "_Edit", MenuOrder = 3500.1, MenuCategory = "Hex", MenuIcon = "Binary", MenuInputGestureText = "Ctrl+X")]
	sealed class ShowAddressReferenceInHexEditorCommand : HexCommand {
		public override void Execute(ContextMenuEntryContext context) {
			ExecuteInternal(context);
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			return IsVisibleInternal(context);
		}

		internal static void ExecuteInternal(ContextMenuEntryContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		internal static bool IsVisibleInternal(ContextMenuEntryContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(ContextMenuEntryContext context) {
			if (context.Reference == null)
				return null;

			var addr = context.Reference.Reference as AddressReference;
			if (addr != null)
				return addr;

			var rsrc = context.Reference.Reference as IResourceNode;
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
			var mod = asmNode.LoadedAssembly.ModuleDefinition;
			if (mod != null)
				return mod.Location;
			var peImage = asmNode.LoadedAssembly.PEImage;
			return peImage == null ? null : peImage.FileName;
		}
	}

	[ExportContextMenuEntry(Header = "Show Instructions in He_x Editor", Order = 500.2, Category = "Hex", Icon = "Binary", InputGestureText = "Ctrl+X")]
	[ExportMainMenuCommand(MenuHeader = "Show Instructions in He_x Editor", Menu = "_Edit", MenuOrder = 3500.2, MenuCategory = "Hex", MenuIcon = "Binary", MenuInputGestureText = "Ctrl+X")]
	sealed class ShowILRangeInHexEditorCommand : HexCommand {
		public override void Execute(ContextMenuEntryContext context) {
			ExecuteInternal(context);
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			return IsVisibleInternal(context);
		}

		internal static void ExecuteInternal(ContextMenuEntryContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		internal static bool IsVisibleInternal(ContextMenuEntryContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(ContextMenuEntryContext context) {
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (TVShowMethodInstructionsInHexEditorCommand.IsVisibleInternal(context))
				return null;

			var mappings = GetMappings(context);
			if (mappings == null || mappings.Count == 0)
				return null;

			var method = mappings[0].MemberMapping.MethodDefinition;
			var mod = mappings[0].MemberMapping.MethodDefinition.Module as ModuleDefMD;
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

		static IList<SourceCodeMapping> GetMappings(ContextMenuEntryContext context) {
			return MethodBody.EditILInstructionsCommand.GetMappings(context);
		}
	}

	[ExportContextMenuEntry(Header = "Show in He_x Editor", Order = 500.3, Category = "Hex", Icon = "Binary", InputGestureText = "Ctrl+X")]
	[ExportMainMenuCommand(MenuHeader = "Show in He_x Editor", Menu = "_Edit", MenuOrder = 3500.3, MenuCategory = "Hex", MenuIcon = "Binary", MenuInputGestureText = "Ctrl+X")]
	sealed class ShowHexNodeInHexEditorCommand : HexCommand {
		public override void Execute(ContextMenuEntryContext context) {
			ExecuteInternal(context);
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			return IsVisibleInternal(context);
		}

		internal static void ExecuteInternal(ContextMenuEntryContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		internal static bool IsVisibleInternal(ContextMenuEntryContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(ContextMenuEntryContext context) {
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (ShowILRangeInHexEditorCommand.IsVisibleInternal(context))
				return null;

			if (context.SelectedTreeNodes == null || context.SelectedTreeNodes.Length != 1)
				return null;
			var hexNode = context.SelectedTreeNodes[0] as HexTreeNode;
			if (hexNode == null)
				return null;

			var name = ShowAddressReferenceInHexEditorCommand.GetFilename(hexNode);
			if (string.IsNullOrEmpty(name))
				return null;

			return new AddressReference(name, false, hexNode.StartOffset, hexNode.StartOffset == 0 && hexNode.EndOffset == ulong.MaxValue ? ulong.MaxValue : hexNode.EndOffset - hexNode.StartOffset + 1);
		}
	}

	[ExportContextMenuEntry(Header = "Show Data in He_x Editor", Order = 500.4, Category = "Hex", Icon = "Binary")]
	[ExportMainMenuCommand(MenuHeader = "Show Data in He_x Editor", Menu = "_Edit", MenuOrder = 3500.4, MenuCategory = "Hex", MenuIcon = "Binary")]
	sealed class ShowStorageStreamDataInHexEditorCommand : HexCommand {
		public override void Execute(ContextMenuEntryContext context) {
			ExecuteInternal(context);
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			return IsVisibleInternal(context);
		}

		internal static void ExecuteInternal(ContextMenuEntryContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		internal static bool IsVisibleInternal(ContextMenuEntryContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(ContextMenuEntryContext context) {
			if (ShowAddressReferenceInHexEditorCommand.IsVisibleInternal(context))
				return null;
			if (ShowILRangeInHexEditorCommand.IsVisibleInternal(context))
				return null;

			if (context.SelectedTreeNodes == null || context.SelectedTreeNodes.Length != 1)
				return null;
			if (!(context.SelectedTreeNodes[0] is HexTreeNode))
				return null;

			var mod = ILSpyTreeNode.GetModule(context.SelectedTreeNodes[0]) as ModuleDefMD;
			if (mod == null)
				return null;
			var pe = mod.MetaData.PEImage;

			var sectNode = context.SelectedTreeNodes[0] as ImageSectionHeaderTreeNode;
			if (sectNode != null) {
				if (sectNode.SectionNumber >= pe.ImageSectionHeaders.Count)
					return null;
				var sect = pe.ImageSectionHeaders[sectNode.SectionNumber];
				return new AddressReference(mod.Location, false, sect.PointerToRawData, sect.SizeOfRawData);
			}

			var stgNode = context.SelectedTreeNodes[0] as StorageStreamTreeNode;
			if (stgNode != null) {
				if (stgNode.StreamNumber >= mod.MetaData.MetaDataHeader.StreamHeaders.Count)
					return null;
				var sh = mod.MetaData.MetaDataHeader.StreamHeaders[stgNode.StreamNumber];

				return new AddressReference(mod.Location, false, (ulong)mod.MetaData.MetaDataHeader.StartOffset + sh.Offset, sh.StreamSize);
			}

			return null;
		}
	}

	[ExportContextMenuEntry(Header = "Show Instructions in He_x Editor", Order = 500.5, Category = "Hex", Icon = "Binary")]
	[ExportMainMenuCommand(MenuHeader = "Show Instructions in He_x Editor", Menu = "_Edit", MenuOrder = 3500.5, MenuCategory = "Hex", MenuIcon = "Binary")]
	sealed class TVShowMethodInstructionsInHexEditorCommand : HexCommand {
		public override void Execute(ContextMenuEntryContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			return IsVisibleInternal(context);
		}

		internal static bool IsVisibleInternal(ContextMenuEntryContext context) {
			return GetAddressReference(context) != null;
		}

		internal static IMemberDef GetMemberDef(ContextMenuEntryContext context) {
			if (context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length == 1 && context.SelectedTreeNodes[0] is IMemberTreeNode)
				return MainWindow.ResolveReference(((IMemberTreeNode)context.SelectedTreeNodes[0]).Member);

			// Only allow declarations of the defs, i.e., right-clicking a method call with a method
			// def as reference should return null, not the method def.
			if (context.Reference != null && context.Reference.IsLocalTarget && context.Reference.Reference is IMemberRef) {
				// Don't resolve it. It's confusing if we show the method body of a called method
				// instead of the current method.
				return context.Reference.Reference as IMemberDef;
			}

			return null;
		}

		static AddressReference GetAddressReference(ContextMenuEntryContext context) {
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

	[ExportContextMenuEntry(Header = "Show Method Body in Hex Editor", Order = 500.6, Category = "Hex", Icon = "Binary")]
	[ExportMainMenuCommand(MenuHeader = "Show Method Body in Hex Editor", Menu = "_Edit", MenuOrder = 3500.6, MenuCategory = "Hex", MenuIcon = "Binary")]
	sealed class TVShowMethodHeaderInHexEditorCommand : HexCommand {
		public override void Execute(ContextMenuEntryContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(ContextMenuEntryContext context) {
			var info = TVChangeBodyHexEditorCommand.GetMethodLengthAndOffset(context);
			if (info != null)
				return new AddressReference(info.Value.Filename, false, info.Value.Offset, info.Value.Size);

			return null;
		}
	}

	[ExportContextMenuEntry(Header = "Show Initial Value in Hex Editor", Order = 500.7, Category = "Hex", Icon = "Binary")]
	[ExportMainMenuCommand(MenuHeader = "Show Initial Value in Hex Editor", Menu = "_Edit", MenuOrder = 3500.7, MenuCategory = "Hex", MenuIcon = "Binary")]
	sealed class TVShowFieldInitialValueInHexEditorCommand : HexCommand {
		public override void Execute(ContextMenuEntryContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(ContextMenuEntryContext context) {
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

	[ExportContextMenuEntry(Header = "Show in Hex Editor", Order = 500.8, Category = "Hex", Icon = "Binary")]
	[ExportMainMenuCommand(MenuHeader = "Show in Hex Editor", Menu = "_Edit", MenuOrder = 3500.8, MenuCategory = "Hex", MenuIcon = "Binary")]
	sealed class TVShowResourceInHexEditorCommand : HexCommand {
		public override void Execute(ContextMenuEntryContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(ContextMenuEntryContext context) {
			if (context.SelectedTreeNodes == null || context.SelectedTreeNodes.Length != 1)
				return null;

			var rsrc = context.SelectedTreeNodes[0] as IResourceNode;
			if (rsrc != null && rsrc.FileOffset != 0) {
				var mod = ILSpyTreeNode.GetModule((ILSpyTreeNode)rsrc);
				if (mod != null && !string.IsNullOrEmpty(mod.Location))
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

	abstract class TVChangeBodyHexEditorCommand : HexCommand {
		protected abstract string GetDescription(byte[] data);

		public override void Execute(ContextMenuEntryContext context) {
			var data = GetData(context);
			if (data == null)
				return;
			var info = GetMethodLengthAndOffset(context);
			if (info == null || info.Value.Size < (ulong)data.Length)
				return;
			WriteHexUndoCommand.AddAndExecute(info.Value.Filename, info.Value.Offset, data, GetDescription(data));
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			var data = GetData(context);
			if (data == null)
				return false;
			var info = GetMethodLengthAndOffset(context);
			return info != null && info.Value.Size >= (ulong)data.Length;
		}

		byte[] GetData(ContextMenuEntryContext context) {
			var md = TVShowMethodInstructionsInHexEditorCommand.GetMemberDef(context) as MethodDef;
			if (md == null)
				return null;
			return GetData(md);
		}

		protected abstract byte[] GetData(MethodDef method);

		internal static LengthAndOffset? GetMethodLengthAndOffset(ContextMenuEntryContext context) {
			var md = TVShowMethodInstructionsInHexEditorCommand.GetMemberDef(context) as MethodDef;
			if (md == null)
				return null;
			var mod = md.Module;
			if (mod == null || string.IsNullOrEmpty(mod.Location))
				return null;
			uint rva;
			long fileOffset;
			if (!md.GetRVA(out rva, out fileOffset))
				return null;

			return new LengthAndOffset(mod.Location, (ulong)fileOffset, InstructionUtils.GetTotalMethodBodyLength(md));
		}
	}

	[ExportContextMenuEntry(Header = "Hex Write 'return true' Body", Order = 500.9, Category = "Hex")]
	[ExportMainMenuCommand(MenuHeader = "Hex Write 'return true' Body", Menu = "_Edit", MenuOrder = 3500.9, MenuCategory = "Hex")]
	sealed class TVChangeBodyToReturnTrueHexEditorCommand : TVChangeBodyHexEditorCommand {
		protected override string GetDescription(byte[] data) {
			return "Hex Write 'return true' Body";
		}

		protected override byte[] GetData(MethodDef method) {
			if (method.MethodSig.GetRetType().RemovePinnedAndModifiers().GetElementType() != ElementType.Boolean)
				return null;
			return data;
		}
		static readonly byte[] data = new byte[] { 0x0A, 0x17, 0x2A };
	}

	[ExportContextMenuEntry(Header = "Hex Write 'return false' Body", Order = 501.0, Category = "Hex")]
	[ExportMainMenuCommand(MenuHeader = "Hex Write 'return false' Body", Menu = "_Edit", MenuOrder = 3501.0, MenuCategory = "Hex")]
	sealed class TVChangeBodyToReturnFalseHexEditorCommand : TVChangeBodyHexEditorCommand {
		protected override string GetDescription(byte[] data) {
			return "Hex Write 'return false' Body";
		}

		protected override byte[] GetData(MethodDef method) {
			if (method.MethodSig.GetRetType().RemovePinnedAndModifiers().GetElementType() != ElementType.Boolean)
				return null;
			return data;
		}
		static readonly byte[] data = new byte[] { 0x0A, 0x16, 0x2A };
	}

	[ExportContextMenuEntry(Header = "Hex Write Empty Body", Order = 501.1, Category = "Hex")]
	[ExportMainMenuCommand(MenuHeader = "Hex Write Empty Body", Menu = "_Edit", MenuOrder = 3501.1, MenuCategory = "Hex")]
	sealed class TVWriteEmptyBodyHexEditorCommand : TVChangeBodyHexEditorCommand {
		protected override string GetDescription(byte[] data) {
			return "Hex Write Empty Body";
		}

		protected override byte[] GetData(MethodDef method) {
			var sig = method.MethodSig.GetRetType().RemovePinnedAndModifiers();

			// This is taken care of by the write 'return true/false' commands
			if (sig.GetElementType() == ElementType.Boolean)
				return null;

			return GetData(sig, 0);
		}

		byte[] GetData(TypeSig typeSig, int level) {
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

	[ExportContextMenuEntry(Header = "Hex Copy Method Body", Order = 501.2, Category = "Hex")]
	[ExportMainMenuCommand(MenuHeader = "Hex Copy Method Body", Menu = "_Edit", MenuOrder = 3501.2, MenuCategory = "Hex")]
	sealed class TVCopyMethodBodyHexEditorCommand : HexCommand {
		public override void Execute(ContextMenuEntryContext context) {
			var data = GetMethodBodyBytes(context);
			if (data == null)
				return;
			ClipboardUtils.SetText(ClipboardUtils.ToHexString(data));
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			return TVChangeBodyHexEditorCommand.GetMethodLengthAndOffset(context) != null;
		}

		static byte[] GetMethodBodyBytes(ContextMenuEntryContext context) {
			var info = TVChangeBodyHexEditorCommand.GetMethodLengthAndOffset(context);
			if (info == null || info.Value.Size > int.MaxValue)
				return null;
			var doc = HexDocumentManager.Instance.GetOrCreate(info.Value.Filename);
			if (doc == null)
				return null;
			return doc.ReadBytes(info.Value.Offset, (int)info.Value.Size);
		}
	}

	[ExportContextMenuEntry(Header = "Hex Paste Method Body", Order = 501.3, Category = "Hex")]
	[ExportMainMenuCommand(MenuHeader = "Hex Paste Method Body", Menu = "_Edit", MenuOrder = 3501.3, MenuCategory = "Hex")]
	sealed class TVPasteMethodBodyHexEditorCommand : TVChangeBodyHexEditorCommand {
		protected override string GetDescription(byte[] data) {
			return "Hex Paste Method Body";
		}

		protected override byte[] GetData(MethodDef method) {
			return ClipboardUtils.GetData();
		}
	}

	[ExportContextMenuEntry(Order = 510.0, Category = "Hex")]
	[ExportMainMenuCommand(Menu = "_Edit", MenuOrder = 3510.0, MenuCategory = "Hex")]
	sealed class GoToMDTableRowHexEditorCommand : HexCommand {
		internal static void OnLoaded() {
			MainWindow.Instance.CodeBindings.Add(new RoutedCommand("GoToMDTableRow", typeof(GoToMDTableRowHexEditorCommand)),
				(s, e) => Execute(),
				(s, e) => e.CanExecute = CanExecute(),
				ModifierKeys.Shift | ModifierKeys.Alt, Key.R);
		}

		static void Execute() {
			ExecuteInternal(CreateContext());
		}

		static bool CanExecute() {
			return IsVisibleInternal(CreateContext());
		}

		public override void Execute(ContextMenuEntryContext context) {
			ExecuteInternal(context);
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			return IsVisibleInternal(context);
		}

		public override void Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			var tokRef = GetTokenReference(context);
			menuItem.Header = string.Format("Go to MD Table Row ({0:X8})", tokRef.Token);
			if (context.Element == MainWindow.Instance.treeView || context.Element is DecompilerTextView)
				menuItem.InputGestureText = "Shift+Alt+R";
		}

		internal static void ExecuteInternal(ContextMenuEntryContext context) {
			var @ref = GetTokenReference(context);
			if (@ref != null)
				MainWindow.Instance.JumpToReference(@ref);
		}

		internal static bool IsVisibleInternal(ContextMenuEntryContext context) {
			return GetTokenReference(context) != null;
		}

		static TokenReference GetTokenReference(ContextMenuEntryContext context) {
			if (context == null)
				return null;
			if (context.Reference != null) {
				var tokRef = context.Reference.Reference as TokenReference;
				if (tokRef != null)
					return tokRef;

				var mr = context.Reference.Reference as IMemberRef;
				if (mr != null)
					return CreateTokenReference(mr.Module, mr);

				var p = context.Reference.Reference as Parameter;
				if (p != null) {
					var pd = p.ParamDef;
					if (pd != null && pd.DeclaringMethod != null)
						return CreateTokenReference(pd.DeclaringMethod.Module, pd);
				}
			}
			if (context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length == 1) {
				var node = context.SelectedTreeNodes[0] as ITokenTreeNode;
				if (node != null && node.MDTokenProvider != null) {
					var mod = ILSpyTreeNode.GetModule((SharpTreeNode)node);
					if (mod != null)
						return new TokenReference(mod.Location, node.MDTokenProvider.MDToken.Raw);
				}
			}

			return null;
		}

		static TokenReference CreateTokenReference(ModuleDef module, IMDTokenProvider @ref) {
			if (module == null || @ref == null)
				return null;
			var mod = module as ModuleDefMD;
			if (mod == null)
				return null;
			// Make sure it's not a created method/field/etc
			var res = mod.ResolveToken(@ref.MDToken.Raw);
			if (res == null)
				return null;
			return new TokenReference(module.Location, @ref.MDToken.Raw);
		}
	}

	[ExportContextMenuEntry(Header = "Go to MD Table Row…", Order = 510.1, Category = "Hex", InputGestureText = "Ctrl+Shift+D")]
	[ExportMainMenuCommand(MenuHeader = "Go to MD Table Row…", Menu = "_Edit", MenuOrder = 3510.1, MenuCategory = "Hex", MenuInputGestureText = "Ctrl+Shift+D")]
	sealed class GoToMDTableRowUIHexEditorCommand : HexCommand {
		internal static void OnLoaded() {
			MainWindow.Instance.CodeBindings.Add(new RoutedCommand("GoToMDTableRowUI", typeof(GoToMDTableRowUIHexEditorCommand)),
				(s, e) => Execute(),
				(s, e) => e.CanExecute = CanExecute(),
				ModifierKeys.Control | ModifierKeys.Shift, Key.D);
		}

		static void Execute() {
			Execute2(CreateContext());
		}

		static bool CanExecute() {
			return CanExecute(CreateContext());
		}

		public override void Execute(ContextMenuEntryContext context) {
			Execute2(context);
		}

		public override bool IsEnabled(ContextMenuEntryContext context) {
			return true;
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			return CanExecute(context);
		}

		static bool CanExecute(ContextMenuEntryContext context) {
			DecompileTabState tabState;
			return GetModule(context, out tabState) != null;
		}

		static ModuleDefMD GetModule(ContextMenuEntryContext context, out DecompileTabState tabState) {
			tabState = null;
			if (context == null)
				return null;

			var textView = context.Element as DecompilerTextView;
			if (textView != null) {
				tabState = DecompileTabState.GetDecompileTabState(textView);
				if (tabState != null)
					return ILSpyTreeNode.GetModule(tabState.DecompiledNodes) as ModuleDefMD;
			}

			if (context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length == 1)
				return ILSpyTreeNode.GetModule(context.SelectedTreeNodes[0]) as ModuleDefMD;

			return null;
		}

		static void Execute2(ContextMenuEntryContext context) {
			DecompileTabState tabState;
			var module = GetModule(context, out tabState);
			if (module == null)
				return;

			uint? token = GoToTokenContextMenuEntry.AskForToken("Go to MD Table Row");
			if (token == null)
				return;

			var tokRef = new TokenReference(module.Location, token.Value);
			if (MainWindow.Instance.AssemblyListTreeNode.FindTokenNode(tokRef) == null) {
				MainWindow.Instance.ShowMessageBox(string.Format("Token {0:X8} doesn't exist in the metadata", token.Value));
				return;
			}

			if (tabState != null)
				MainWindow.Instance.JumpToReference(tabState.TextView, tokRef);
			else
				MainWindow.Instance.JumpToReference(tokRef);
		}
	}

	abstract class HexBoxContextMenuEntry : IContextMenuEntry2 {
		public void Execute(ContextMenuEntryContext context) {
			var tabState = GetHexTabState(context.Element as HexBox);
			if (tabState != null)
				Execute(tabState);
		}

		public void Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			var tabState = GetHexTabState(context.Element as HexBox);
			if (tabState != null)
				Initialize(tabState, menuItem);
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			var tabState = GetHexTabState(context.Element as HexBox);
			if (tabState != null)
				return IsEnabled(tabState);
			return false;
		}

		public bool IsVisible(ContextMenuEntryContext context) {
			var tabState = GetHexTabState(context.Element as HexBox);
			if (tabState != null)
				return IsVisible(tabState);
			return false;
		}

		static HexTabState GetHexTabState(HexBox hexBox) {
			return (HexTabState)TabState.GetTabState(hexBox);
		}

		protected abstract void Execute(HexTabState tabState);
		protected virtual void Initialize(HexTabState tabState, MenuItem menuItem) {
		}
		protected virtual bool IsEnabled(HexTabState tabState) {
			return IsVisible(tabState);
		}
		protected abstract bool IsVisible(HexTabState tabState);
	}

	[ExportContextMenuEntry(Header = "Go to Offset…", Order = 100, Category = "Misc", InputGestureText = "Ctrl+G")]
	sealed class GoToOffsetHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		internal static void OnLoaded() {
			MainWindow.Instance.HexBindings.Add(new RoutedCommand("GoToOffset", typeof(GoToOffsetHexBoxContextMenuEntry)),
				(s, e) => Execute(),
				(s, e) => e.CanExecute = CanExecute(),
				ModifierKeys.Control, Key.G);
		}

		static HexTabState GetHexTabState(ContextMenuEntryContext context) {
			return TabState.GetTabState(context.Element as HexBox) as HexTabState;
		}

		static void Execute() {
			Execute2(MainWindow.Instance.ActiveTabState as HexTabState);
		}

		static bool CanExecute() {
			return CanExecute(MainWindow.Instance.ActiveTabState as HexTabState);
		}

		protected override void Execute(HexTabState tabState) {
			Execute2(tabState);
		}

		protected override bool IsVisible(HexTabState tabState) {
			return CanExecute(tabState);
		}

		static bool CanExecute(HexTabState tabState) {
			return tabState != null;
		}

		static void Execute2(HexTabState tabState) {
			if (!CanExecute(tabState))
				return;

			var hb = tabState.HexBox;
			var data = new GoToOffsetVM(hb.PhysicalToVisibleOffset(hb.CaretPosition.Offset), hb.PhysicalToVisibleOffset(hb.StartOffset), hb.PhysicalToVisibleOffset(hb.EndOffset));
			var win = new GoToOffsetDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			hb.CaretPosition = new HexBoxPosition(hb.VisibleToPhysicalOffset(data.OffsetVM.Value), hb.CaretPosition.Kind, 0);
		}
	}

	[ExportContextMenuEntry(Header = "Select…", Order = 110, Category = "Misc", InputGestureText = "Ctrl+L")]
	sealed class SelectRangeHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		internal static void OnLoaded() {
			MainWindow.Instance.HexBindings.Add(new RoutedCommand("Select", typeof(SelectRangeHexBoxContextMenuEntry)),
				(s, e) => Execute(),
				(s, e) => e.CanExecute = CanExecute(),
				ModifierKeys.Control, Key.L);
		}

		static void Execute() {
			ExecuteInternal(GetHexTabState());
		}

		static bool CanExecute() {
			return GetHexTabState() != null;
		}

		static HexTabState GetHexTabState() {
			return MainWindow.Instance.ActiveTabState as HexTabState;
		}

		protected override void Execute(HexTabState tabState) {
			ExecuteInternal(tabState);
		}

		static void ExecuteInternal(HexTabState tabState) {
			if (tabState == null)
				return;
			var hb = tabState.HexBox;
			ulong start = hb.CaretPosition.Offset;
			ulong end = start;
			if (hb.Selection != null) {
				start = hb.Selection.Value.StartOffset;
				end = hb.Selection.Value.EndOffset;
			}
			var data = new SelectVM(hb.PhysicalToVisibleOffset(start), hb.PhysicalToVisibleOffset(end), hb.PhysicalToVisibleOffset(hb.StartOffset), hb.PhysicalToVisibleOffset(hb.EndOffset));
			var win = new SelectDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			hb.Selection = new HexSelection(hb.VisibleToPhysicalOffset(data.EndVM.Value), hb.VisibleToPhysicalOffset(data.StartVM.Value));
			hb.CaretPosition = new HexBoxPosition(hb.VisibleToPhysicalOffset(data.StartVM.Value), hb.CaretPosition.Kind, 0);
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}
	}

	[ExportContextMenuEntry(Header = "Save Se_lection…", Order = 120, Category = "Misc")]
	sealed class SaveSelectionHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			var doc = tabState.HexBox.Document;
			if (doc == null)
				return;
			var sel = tabState.HexBox.Selection;
			if (sel == null)
				return;

			var dialog = new WF.SaveFileDialog() {
				Filter = PickFilenameConstants.AnyFilenameFilter,
				RestoreDirectory = true,
				ValidateNames = true,
			};

			if (dialog.ShowDialog() != WF.DialogResult.OK)
				return;

			var filename = dialog.FileName;
			try {
				using (var file = File.Create(filename))
					Write(doc, file, sel.Value.StartOffset, sel.Value.EndOffset);
			}
			catch (Exception ex) {
				MainWindow.Instance.ShowMessageBox(string.Format("Could not save '{0}'\nERROR: {1}", filename, ex.Message));
			}
		}

		protected override bool IsVisible(HexTabState tabState) {
			return tabState.HexBox.Document != null && tabState.HexBox.Selection != null;
		}

		static void Write(HexDocument doc, Stream target, ulong start, ulong end) {
			const int MAX_BUFFER_LENGTH = 1024 * 64;
			byte[] buffer = new byte[end - start >= MAX_BUFFER_LENGTH ? MAX_BUFFER_LENGTH : (int)(end - start + 1)];
			ulong offs = start;
			while (offs <= end) {
				ulong bytesLeft = offs == 0 && end == ulong.MaxValue ? ulong.MaxValue : end - offs + 1;
				int bytesToRead = bytesLeft >= (ulong)buffer.Length ? buffer.Length : (int)bytesLeft;

				doc.Read(offs, buffer, 0, bytesToRead);
				target.Write(buffer, 0, bytesToRead);

				ulong nextOffs = offs + (ulong)bytesToRead;
				if (nextOffs < offs)
					break;
				offs = nextOffs;
			}
		}
	}

	[ExportContextMenuEntry(Header = "Show Only Selected Bytes", Order = 130, Category = "Misc")]
	sealed class ShowSelectionHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			var sel = tabState.HexBox.Selection;
			if (sel == null)
				return;

			tabState.HexBox.StartOffset = sel.Value.StartOffset;
			tabState.HexBox.EndOffset = sel.Value.EndOffset;
		}

		protected override bool IsVisible(HexTabState tabState) {
			return tabState.HexBox.Selection != null &&
				(tabState.HexBox.StartOffset != tabState.HexBox.Selection.Value.StartOffset ||
				tabState.HexBox.EndOffset != tabState.HexBox.Selection.Value.EndOffset);
		}
	}

	[ExportContextMenuEntry(Header = "Show All Bytes", Order = 140, Category = "Misc")]
	sealed class ShowHoleDocumentHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.StartOffset = tabState.DocumentStartOffset;
			tabState.HexBox.EndOffset = tabState.DocumentEndOffset;
			var sel = tabState.HexBox.Selection;
			tabState.HexBox.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate {
				if (sel != null && sel == tabState.HexBox.Selection)
					tabState.SetCaretPositionAndMakeVisible(sel.Value.StartOffset, sel.Value.EndOffset);
				else
					tabState.HexBox.BringCaretIntoView();
			}));
		}

		protected override bool IsVisible(HexTabState tabState) {
			return tabState.HexBox.StartOffset != tabState.DocumentStartOffset ||
				tabState.HexBox.EndOffset != tabState.DocumentEndOffset;
		}
	}

	[ExportContextMenuEntry(Order = 200, Category = "Edit", InputGestureText = "Del")]
	sealed class ClearSelectionHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.ClearBytes();
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override void Initialize(HexTabState tabState, MenuItem menuItem) {
			menuItem.Header = tabState.HexBox.Selection != null ? "Clear Selected Bytes" : "Clear Byte";
		}
	}

	[ExportContextMenuEntry(Header = "Fill Selection with Byte…", Order = 210, Category = "Edit", Icon = "Fill")]
	sealed class WriteToSelectionSelectionHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			var sel = tabState.HexBox.Selection;
			if (sel == null)
				return;

			var ask = new AskForInput();
			ask.Owner = MainWindow.Instance;
			ask.Title = "Enter Value";
			ask.label.Content = "_Byte";
			ask.textBox.Text = "0xFF";
			ask.ShowDialog();
			if (ask.DialogResult != true)
				return;

			string error;
			byte b = NumberVMUtils.ParseByte(ask.textBox.Text, byte.MinValue, byte.MaxValue, out error);
			if (!string.IsNullOrEmpty(error)) {
				MainWindow.Instance.ShowMessageBox(error);
				return;
			}

			tabState.HexBox.FillBytes(sel.Value.StartOffset, sel.Value.EndOffset, b);
			tabState.HexBox.Selection = null;
		}

		protected override bool IsEnabled(HexTabState tabState) {
			return tabState.HexBox.Selection != null;
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}
	}

	[ExportContextMenuEntry(Header = "Use 0x Prefix (offset)", Order = 500, Category = "Options")]
	sealed class UseHexPrefixHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.UseHexPrefix = !(tabState.UseHexPrefix ?? HexSettings.Instance.UseHexPrefix);
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override void Initialize(HexTabState tabState, MenuItem menuItem) {
			menuItem.IsChecked = tabState.UseHexPrefix ?? HexSettings.Instance.UseHexPrefix;
		}
	}

	[ExportContextMenuEntry(Header = "Show ASCII", Order = 510, Category = "Options")]
	sealed class ShowAsciiHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.ShowAscii = !(tabState.ShowAscii ?? HexSettings.Instance.ShowAscii);
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override void Initialize(HexTabState tabState, MenuItem menuItem) {
			menuItem.IsChecked = tabState.ShowAscii ?? HexSettings.Instance.ShowAscii;
		}
	}

	[ExportContextMenuEntry(Header = "Lower Case Hex", Order = 520, Category = "Options")]
	sealed class LowerCaseHexHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.LowerCaseHex = !(tabState.LowerCaseHex ?? HexSettings.Instance.LowerCaseHex);
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override void Initialize(HexTabState tabState, MenuItem menuItem) {
			menuItem.IsChecked = tabState.LowerCaseHex ?? HexSettings.Instance.LowerCaseHex;
		}
	}

	[ExportContextMenuEntry(Header = "Bytes per Line", Order = 530, Category = "Options")]
	sealed class BytesPerLineHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		static readonly Tuple<int?, string>[] subMenus = new Tuple<int?, string>[] {
			Tuple.Create((int?)0, "_Fit to Width"),
			Tuple.Create((int?)8, "_8 Bytes"),
			Tuple.Create((int?)16, "_16 Bytes"),
			Tuple.Create((int?)32, "_32 Bytes"),
			Tuple.Create((int?)48, "_48 Bytes"),
			Tuple.Create((int?)64, "_64 Bytes"),
			Tuple.Create((int?)null, "_Default"),
		};

		protected override void Initialize(HexTabState tabState, MenuItem menuItem) {
			foreach (var info in subMenus) {
				var mi = new MenuItem {
					Header = info.Item2,
					IsChecked = info.Item1 == tabState.BytesPerLine,
				};
				var tmpInfo = info;
				mi.Click += (s, e) => tabState.BytesPerLine = tmpInfo.Item1;
				menuItem.Items.Add(mi);
			}
		}
	}

	[ExportContextMenuEntry(Header = "Encoding", Order = 540, Category = "Options")]
	sealed class EncodingHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		static readonly Tuple<AsciiEncoding?, string>[] subMenus = new Tuple<AsciiEncoding?, string>[] {
			Tuple.Create((AsciiEncoding?)AsciiEncoding.ASCII, "A_SCII"),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.ANSI, "_ANSI"),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.UTF7, "UTF_7"),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.UTF8, "UTF_8"),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.UTF32, "UTF_32"),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.Unicode, "_Unicode"),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.BigEndianUnicode, "_BE Unicode"),
			Tuple.Create((AsciiEncoding?)null, "_Default"),
		};

		protected override void Initialize(HexTabState tabState, MenuItem menuItem) {
			foreach (var info in subMenus) {
				var mi = new MenuItem {
					Header = info.Item2,
					IsChecked = info.Item1 == tabState.AsciiEncoding,
				};
				var tmpInfo = info;
				mi.Click += (s, e) => tabState.AsciiEncoding = tmpInfo.Item1;
				menuItem.Items.Add(mi);
			}
		}
	}

	[ExportContextMenuEntry(Header = "Settings…", Order = 599, Category = "Options")]
	sealed class LocalSettingsHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			var data = new LocalSettingsVM(new LocalHexSettings(tabState));
			var win = new LocalSettingsDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			data.CreateLocalHexSettings().CopyTo(tabState);
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}
	}

	abstract class CopyBaseHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override bool IsEnabled(HexTabState tabState) {
			return tabState.HexBox.Selection != null;
		}
	}

	[ExportContextMenuEntry(Header = "Cop_y", Order = 600, Category = "HexCopy2", Icon = "Copy", InputGestureText = "Ctrl+C")]
	sealed class CopyHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.Copy();
		}
	}

	[ExportContextMenuEntry(Header = "Copy UTF-8 String", Order = 610, Category = "HexCopy2", InputGestureText = "Ctrl+Shift+8")]
	sealed class CopyUtf8StringHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyUTF8String();
		}
	}

	[ExportContextMenuEntry(Header = "Copy Unicode String", Order = 620, Category = "HexCopy2", InputGestureText = "Ctrl+Shift+U")]
	sealed class CopyUnicodeStringHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyUnicodeString();
		}
	}

	[ExportContextMenuEntry(Header = "Copy C# Array", Order = 630, Category = "HexCopy2", InputGestureText = "Ctrl+Shift+P")]
	sealed class CopyCSharpArrayHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyCSharpArray();
		}
	}

	[ExportContextMenuEntry(Header = "Copy VB Array", Order = 640, Category = "HexCopy2", InputGestureText = "Ctrl+Shift+B")]
	sealed class CopyVBArrayHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyVBArray();
		}
	}

	[ExportContextMenuEntry(Header = "Copy UI Contents", Order = 650, Category = "HexCopy2", InputGestureText = "Ctrl+Shift+C")]
	sealed class CopyUIContentsHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyUIContents();
		}
	}

	[ExportContextMenuEntry(Header = "Copy Offset", Order = 660, Category = "HexCopy2", InputGestureText = "Ctrl+Alt+O")]
	sealed class CopyOffsetHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyOffset();
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}
	}

	[ExportContextMenuEntry(Header = "_Paste", Order = 670, Category = "HexCopy2", Icon = "Paste", InputGestureText = "Ctrl+V")]
	sealed class PasteHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.Paste();
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override bool IsEnabled(HexTabState tabState) {
			return tabState.HexBox.CanPaste();
		}
	}

	[ExportContextMenuEntry(Header = "_Paste (UTF-8)", Order = 680, Category = "HexCopy2", InputGestureText = "Ctrl+8")]
	sealed class PasteUtf8HexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.PasteUtf8();
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override bool IsEnabled(HexTabState tabState) {
			return tabState.HexBox.CanPasteUtf8();
		}
	}

	[ExportContextMenuEntry(Header = "_Paste (Unicode)", Order = 690, Category = "HexCopy2", InputGestureText = "Ctrl+U")]
	sealed class PasteUnicodeHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.PasteUnicode();
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override bool IsEnabled(HexTabState tabState) {
			return tabState.HexBox.CanPasteUnicode();
		}
	}

	[ExportContextMenuEntry(Header = "_Paste (#Blob Data)", Order = 7000, Category = "HexCopy2", InputGestureText = "Ctrl+B")]
	sealed class PasteBlobDataHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		internal static void OnLoaded() {
			MainWindow.Instance.HexBindings.Add(new RoutedCommand("PasteBlobData", typeof(PasteBlobDataHexBoxContextMenuEntry)),
				(s, e) => Execute(),
				(s, e) => e.CanExecute = CanExecute(),
				ModifierKeys.Control, Key.B);
		}

		static void Execute() {
			Execute2(MainWindow.Instance.ActiveTabState as HexTabState);
		}

		static bool CanExecute() {
			return CanExecute(MainWindow.Instance.ActiveTabState as HexTabState);
		}

		static bool CanExecute(HexTabState tabState) {
			return tabState != null && GetBlobData(ClipboardUtils.GetData()) != null;
		}

		static void Execute2(HexTabState tabState) {
			if (!CanExecute(tabState))
				return;

			var data = GetBlobData(ClipboardUtils.GetData());
			if (data != null)
				tabState.HexBox.Paste(data);
		}

		protected override void Execute(HexTabState tabState) {
			Execute2(tabState);
		}

		static byte[] GetBlobData(byte[] data) {
			if (data == null)
				return null;
			uint len = (uint)data.Length;
			int extraLen = MDUtils.GetCompressedUInt32Length(len);
			if (extraLen < 0)
				return null;
			var d = new byte[data.Length + extraLen];
			MDUtils.WriteCompressedUInt32(d, 0, len);
			Array.Copy(data, 0, d, extraLen, data.Length);
			return d;
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override bool IsEnabled(HexTabState tabState) {
			return CanExecute(tabState);
		}
	}
}
