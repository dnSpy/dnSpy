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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Menus;
using dnSpy.Decompiler;
using dnSpy.HexEditor;
using dnSpy.MVVM;
using dnSpy.Shared.UI.Menus;
using dnSpy.Shared.UI.MVVM;
using dnSpy.Tabs;
using dnSpy.TreeNodes.Hex;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IInitializeDataTemplate))]
	sealed class InitializeMDTableKeyboardShortcuts : IInitializeDataTemplate {
		public void Initialize(DependencyObject d) {
			var lv = d as ListView;
			if (lv == null)
				return;
			if (!(lv.DataContext is MetaDataTableVM))
				return;

			lv.InputBindings.Add(new KeyBinding(new SortMDTableCommand.TheMenuMDTableCommand(), Key.T, ModifierKeys.Shift | ModifierKeys.Control));
			lv.InputBindings.Add(new KeyBinding(new CopyAsTextMDTableCommand.TheMenuMDTableCommand(), Key.C, ModifierKeys.Shift | ModifierKeys.Control));
			lv.InputBindings.Add(new KeyBinding(new GoToRidMDTableCommand.TheMenuMDTableCommand(), Key.G, ModifierKeys.Control));
			lv.InputBindings.Add(new KeyBinding(new ShowInHexEditorMDTableCommand.TheMenuMDTableCommand(), Key.X, ModifierKeys.Control));
			lv.AddCommandBinding(ApplicationCommands.Copy, new CopyMDTableCommand.TheMenuMDTableCommand());
			lv.AddCommandBinding(ApplicationCommands.Paste, new PasteMDTableCommand.TheMenuMDTableCommand());
		}
	}

	sealed class MDTableContext {
		public readonly ListView ListView;
		public readonly MetaDataTableVM MetaDataTableVM;
		public readonly MetaDataTableTreeNode TreeNode;
		public readonly MetaDataTableRecordVM[] Records;
		public readonly bool IsContextMenu;

		public MDTableContext(ListView listView, MetaDataTableVM mdVM, MetaDataTableTreeNode mdNode, bool isContextMenu) {
			this.ListView = listView;
			this.MetaDataTableVM = mdVM;
			this.TreeNode = mdNode;
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

	abstract class CtxMenuMDTableCommand : MenuItemBase<MDTableContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override MDTableContext CreateContext(IMenuItemContext context) {
			return MenuMDTableCommand.ToMDTableContext(context.CreatorObject.Object, true);
		}
	}

	abstract class MenuMDTableCommand : MenuItemBase<MDTableContext>, ICommand {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override MDTableContext CreateContext(IMenuItemContext context) {
			return ToMDTableContext(context.CreatorObject.Object, false);
		}

		internal static MDTableContext ToMDTableContext(object obj, bool isContextMenu) {
			return ToMDTableContext(obj as ListView, isContextMenu);
		}

		static MDTableContext ToMDTableContext(ListView listView, bool isContextMenu) {
			if (listView == null)
				return null;
			var mdVM = listView.DataContext as MetaDataTableVM;
			if (mdVM == null)
				return null;

			return new MDTableContext(listView, mdVM, (MetaDataTableTreeNode)mdVM.Owner, isContextMenu);
		}

		static MDTableContext CreateMDTableContext() {
			var tabState = MainWindow.Instance.GetActiveDecompileTabState();
			if (tabState != null) {
				var listView = FindListView(tabState);
				if (listView != null && UIUtils.HasSelectedChildrenFocus(listView))
					return ToMDTableContext(listView, false);
			}

			return null;
		}

		static ListView FindListView(DecompileTabState tabState) {
			var o = tabState.TabItem.Content as DependencyObject;
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
			return ctx != null && IsVisible(ctx) && IsEnabled(ctx);
		}

		void ICommand.Execute(object parameter) {
			var ctx = CreateMDTableContext();
			if (ctx != null)
				Execute(ctx);
		}
	}

	static class SortMDTableCommand {
		[ExportMenuItem(Header = "_Sort Table", InputGestureText = "Ctrl+Shift+T", Group = MenuConstants.GROUP_CTX_CODE_HEX_MD, Order = 0)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}

			public override bool IsEnabled(MDTableContext context) {
				return IsEnabledInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "_Sort Table", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_MD, InputGestureText = "Ctrl+Shift+T", Order = 0)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}

			public override bool IsEnabled(MDTableContext context) {
				return IsEnabledInternal(context);
			}
		}

		static void ExecuteInternal(MDTableContext context) {
			SortTable(context.MetaDataTableVM, 1, context.MetaDataTableVM.Rows, string.Format("Sort {0} table", context.MetaDataTableVM.Table));
		}

		internal static void SortTable(MetaDataTableVM mdTblVM, uint rid, uint count, string descr) {
			var doc = mdTblVM.Document;
			int len = (int)count * mdTblVM.TableInfo.RowSize;
			var data = new byte[len];
			ulong startOffset = mdTblVM.StartOffset + (rid - 1) * (ulong)mdTblVM.TableInfo.RowSize;
			doc.Read(startOffset, data, 0, data.Length);
			TableSorter.Sort(mdTblVM.TableInfo, data);
			WriteHexUndoCommand.AddAndExecute(doc, startOffset, data, descr);
		}

		static bool IsEnabledInternal(MDTableContext context) {
			return TableSorter.CanSort(context.MetaDataTableVM.TableInfo);
		}
	}

	static class SortSelectionMDTableCommand {
		[ExportMenuItem(Header = "So_rt Selection", Group = MenuConstants.GROUP_CTX_CODE_HEX_MD, Order = 10)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}

			public override bool IsEnabled(MDTableContext context) {
				return IsEnabledInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "So_rt Selection", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_MD, Order = 10)]
		sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}

			public override bool IsEnabled(MDTableContext context) {
				return IsEnabledInternal(context);
			}
		}

		static void ExecuteInternal(MDTableContext context) {
			uint rid = context.Records[0].Token.Rid;
			uint count = (uint)context.Records.Length;
			SortMDTableCommand.SortTable(context.MetaDataTableVM, rid, count, string.Format("Sort {0} table, RID {1} - {2}", context.MetaDataTableVM.Table, rid, rid + count - 1));
		}

		static bool IsEnabledInternal(MDTableContext context) {
			return TableSorter.CanSort(context.MetaDataTableVM.TableInfo) &&
					context.Records.Length > 1 &&
					context.ContiguousRecords();
		}
	}

	static class GoToRidMDTableCommand {
		[ExportMenuItem(Header = "_Go to RID...", InputGestureText = "Ctrl+G", Group = MenuConstants.GROUP_CTX_CODE_HEX_MD, Order = 20)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "_Go to RID...", InputGestureText = "Ctrl+G", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_MD, Order = 20)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}
		}

		static void ExecuteInternal(MDTableContext context) {
			var ask = new AskForInput();
			ask.Owner = MainWindow.Instance;
			ask.Title = "Go to RID";
			ask.Label.Content = "_RID";
			ask.ShowDialog();
			if (ask.DialogResult != true)
				return;

			string error;
			uint rid = NumberVMUtils.ParseUInt32(ask.TextBox.Text, 1, context.MetaDataTableVM.Rows, out error);
			if (!string.IsNullOrEmpty(error)) {
				MainWindow.Instance.ShowMessageBox(error);
				return;
			}
			if (rid == 0 || rid > context.MetaDataTableVM.Rows) {
				MainWindow.Instance.ShowMessageBox(string.Format("Invalid RID: {0}", rid));
				return;
			}

			var recVM = context.MetaDataTableVM.Get((int)(rid - 1));
			UIUtils.ScrollSelectAndSetFocus(context.ListView, recVM);
		}
	}

	static class ShowInHexEditorMDTableCommand {
		[ExportMenuItem(Header = "Show in He_x Editor", Icon = "Binary", InputGestureText = "Ctrl+X", Group = MenuConstants.GROUP_CTX_CODE_HEX_MD, Order = 30)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}

			public override bool IsEnabled(MDTableContext context) {
				return IsEnabledInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Show in He_x Editor", Icon = "Binary", InputGestureText = "Ctrl+X", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_MD, Order = 30)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}

			public override bool IsEnabled(MDTableContext context) {
				return IsEnabledInternal(context);
			}
		}

		static void ExecuteInternal(MDTableContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		static bool IsEnabledInternal(MDTableContext context) {
			return GetAddressReference(context) != null;
		}

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
		[ExportMenuItem(Header = "Copy as _Text", InputGestureText = "Ctrl+Shift+C", Group = MenuConstants.GROUP_CTX_CODE_HEX_COPY, Order = 0)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}

			public override bool IsEnabled(MDTableContext context) {
				return IsEnabledInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Copy as _Text", InputGestureText = "Ctrl+Shift+C", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_COPY, Order = 0)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}

			public override bool IsEnabled(MDTableContext context) {
				return IsEnabledInternal(context);
			}
		}

		static void ExecuteInternal(MDTableContext context) {
			var output = new PlainTextOutput();
			context.TreeNode.WriteHeader(output);
			foreach (var rec in context.Records)
				context.TreeNode.Write(output, rec);
			var s = output.ToString();
			if (s.Length > 0)
				Clipboard.SetText(s);
		}

		static bool IsEnabledInternal(MDTableContext context) {
			return context.Records.Length > 0;
		}
	}

	static class CopyMDTableCommand {
		[ExportMenuItem(Header = "Cop_y", Icon = "Copy", InputGestureText = "Ctrl+C", Group = MenuConstants.GROUP_CTX_CODE_HEX_COPY, Order = 10)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}

			public override bool IsEnabled(MDTableContext context) {
				return IsEnabledInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Cop_y", Icon = "Copy", InputGestureText = "Ctrl+C", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_COPY, Order = 10)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}

			public override bool IsEnabled(MDTableContext context) {
				return IsEnabledInternal(context);
			}
		}

		static void ExecuteInternal(MDTableContext context) {
			var doc = context.MetaDataTableVM.Document;
			ulong totalSize = (ulong)context.MetaDataTableVM.TableInfo.RowSize * (ulong)context.Records.Length * 2;
			if (totalSize > int.MaxValue) {
				MainWindow.Instance.ShowMessageBox("You've selected too many bytes of data");
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
			if (s.Length > 0)
				Clipboard.SetText(s);
		}

		static bool IsEnabledInternal(MDTableContext context) {
			return context.Records.Length > 0;
		}
	}

	static class PasteMDTableCommand {
		[ExportMenuItem(Header = "_Paste", Icon = "Paste", InputGestureText = "Ctrl+V", Group = MenuConstants.GROUP_CTX_CODE_HEX_COPY, Order = 20)]
		sealed class TheCtxMenuMDTableCommand : CtxMenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}

			public override bool IsEnabled(MDTableContext context) {
				return IsEnabledInternal(context);
			}

			public override string GetHeader(MDTableContext context) {
				return GetHeaderInternal(context);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "_Paste", Icon = "Paste", InputGestureText = "Ctrl+V", Group = MenuConstants.GROUP_APP_MENU_EDIT_HEX_COPY, Order = 20)]
		internal sealed class TheMenuMDTableCommand : MenuMDTableCommand {
			public override void Execute(MDTableContext context) {
				ExecuteInternal(context);
			}

			public override bool IsEnabled(MDTableContext context) {
				return IsEnabledInternal(context);
			}

			public override string GetHeader(MDTableContext context) {
				return GetHeaderInternal(context);
			}
		}

		static void ExecuteInternal(MDTableContext context) {
			var data = GetPasteData(context);
			if (data == null)
				return;

			var doc = context.MetaDataTableVM.Document;
			int recs = data.Length / context.MetaDataTableVM.TableInfo.RowSize;
			WriteHexUndoCommand.AddAndExecute(doc, context.Records[0].StartOffset, data,
				string.Format(recs == 1 ? "Paste {0} {1} record @ {2:X8}, RID {3}" : "Paste {0} {1} records @ {2:X8}, RID {3}",
						recs, context.MetaDataTableVM.Table, context.Records[0].StartOffset, context.Records[0].Token.Rid));
		}

		static bool IsEnabledInternal(MDTableContext context) {
			return GetPasteData(context) != null;
		}

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
			return string.Format("_Paste {0} records @ {1:X8}, RID {2}", recs, context.Records[0].StartOffset, context.Records[0].Token.Rid);
		}
	}
}
