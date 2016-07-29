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
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.AsmEditor.Hex.Nodes;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.AsmEditor.Utilities;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.HexEditor;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IInitializeDataTemplate))]
	sealed class InitializeMDTableKeyboardShortcuts : IInitializeDataTemplate {
		readonly IFileTabManager fileTabManager;
		readonly Lazy<IUndoCommandManager> undoCommandManager;

		[ImportingConstructor]
		InitializeMDTableKeyboardShortcuts(IFileTabManager fileTabManager, Lazy<IUndoCommandManager> undoCommandManager) {
			this.fileTabManager = fileTabManager;
			this.undoCommandManager = undoCommandManager;
		}

		public void Initialize(DependencyObject d) {
			var lv = d as ListView;
			if (lv == null)
				return;
			if (!(lv.DataContext is MetaDataTableVM))
				return;

			lv.InputBindings.Add(new KeyBinding(new CtxMenuMDTableCommandProxy(fileTabManager, new SortMDTableCommand.TheMenuMDTableCommand(undoCommandManager)), Key.T, ModifierKeys.Shift | ModifierKeys.Control));
			lv.InputBindings.Add(new KeyBinding(new CtxMenuMDTableCommandProxy(fileTabManager, new CopyAsTextMDTableCommand.TheMenuMDTableCommand()), Key.C, ModifierKeys.Shift | ModifierKeys.Control));
			lv.InputBindings.Add(new KeyBinding(new CtxMenuMDTableCommandProxy(fileTabManager, new GoToRidMDTableCommand.TheMenuMDTableCommand()), Key.G, ModifierKeys.Control));
			lv.InputBindings.Add(new KeyBinding(new CtxMenuMDTableCommandProxy(fileTabManager, new ShowInHexEditorMDTableCommand.TheMenuMDTableCommand(fileTabManager)), Key.X, ModifierKeys.Control));
			Add(lv, ApplicationCommands.Copy, new CtxMenuMDTableCommandProxy(fileTabManager, new CopyMDTableCommand.TheMenuMDTableCommand()));
			Add(lv, ApplicationCommands.Paste, new CtxMenuMDTableCommandProxy(fileTabManager, new PasteMDTableCommand.TheMenuMDTableCommand(undoCommandManager)));
		}

		static void Add(UIElement elem, ICommand cmd, ICommand realCmd) {
			elem.CommandBindings.Add(new CommandBinding(cmd, (s, e) => realCmd.Execute(e.Parameter), (s, e) => e.CanExecute = realCmd.CanExecute(e.Parameter)));
		}
	}

	sealed class MDTableContext {
		public ListView ListView { get; }
		public MetaDataTableVM MetaDataTableVM { get; }
		public MetaDataTableNode Node { get; }
		public MetaDataTableRecordVM[] Records { get; }
		public bool IsContextMenu { get; }

		public MDTableContext(ListView listView, MetaDataTableVM mdVM, MetaDataTableNode mdNode, bool isContextMenu) {
			this.ListView = listView;
			this.MetaDataTableVM = mdVM;
			this.Node = mdNode;
			this.Records = listView.SelectedItems.Cast<MetaDataTableRecordVM>().OrderBy(a => a.StartOffset).ToArray();
			this.IsContextMenu = isContextMenu;
		}

		public bool ContiguousRecords() {
			if (Records.Length <= 1)
				return true;
			for (int i = 1; i < Records.Length; i++) {
				if (Records[i - 1].StartOffset + (ulong)MetaDataTableVM.TableInfo.RowSize != Records[i].StartOffset)
					return false;
			}
			return true;
		}
	}

	sealed class CtxMenuMDTableCommandProxy : ICommand {
		readonly IFileTabManager fileTabManager;
		readonly MenuItemBase<MDTableContext> cmd;

		public CtxMenuMDTableCommandProxy(IFileTabManager fileTabManager, MenuItemBase<MDTableContext> cmd) {
			this.fileTabManager = fileTabManager;
			this.cmd = cmd;
		}

		MDTableContext CreateMDTableContext() {
			var tab = fileTabManager.ActiveTab;
			if (tab != null) {
				var listView = FindListView(tab);
				if (listView != null && UIUtils.HasSelectedChildrenFocus(listView))
					return MenuMDTableCommand.ToMDTableContext(listView, false);
			}

			return null;
		}

		static ListView FindListView(IFileTab tab) {
			var o = tab.UIContext.UIObject as DependencyObject;
			while (o != null) {
				var lv = o as ListView;
				if (lv != null && InitDataTemplateAP.GetInitialize(lv))
					return lv;
				var children = UIUtils.GetChildren(o).ToArray();
				if (children.Length != 1)
					return null;
				o = children[0];
			}

			return null;
		}

		event EventHandler ICommand.CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		bool ICommand.CanExecute(object parameter) {
			var ctx = CreateMDTableContext();
			return ctx != null && cmd.IsVisible(ctx) && cmd.IsEnabled(ctx);
		}

		void ICommand.Execute(object parameter) {
			var ctx = CreateMDTableContext();
			if (ctx != null)
				cmd.Execute(ctx);
		}
	}

	abstract class CtxMenuMDTableCommand : MenuItemBase<MDTableContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override MDTableContext CreateContext(IMenuItemContext context) => MenuMDTableCommand.ToMDTableContext(context.CreatorObject.Object, true);
	}

	abstract class MenuMDTableCommand : MenuItemBase<MDTableContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override MDTableContext CreateContext(IMenuItemContext context) => ToMDTableContext(context.CreatorObject.Object, false);
		internal static MDTableContext ToMDTableContext(object obj, bool isContextMenu) => ToMDTableContext(obj as ListView, isContextMenu);

		internal static MDTableContext ToMDTableContext(ListView listView, bool isContextMenu) {
			if (listView == null)
				return null;
			var mdVM = listView.DataContext as MetaDataTableVM;
			if (mdVM == null)
				return null;

			return new MDTableContext(listView, mdVM, (MetaDataTableNode)mdVM.Owner, isContextMenu);
		}
	}

	static class SortMDTableCommand {
		[ExportMenuItem(Header = "res:SortMetadataTableCommand", InputGestureText = "res:ShortCutKeyCtrlShiftT", Group = MenuConstants.GROUP_CTX_CODE_HEX_MD, Order = 0)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			TheCtxMenuMDTableCommand(Lazy<IUndoCommandManager> undoCommandManager) {
				this.undoCommandManager = undoCommandManager;
			}

			public override void Execute(MDTableContext context) => ExecuteInternal(undoCommandManager, context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:SortMetadataTableCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_MD, InputGestureText = "res:ShortCutKeyCtrlShiftT", Order = 0)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			public TheMenuMDTableCommand(Lazy<IUndoCommandManager> undoCommandManager) {
				this.undoCommandManager = undoCommandManager;
			}

			public override void Execute(MDTableContext context) => ExecuteInternal(undoCommandManager, context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		static void ExecuteInternal(Lazy<IUndoCommandManager> undoCommandManager, MDTableContext context) =>
			SortTable(undoCommandManager, context.MetaDataTableVM, 1, context.MetaDataTableVM.Rows, string.Format(dnSpy_AsmEditor_Resources.SortMetadataTableCommand2, context.MetaDataTableVM.Table));

		internal static void SortTable(Lazy<IUndoCommandManager> undoCommandManager, MetaDataTableVM mdTblVM, uint rid, uint count, string descr) {
			var doc = mdTblVM.Document;
			int len = (int)count * mdTblVM.TableInfo.RowSize;
			var data = new byte[len];
			ulong startOffset = mdTblVM.StartOffset + (rid - 1) * (ulong)mdTblVM.TableInfo.RowSize;
			doc.Read(startOffset, data, 0, data.Length);
			TableSorter.Sort(mdTblVM.TableInfo, data);
			WriteHexUndoCommand.AddAndExecute(undoCommandManager.Value, doc, startOffset, data, descr);
		}

		static bool IsEnabledInternal(MDTableContext context) => TableSorter.CanSort(context.MetaDataTableVM.TableInfo);
	}

	static class SortSelectionMDTableCommand {
		[ExportMenuItem(Header = "res:SortSelectionCommand", Group = MenuConstants.GROUP_CTX_CODE_HEX_MD, Order = 10)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			TheCtxMenuMDTableCommand(Lazy<IUndoCommandManager> undoCommandManager) {
				this.undoCommandManager = undoCommandManager;
			}

			public override void Execute(MDTableContext context) => ExecuteInternal(undoCommandManager, context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:SortSelectionCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_MD, Order = 10)]
		sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			TheMenuMDTableCommand(Lazy<IUndoCommandManager> undoCommandManager) {
				this.undoCommandManager = undoCommandManager;
			}

			public override void Execute(MDTableContext context) => ExecuteInternal(undoCommandManager, context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		static void ExecuteInternal(Lazy<IUndoCommandManager> undoCommandManager, MDTableContext context) {
			uint rid = context.Records[0].Token.Rid;
			uint count = (uint)context.Records.Length;
			SortMDTableCommand.SortTable(undoCommandManager, context.MetaDataTableVM, rid, count, string.Format(dnSpy_AsmEditor_Resources.SortTable_RowIdentifier, context.MetaDataTableVM.Table, rid, rid + count - 1));
		}

		static bool IsEnabledInternal(MDTableContext context) =>
			TableSorter.CanSort(context.MetaDataTableVM.TableInfo) &&
			context.Records.Length > 1 &&
			context.ContiguousRecords();
	}

	static class GoToRidMDTableCommand {
		[ExportMenuItem(Header = "res:GoToRowIdentifierCommand", InputGestureText = "res:ShortCutKeyCtrlG", Group = MenuConstants.GROUP_CTX_CODE_HEX_MD, Order = 20)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:GoToRowIdentifierCommand", InputGestureText = "res:ShortCutKeyCtrlG", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_MD, Order = 20)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
		}

		static void ExecuteInternal(MDTableContext context) {
			var recVM = Ask(dnSpy_AsmEditor_Resources.GoToRowIdentifier_Title, context);
			if (recVM != null)
				UIUtils.ScrollSelectAndSetFocus(context.ListView, recVM);
		}

		static MetaDataTableRecordVM Ask(string title, MDTableContext context) {
			return MsgBox.Instance.Ask(dnSpy_AsmEditor_Resources.GoToMetaDataTableRow_RID, null, title, s => {
				string error;
				uint rid = SimpleTypeConverter.ParseUInt32(s, 1, context.MetaDataTableVM.Rows, out error);
				if (!string.IsNullOrEmpty(error))
					return null;
				return context.MetaDataTableVM.Get((int)(rid - 1));
			}, s => {
				string error;
				uint rid = SimpleTypeConverter.ParseUInt32(s, 1, context.MetaDataTableVM.Rows, out error);
				if (!string.IsNullOrEmpty(error))
					return error;
				if (rid == 0 || rid > context.MetaDataTableVM.Rows)
					return string.Format(dnSpy_AsmEditor_Resources.GoToRowIdentifier_InvalidRowIdentifier, rid);
				return string.Empty;
			});
		}
	}

	static class ShowInHexEditorMDTableCommand {
		[ExportMenuItem(Header = "res:ShowInHexEditorCommand", Icon = "Binary", InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_CTX_CODE_HEX_MD, Order = 30)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			readonly IFileTabManager fileTabManager;

			[ImportingConstructor]
			TheCtxMenuMDTableCommand(IFileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
			}

			public override void Execute(MDTableContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ShowInHexEditorCommand", Icon = "Binary", InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_MD, Order = 30)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			readonly IFileTabManager fileTabManager;

			[ImportingConstructor]
			public TheMenuMDTableCommand(IFileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
			}

			public override void Execute(MDTableContext context) => ExecuteInternal(fileTabManager, context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		static void ExecuteInternal(IFileTabManager fileTabManager, MDTableContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				fileTabManager.FollowReference(@ref);
		}

		static bool IsEnabledInternal(MDTableContext context) => GetAddressReference(context) != null;

		static AddressReference GetAddressReference(MDTableContext context) {
			if (context.Records.Length == 0)
				return null;
			if (!context.ContiguousRecords())
				return null;

			ulong start = context.Records[0].StartOffset;
			ulong end = context.Records[context.Records.Length - 1].EndOffset;
			return new AddressReference(context.MetaDataTableVM.Document.Name, false, start, end - start + 1);
		}
	}

	static class CopyAsTextMDTableCommand {
		[ExportMenuItem(Header = "res:CopyAsTextCommand2", InputGestureText = "res:ShortCutKeyCtrlShiftC", Group = MenuConstants.GROUP_CTX_CODE_HEX_COPY, Order = 0)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CopyAsTextCommand2", InputGestureText = "res:ShortCutKeyCtrlShiftC", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_COPY, Order = 0)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		static void ExecuteInternal(MDTableContext context) {
			var output = new StringBuilderTextColorOutput();
			var output2 = TextColorWriterToDecompilerOutput.Create(output);
			context.Node.WriteHeader(output2);
			foreach (var rec in context.Records)
				context.Node.Write(output2, rec);
			var s = output.ToString();
			if (s.Length > 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		static bool IsEnabledInternal(MDTableContext context) => context.Records.Length > 0;
	}

	static class CopyMDTableCommand {
		[ExportMenuItem(Header = "res:CopyCommand", Icon = "Copy", InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_CODE_HEX_COPY, Order = 10)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CopyCommand", Icon = "Copy", InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_COPY, Order = 10)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) => ExecuteInternal(context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
		}

		static void ExecuteInternal(MDTableContext context) {
			var doc = context.MetaDataTableVM.Document;
			ulong totalSize = (ulong)context.MetaDataTableVM.TableInfo.RowSize * (ulong)context.Records.Length * 2;
			if (totalSize > int.MaxValue) {
				MsgBox.Instance.Show(dnSpy_AsmEditor_Resources.TooManyBytesSelected);
				return;
			}
			var sb = new StringBuilder((int)totalSize);
			var recData = new byte[context.MetaDataTableVM.TableInfo.RowSize];
			foreach (var rec in context.Records) {
				doc.Read(rec.StartOffset, recData, 0, recData.Length);
				foreach (var b in recData)
					sb.Append(string.Format("{0:X2}", b));
			}
			var s = sb.ToString();
			if (s.Length > 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		static bool IsEnabledInternal(MDTableContext context) => context.Records.Length > 0;
	}

	static class PasteMDTableCommand {
		[ExportMenuItem(Header = "res:PasteCommand", Icon = "Paste", InputGestureText = "res:ShortCutKeyCtrlV", Group = MenuConstants.GROUP_CTX_CODE_HEX_COPY, Order = 20)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			TheCtxMenuMDTableCommand(Lazy<IUndoCommandManager> undoCommandManager) {
				this.undoCommandManager = undoCommandManager;
			}

			public override void Execute(MDTableContext context) => ExecuteInternal(undoCommandManager, context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
			public override string GetHeader(MDTableContext context) => GetHeaderInternal(context);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:PasteCommand", Icon = "Paste", InputGestureText = "res:ShortCutKeyCtrlV", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_COPY, Order = 20)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			public TheMenuMDTableCommand(Lazy<IUndoCommandManager> undoCommandManager) {
				this.undoCommandManager = undoCommandManager;
			}

			public override void Execute(MDTableContext context) => ExecuteInternal(undoCommandManager, context);
			public override bool IsEnabled(MDTableContext context) => IsEnabledInternal(context);
			public override string GetHeader(MDTableContext context) => GetHeaderInternal(context);
		}

		static void ExecuteInternal(Lazy<IUndoCommandManager> undoCommandManager, MDTableContext context) {
			var data = GetPasteData(context);
			if (data == null)
				return;

			var doc = context.MetaDataTableVM.Document;
			int recs = data.Length / context.MetaDataTableVM.TableInfo.RowSize;
			WriteHexUndoCommand.AddAndExecute(undoCommandManager.Value, doc, context.Records[0].StartOffset, data,
				string.Format(recs == 1 ? dnSpy_AsmEditor_Resources.Hex_Undo_Message_Paste_Record : dnSpy_AsmEditor_Resources.Hex_Undo_Message_Paste_Records,
						recs, context.MetaDataTableVM.Table, context.Records[0].StartOffset, context.Records[0].Token.Rid));
		}

		static bool IsEnabledInternal(MDTableContext context) => GetPasteData(context) != null;

		static byte[] GetPasteData(MDTableContext context) {
			if (context.Records.Length == 0)
				return null;

			var data = ClipboardUtils.GetData();
			if (data == null || data.Length == 0)
				return null;

			if (data.Length % context.MetaDataTableVM.TableInfo.RowSize != 0)
				return null;

			int recs = data.Length / context.MetaDataTableVM.TableInfo.RowSize;
			if ((uint)context.Records[0].Index + (uint)recs > context.MetaDataTableVM.Rows)
				return null;

			return data;
		}

		static string GetHeaderInternal(MDTableContext context) {
			var data = GetPasteData(context);
			if (data == null)
				return null;
			int recs = data.Length / context.MetaDataTableVM.TableInfo.RowSize;
			if (recs <= 1)
				return null;
			return string.Format(dnSpy_AsmEditor_Resources.PasteRecordsCommand, recs, context.Records[0].StartOffset, context.Records[0].Token.Rid);
		}
	}
}
