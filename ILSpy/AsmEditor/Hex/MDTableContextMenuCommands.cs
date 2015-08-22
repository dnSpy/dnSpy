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
using dnSpy.Decompiler;
using dnSpy.HexEditor;
using dnSpy.Tabs;
using dnSpy.TreeNodes;
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

			lv.InputBindings.Add(new KeyBinding(new SortMDTableCommand(), Key.T, ModifierKeys.Shift | ModifierKeys.Control));
			lv.InputBindings.Add(new KeyBinding(new CopyAsTextMDTableCommand(), Key.C, ModifierKeys.Shift | ModifierKeys.Control));
			lv.InputBindings.Add(new KeyBinding(new GoToRidMDTableCommand(), Key.G, ModifierKeys.Control));
			lv.InputBindings.Add(new KeyBinding(new ShowInHexEditorMDTableCommand(), Key.X, ModifierKeys.Control));
			Add(lv, ApplicationCommands.Copy, new CopyMDTableCommand());
			Add(lv, ApplicationCommands.Paste, new PasteMDTableCommand());
		}

		static void Add(UIElement uiElem, RoutedCommand routedCommand, ICommand realCommand) {
			uiElem.CommandBindings.Add(new CommandBinding(routedCommand, (s, e) => realCommand.Execute(null), (s, e) => e.CanExecute = realCommand.CanExecute(null)));
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

	abstract class MDTableCommand : ICommand, IContextMenuEntry2, IMainMenuCommand, IMainMenuCommandInitialize {
		static MDTableContext ToMDTableContext(ContextMenuEntryContext ctx, bool isContextMenu) {
			var listView = ctx.Element as ListView;
			if (listView == null)
				return null;
			var mdVM = listView.DataContext as MetaDataTableVM;
			if (mdVM == null)
				return null;

			return new MDTableContext(listView, mdVM, (MetaDataTableTreeNode)mdVM.Owner, isContextMenu);
		}

		static ContextMenuEntryContext CreateContextMenuEntryContext() {
			var tabState = MainWindow.Instance.GetActiveDecompileTabState();
			if (tabState != null) {
				var listView = FindListView(tabState);
				if (listView != null && UIUtils.HasSelectedChildrenFocus(listView))
					return ContextMenuEntryContext.Create(listView, true);
			}

			return ContextMenuEntryContext.Create(null, true);
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
			var ctx = ToMDTableContext(CreateContextMenuEntryContext(), false);
			return ctx != null && IsVisible(ctx) && IsEnabled(ctx);
		}

		void ICommand.Execute(object parameter) {
			var ctx = ToMDTableContext(CreateContextMenuEntryContext(), false);
			if (ctx != null)
				Execute(ctx);
		}

		void IContextMenuEntry<ContextMenuEntryContext>.Execute(ContextMenuEntryContext context) {
			var ctx = ToMDTableContext(context, true);
			if (ctx != null)
				Execute(ctx);
		}

		void IContextMenuEntry2<ContextMenuEntryContext>.Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			var ctx = ToMDTableContext(context, true);
			if (ctx != null)
				Initialize(ctx, menuItem);
		}

		bool IContextMenuEntry<ContextMenuEntryContext>.IsEnabled(ContextMenuEntryContext context) {
			var ctx = ToMDTableContext(context, true);
			return ctx != null && IsEnabled(ctx);
		}

		bool IContextMenuEntry<ContextMenuEntryContext>.IsVisible(ContextMenuEntryContext context) {
			var ctx = ToMDTableContext(context, true);
			return ctx != null && IsVisible(ctx);
		}

		bool IMainMenuCommand.IsVisible {
			get {
				var ctx = ToMDTableContext(CreateContextMenuEntryContext(), false);
				return ctx != null && IsVisible(ctx);
			}
		}

		void IMainMenuCommandInitialize.Initialize(MenuItem menuItem) {
			var ctx = ToMDTableContext(CreateContextMenuEntryContext(), false);
			if (ctx != null)
				Initialize(ctx, menuItem);
		}

		public abstract void Execute(MDTableContext context);

		public virtual void Initialize(MDTableContext context, MenuItem menuItem) {
		}

		public virtual bool IsEnabled(MDTableContext context) {
			return true;
		}

		public abstract bool IsVisible(MDTableContext context);
	}

	[ExportContextMenuEntry(Header = "_Sort Table", Order = 500, Category = "Hex", InputGestureText = "Ctrl+Shift+T")]
	[ExportMainMenuCommand(MenuHeader = "_Sort Table", Menu = "_Edit", MenuOrder = 3500, MenuCategory = "Hex", MenuInputGestureText = "Ctrl+Shift+T")]
	sealed class SortMDTableCommand : MDTableCommand {
		public override void Execute(MDTableContext context) {
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

		public override bool IsEnabled(MDTableContext context) {
			return TableSorter.CanSort(context.MetaDataTableVM.TableInfo);
		}

		public override bool IsVisible(MDTableContext context) {
			return true;
		}
	}

	[ExportContextMenuEntry(Header = "So_rt Selection", Order = 510, Category = "Hex")]
	[ExportMainMenuCommand(MenuHeader = "So_rt Selection", Menu = "_Edit", MenuOrder = 3510, MenuCategory = "Hex")]
	sealed class SortSelectionMDTableCommand : MDTableCommand {
		public override void Execute(MDTableContext context) {
			uint rid = context.Records[0].Token.Rid;
			uint count = (uint)context.Records.Length;
			SortMDTableCommand.SortTable(context.MetaDataTableVM, rid, count, string.Format("Sort {0} table, RID {1} - {2}", context.MetaDataTableVM.Table, rid, rid + count - 1));
		}

		public override bool IsEnabled(MDTableContext context) {
			return TableSorter.CanSort(context.MetaDataTableVM.TableInfo) &&
					context.Records.Length > 1 &&
					context.ContiguousRecords();
		}

		public override bool IsVisible(MDTableContext context) {
			return true;
		}
	}

	[ExportContextMenuEntry(Header = "_Go to RID…", Order = 520, Category = "Hex", InputGestureText = "Ctrl+G")]
	[ExportMainMenuCommand(MenuHeader = "_Go to RID…", Menu = "_Edit", MenuOrder = 3520, MenuCategory = "Hex", MenuInputGestureText = "Ctrl+G")]
	sealed class GoToRidMDTableCommand : MDTableCommand {
		public override void Execute(MDTableContext context) {
			var ask = new AskForInput();
			ask.Owner = MainWindow.Instance;
			ask.Title = "Go to RID";
			ask.label.Content = "_RID";
			ask.ShowDialog();
			if (ask.DialogResult != true)
				return;

			string error;
			uint rid = NumberVMUtils.ParseUInt32(ask.textBox.Text, 1, context.MetaDataTableVM.Rows, out error);
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

		public override bool IsVisible(MDTableContext context) {
			return true;
		}
	}

	[ExportContextMenuEntry(Header = "Show in He_x Editor", Order = 530, Category = "Hex", Icon = "Binary", InputGestureText = "Ctrl+X")]
	[ExportMainMenuCommand(MenuHeader = "Show in He_x Editor", Menu = "_Edit", MenuOrder = 3530, MenuCategory = "Hex", MenuIcon = "Binary", MenuInputGestureText = "Ctrl+X")]
	sealed class ShowInHexEditorMDTableCommand : MDTableCommand {
		public override void Execute(MDTableContext context) {
			var @ref = GetAddressReference(context);
			if (@ref != null)
				MainWindow.Instance.GoToAddress(@ref);
		}

		public override bool IsEnabled(MDTableContext context) {
			return GetAddressReference(context) != null;
		}

		public override bool IsVisible(MDTableContext context) {
			return true;
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

	[ExportContextMenuEntry(Header = "Copy as _Text", Order = 900, Category = "HexCopy", InputGestureText = "Ctrl+Shift+C")]
	[ExportMainMenuCommand(MenuHeader = "Copy as _Text", Menu = "_Edit", MenuOrder = 3900, MenuCategory = "HexCopy", MenuInputGestureText = "Ctrl+Shift+C")]
	sealed class CopyAsTextMDTableCommand : MDTableCommand {
		public override void Execute(MDTableContext context) {
			var output = new PlainTextOutput();
			context.TreeNode.WriteHeader(output);
			foreach (var rec in context.Records)
				context.TreeNode.Write(output, rec);
			var s = output.ToString();
			if (s.Length > 0)
				Clipboard.SetText(s);
		}

		public override bool IsEnabled(MDTableContext context) {
			return context.Records.Length > 0;
		}

		public override bool IsVisible(MDTableContext context) {
			return true;
		}
	}

	[ExportContextMenuEntry(Header = "Cop_y", Order = 910, Category = "HexCopy", Icon = "Copy", InputGestureText = "Ctrl+C")]
	[ExportMainMenuCommand(MenuHeader = "Cop_y", Menu = "_Edit", MenuOrder = 3910, MenuCategory = "HexCopy", MenuIcon = "Copy", MenuInputGestureText = "Ctrl+C")]
	sealed class CopyMDTableCommand : MDTableCommand {
		public override void Execute(MDTableContext context) {
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

		public override bool IsEnabled(MDTableContext context) {
			return context.Records.Length > 0;
		}

		public override bool IsVisible(MDTableContext context) {
			return true;
		}
	}

	[ExportContextMenuEntry(Header = "_Paste", Order = 920, Category = "HexCopy", Icon = "Paste", InputGestureText = "Ctrl+V")]
	[ExportMainMenuCommand(MenuHeader = "_Paste", Menu = "_Edit", MenuOrder = 3910, MenuCategory = "HexCopy", MenuIcon = "Paste", MenuInputGestureText = "Ctrl+V")]
	sealed class PasteMDTableCommand : MDTableCommand {
		public override void Execute(MDTableContext context) {
			var data = GetPasteData(context);
			if (data == null)
				return;

			var doc = context.MetaDataTableVM.Document;
			int recs = data.Length / context.MetaDataTableVM.TableInfo.RowSize;
			WriteHexUndoCommand.AddAndExecute(doc, context.Records[0].StartOffset, data,
				string.Format(recs == 1 ? "Paste {0} {1} record @ {2:X8}, RID {3}" : "Paste {0} {1} records @ {2:X8}, RID {3}",
						recs, context.MetaDataTableVM.Table, context.Records[0].StartOffset, context.Records[0].Token.Rid));
		}

		public override bool IsEnabled(MDTableContext context) {
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

		public override bool IsVisible(MDTableContext context) {
			return true;
		}

		public override void Initialize(MDTableContext context, MenuItem menuItem) {
			var data = GetPasteData(context);
			if (data == null)
				return;
			int recs = data.Length / context.MetaDataTableVM.TableInfo.RowSize;
			if (recs <= 1)
				return;
			menuItem.Header = string.Format("_Paste {0} records @ {1:X8}, RID {2}", recs, context.Records[0].StartOffset, context.Records[0].Token.Rid);
		}
	}
}
