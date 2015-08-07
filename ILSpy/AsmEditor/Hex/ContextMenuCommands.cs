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
using dnSpy.HexEditor;
using dnSpy.Tabs;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using WF = System.Windows.Forms;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IPlugin))]
	sealed class HexContextMenuPlugin : IPlugin {
		public void OnLoaded() {
			GoToOffsetHexBoxContextMenuEntry.OnLoaded();
			OpenHexEditorContextMenuEntry.OnLoaded();
		}
	}

	[ExportContextMenuEntry(Header = "Open He_x Editor", Order = 500, Category = "Hex", Icon = "Binary", InputGestureText = "Ctrl+X")]
	sealed class OpenHexEditorContextMenuEntry : IContextMenuEntry2 {
		internal static void OnLoaded() {
			MainWindow.Instance.CodeBindings.Add(ApplicationCommands.Cut,
				(s, e) => ExecuteCommand(),
				(s, e) => e.CanExecute = CanExecuteCommand(),
				ModifierKeys.Control, Key.X);
		}

		static TextViewContext CreateTextViewContext() {
			var textView = MainWindow.Instance.ActiveTextView;
			return textView == null ? null : TextViewContext.Create(textView: textView, openedFromKeyboard: true);
		}

		static void ExecuteCommand() {
			var context = CreateTextViewContext();
			if (context == null)
				return;
			if (ShowAddressReferenceInHexEditorContextMenuEntry.IsVisibleInternal(context))
				ShowAddressReferenceInHexEditorContextMenuEntry.ExecuteInternal(context);
			else if (ShowILRangeInHexEditorContextMenuEntry.IsVisibleInternal(context))
				ShowILRangeInHexEditorContextMenuEntry.ExecuteInternal(context);
			else if (IsVisibleInternal(context))
				ExecuteInternal(context);
		}

		static bool CanExecuteCommand() {
			var context = CreateTextViewContext();
			if (context == null)
				return false;
			return ShowAddressReferenceInHexEditorContextMenuEntry.IsVisibleInternal(context) ||
				ShowILRangeInHexEditorContextMenuEntry.IsVisibleInternal(context) ||
				IsVisibleInternal(context);
		}

		public void Execute(TextViewContext context) {
			ExecuteInternal(context);
		}

		internal static void ExecuteInternal(TextViewContext context) {
			var node = GetNode(context);
			if (node != null)
				MainWindow.Instance.OpenOrShowHexBox(node.LoadedAssembly.FileName);
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public bool IsVisible(TextViewContext context) {
			return IsVisibleInternal(context);
		}

		static bool IsVisibleInternal(TextViewContext context) {
			var node = GetNode(context);
			return node != null && !string.IsNullOrEmpty(node.LoadedAssembly.FileName);
		}

		static AssemblyTreeNode GetAssemblyTreeNode(TextViewContext context) {
			if (ShowAddressReferenceInHexEditorContextMenuEntry.IsVisibleInternal(context))
				return null;
			if (ShowILRangeInHexEditorContextMenuEntry.IsVisibleInternal(context))
				return null;
			if (context.TextView != null)
				return GetActiveAssemblyTreeNode();
			if (context.TreeView == MainWindow.Instance.treeView) {
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

		static AssemblyTreeNode GetNode(TextViewContext context) {
			return GetAssemblyTreeNode(context);
		}

		public void Initialize(TextViewContext context, MenuItem menuItem) {
			menuItem.Header = MainWindow.Instance.GetHexTabState(GetAssemblyTreeNode(context)) == null ? "Open Hex Editor" : "Show Hex Editor";
		}
	}

	[ExportContextMenuEntry(Header = "Show in He_x Editor", Order = 500.1, Category = "Hex", Icon = "Binary", InputGestureText = "Ctrl+X")]
	sealed class ShowAddressReferenceInHexEditorContextMenuEntry : IContextMenuEntry {
		public void Execute(TextViewContext context) {
			ExecuteInternal(context);
		}

		internal static void ExecuteInternal(TextViewContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public bool IsVisible(TextViewContext context) {
			return IsVisibleInternal(context);
		}

		internal static bool IsVisibleInternal(TextViewContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(TextViewContext context) {
			if (context.Reference == null)
				return null;

			var addr = context.Reference.Reference as AddressReference;
			if (addr != null)
				return addr;

			var rsrc = context.Reference.Reference as IResourceNode;
			if (rsrc != null && rsrc.FileOffset != 0) {
				var mod = ILSpyTreeNode.GetModule((ILSpyTreeNode)rsrc);
				if (mod != null && !string.IsNullOrEmpty(mod.Location))
					return new AddressReference(mod.Location, false, rsrc.FileOffset, rsrc.Length);
			}

			return null;
		}
	}

	[ExportContextMenuEntry(Header = "Show Instructions in He_x Editor", Order = 500.2, Category = "Hex", Icon = "Binary", InputGestureText = "Ctrl+X")]
	sealed class ShowILRangeInHexEditorContextMenuEntry : IContextMenuEntry {
		public void Execute(TextViewContext context) {
			ExecuteInternal(context);
		}

		internal static void ExecuteInternal(TextViewContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public bool IsVisible(TextViewContext context) {
			return IsVisibleInternal(context);
		}

		internal static bool IsVisibleInternal(TextViewContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(TextViewContext context) {
			if (ShowAddressReferenceInHexEditorContextMenuEntry.IsVisibleInternal(context))
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

		static IList<SourceCodeMapping> GetMappings(TextViewContext context) {
			return MethodBody.EditILInstructionsCommand.GetMappings(context);
		}
	}

	[ExportContextMenuEntry(Header = "Show Instructions in He_x Editor", Order = 500.3, Category = "Hex", Icon = "Binary")]
	sealed class TVShowMethodInstructionsInHexEditorContextMenuEntry : IContextMenuEntry {
		public void Execute(TextViewContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public bool IsVisible(TextViewContext context) {
			return GetAddressReference(context) != null;
		}

		internal static IMemberDef GetMemberDef(TextViewContext context) {
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

		static AddressReference GetAddressReference(TextViewContext context) {
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

	[ExportContextMenuEntry(Header = "Show Method Body in Hex Editor", Order = 500.4, Category = "Hex", Icon = "Binary")]
	sealed class TVShowMethodHeaderInHexEditorContextMenuEntry : IContextMenuEntry {
		public void Execute(TextViewContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public bool IsVisible(TextViewContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(TextViewContext context) {
			var md = TVShowMethodInstructionsInHexEditorContextMenuEntry.GetMemberDef(context) as MethodDef;
			if (md == null)
				return null;

			var info = TVChangeBodyHexEditorContextMenuEntry.GetMethodLengthAndOffset(context);
			if (info != null)
				return new AddressReference(info.Value.Filename, false, info.Value.Offset, info.Value.Size);

			return null;
		}
	}

	[ExportContextMenuEntry(Header = "Show Initial Value in Hex Editor", Order = 500.5, Category = "Hex", Icon = "Binary")]
	sealed class TVShowFieldInitialValueInHexEditorContextMenuEntry : IContextMenuEntry {
		public void Execute(TextViewContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public bool IsVisible(TextViewContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(TextViewContext context) {
			var fd = TVShowMethodInstructionsInHexEditorContextMenuEntry.GetMemberDef(context) as FieldDef;
			if (fd == null || fd.RVA == 0)
				return null;
			var iv = fd.InitialValue;
			if (iv == null)
				return null;

			var mod = fd.Module;
			return new AddressReference(mod == null ? null : mod.Location, true, (ulong)fd.RVA, (ulong)iv.Length);
		}
	}

	[ExportContextMenuEntry(Header = "Show in Hex Editor", Order = 500.6, Category = "Hex", Icon = "Binary")]
	sealed class TVShowResourceInHexEditorContextMenuEntry : IContextMenuEntry {
		public void Execute(TextViewContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public bool IsVisible(TextViewContext context) {
			return GetAddressReference(context) != null;
		}

		static AddressReference GetAddressReference(TextViewContext context) {
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

	abstract class TVChangeBodyHexEditorContextMenuEntry : IContextMenuEntry {
		readonly byte[] data;
		readonly string descr;

		protected TVChangeBodyHexEditorContextMenuEntry(byte[] data, string descr) {
			this.data = data;
			this.descr = descr;
		}

		public void Execute(TextViewContext context) {
			var info = GetMethodLengthAndOffset(context);
			if (info == null || info.Value.Size < (ulong)data.Length)
				return;
			WriteHexUndoCommand.AddAndExecute(info.Value.Filename, info.Value.Offset, data, descr);
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public bool IsVisible(TextViewContext context) {
			var info = GetMethodLengthAndOffset(context);
			return info != null && info.Value.Size >= (ulong)data.Length;
		}

		internal static LengthAndOffset? GetMethodLengthAndOffset(TextViewContext context) {
			var md = TVShowMethodInstructionsInHexEditorContextMenuEntry.GetMemberDef(context) as MethodDef;
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

	[ExportContextMenuEntry(Header = "Hex Write Return True Body", Order = 500.7, Category = "Hex")]
	sealed class TVChangeBodyToReturnTrueHexEditorContextMenuEntry : TVChangeBodyHexEditorContextMenuEntry {
		public TVChangeBodyToReturnTrueHexEditorContextMenuEntry()
			:base(new byte[] { 0x06, 0x17 }, "Hex Write Return True Body") {
		}
	}

	[ExportContextMenuEntry(Header = "Hex Write Return False Body", Order = 500.8, Category = "Hex")]
	sealed class TVChangeBodyToReturnFalseHexEditorContextMenuEntry : TVChangeBodyHexEditorContextMenuEntry {
		public TVChangeBodyToReturnFalseHexEditorContextMenuEntry()
			: base(new byte[] { 0x06, 0x16 }, "Hex Write Return False Body") {
		}
	}

	abstract class HexBoxContextMenuEntry : IContextMenuEntry2 {
		public void Execute(TextViewContext context) {
			var tabState = GetHexTabState(context.HexBox);
			if (tabState != null)
				Execute(tabState);
		}

		public void Initialize(TextViewContext context, MenuItem menuItem) {
			var tabState = GetHexTabState(context.HexBox);
			if (tabState != null)
				Initialize(tabState, menuItem);
		}

		public bool IsEnabled(TextViewContext context) {
			var tabState = GetHexTabState(context.HexBox);
			if (tabState != null)
				return IsEnabled(tabState);
			return false;
		}

		public bool IsVisible(TextViewContext context) {
			var tabState = GetHexTabState(context.HexBox);
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

		static HexTabState GetHexTabState(TextViewContext context) {
			return TabState.GetTabState(context.HexBox) as HexTabState;
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

	[ExportContextMenuEntry(Header = "Select…", Order = 110, Category = "Misc")]
	sealed class SelectRangeHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
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
				Filter = "All files (*.*)|*.*",
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

	[ExportContextMenuEntry(Header = "Fill Selection with Byte...", Order = 210, Category = "Edit")]
	sealed class WriteToSelectionSelectionHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			//TODO:
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

	[ExportContextMenuEntry(Header = "Cop_y", Order = 600, Category = "Copy", Icon = "Copy", InputGestureText = "Ctrl+C")]
	sealed class CopyHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.Copy();
		}
	}

	[ExportContextMenuEntry(Header = "Copy UTF-8 String", Order = 610, Category = "Copy", InputGestureText = "Ctrl+Shift+8")]
	sealed class CopyUtf8StringHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyUTF8String();
		}
	}

	[ExportContextMenuEntry(Header = "Copy Unicode String", Order = 620, Category = "Copy", InputGestureText = "Ctrl+Shift+U")]
	sealed class CopyUnicodeStringHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyUnicodeString();
		}
	}

	[ExportContextMenuEntry(Header = "Copy C# Array", Order = 630, Category = "Copy", InputGestureText = "Ctrl+Shift+P")]
	sealed class CopyCSharpArrayHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyCSharpArray();
		}
	}

	[ExportContextMenuEntry(Header = "Copy VB Array", Order = 640, Category = "Copy", InputGestureText = "Ctrl+Shift+B")]
	sealed class CopyVBArrayHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyVBArray();
		}
	}

	[ExportContextMenuEntry(Header = "Copy UI Contents", Order = 650, Category = "Copy", InputGestureText = "Ctrl+Shift+C")]
	sealed class CopyUIContentsHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyUIContents();
		}
	}

	[ExportContextMenuEntry(Header = "Copy Offset", Order = 660, Category = "Copy", InputGestureText = "Ctrl+Alt+O")]
	sealed class CopyOffsetHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyOffset();
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}
	}

	[ExportContextMenuEntry(Header = "_Paste", Order = 670, Category = "Copy", Icon = "Paste", InputGestureText = "Ctrl+V")]
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

	[ExportContextMenuEntry(Header = "_Paste (UTF-8)", Order = 680, Category = "Copy", InputGestureText = "Ctrl+8")]
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

	[ExportContextMenuEntry(Header = "_Paste (Unicode)", Order = 690, Category = "Copy", InputGestureText = "Ctrl+U")]
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
}
