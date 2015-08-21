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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Tabs;
using dnSpy.TreeNodes.Hex;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor.Hex {
	sealed class MDTableContext {
		public readonly ListView ListView;
		public readonly MetaDataTableVM MetaDataTableVM;
		public readonly MetaDataTableTreeNode TreeNode;
		public readonly MetaDataTableRecordVM[] Records;

		public MDTableContext(ListView listView, MetaDataTableVM mdVM, MetaDataTableTreeNode mdNode) {
			this.ListView = listView;
			this.MetaDataTableVM = mdVM;
			this.TreeNode = mdNode;
			this.Records = listView.SelectedItems.Cast<MetaDataTableRecordVM>().OrderBy(a => a.StartOffset).ToArray();
		}
	}

	abstract class MDTableCommand : ICommand, IContextMenuEntry2, IMainMenuCommand, IMainMenuCommandInitialize {
		static MDTableContext ToMDTableContext(ContextMenuEntryContext ctx) {
			var listView = ctx.Element as ListView;
			if (listView == null)
				return null;
			var mdVM = listView.DataContext as MetaDataTableVM;
			if (mdVM == null)
				return null;

			return new MDTableContext(listView, mdVM, (MetaDataTableTreeNode)mdVM.Owner);
		}

		static ContextMenuEntryContext CreateContextMenuEntryContext() {
			var tabState = MainWindow.Instance.GetActiveDecompileTabState();
			if (tabState != null) {
				var listView = FindListView(tabState);
				if (listView != null && UIUtils.HasChildrenFocus(listView))
					return ContextMenuEntryContext.Create(listView, true);
			}

			return ContextMenuEntryContext.Create(null, true);
		}

		static ListView FindListView(DecompileTabState tabState) {
			var o = tabState.TabItem.Content as DependencyObject;
			while (o != null) {
				var lv = o as ListView;
				if (lv != null && ContextMenuAP.GetInstall(lv))
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
			var ctx = ToMDTableContext(CreateContextMenuEntryContext());
			return ctx != null && IsVisible(ctx) && IsEnabled(ctx);
		}

		void ICommand.Execute(object parameter) {
			var ctx = ToMDTableContext(CreateContextMenuEntryContext());
			if (ctx != null)
				Execute(ctx);
		}

		void IContextMenuEntry<ContextMenuEntryContext>.Execute(ContextMenuEntryContext context) {
			var ctx = ToMDTableContext(context);
			if (ctx != null)
				Execute(ctx);
		}

		void IContextMenuEntry2<ContextMenuEntryContext>.Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			var ctx = ToMDTableContext(context);
			if (ctx != null)
				Initialize(ctx, menuItem);
		}

		bool IContextMenuEntry<ContextMenuEntryContext>.IsEnabled(ContextMenuEntryContext context) {
			var ctx = ToMDTableContext(context);
			return ctx != null && IsEnabled(ctx);
		}

		bool IContextMenuEntry<ContextMenuEntryContext>.IsVisible(ContextMenuEntryContext context) {
			var ctx = ToMDTableContext(context);
			return ctx != null && IsVisible(ctx);
		}

		bool IMainMenuCommand.IsVisible {
			get {
				var ctx = ToMDTableContext(CreateContextMenuEntryContext());
				return ctx != null && IsVisible(ctx);
			}
		}

		void IMainMenuCommandInitialize.Initialize(MenuItem menuItem) {
			var ctx = ToMDTableContext(CreateContextMenuEntryContext());
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

	[ExportContextMenuEntry(Header = "Copy as Text", Order = 900, Category = "Hex", InputGestureText = "Ctrl+Shift+C")]
	[ExportMainMenuCommand(MenuHeader = "Copy as Text", Menu = "_Edit", MenuOrder = 3900, MenuCategory = "Hex", MenuInputGestureText = "Ctrl+Shift+C")]
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

		public override bool IsVisible(MDTableContext context) {
			return context.Records.Length > 0;
		}
	}
}
