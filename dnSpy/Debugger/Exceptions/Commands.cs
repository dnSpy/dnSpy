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
using System.Linq;
using System.Windows;
using dnlib.DotNet;
using dnSpy.MVVM;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.Debugger.Exceptions {
	sealed class ExceptionsCtxMenuContext {
		public readonly ExceptionsVM VM;
		public readonly ExceptionVM[] SelectedItems;

		public ExceptionsCtxMenuContext(ExceptionsVM vm, ExceptionVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class ExceptionsCtxMenuCommandProxy : ContextMenuEntryCommandProxy {
		public ExceptionsCtxMenuCommandProxy(ExceptionsCtxMenuCommand cmd)
			: base(cmd) {
		}

		protected override ContextMenuEntryContext CreateContext() {
			return ContextMenuEntryContext.Create(ExceptionsControlCreator.ExceptionsControlInstance.listBox);
		}
	}

	abstract class ExceptionsCtxMenuCommand : ContextMenuEntryBase<ExceptionsCtxMenuContext> {
		protected override ExceptionsCtxMenuContext CreateContext(ContextMenuEntryContext context) {
			var ui = ExceptionsControlCreator.ExceptionsControlInstance;
			if (context.Element != ui.listBox)
				return null;
			var vm = ui.DataContext as ExceptionsVM;
			if (vm == null)
				return null;

			var dict = new Dictionary<object, int>(ui.listBox.Items.Count);
			for (int i = 0; i < ui.listBox.Items.Count; i++)
				dict[ui.listBox.Items[i]] = i;
			var elems = ui.listBox.SelectedItems.OfType<ExceptionVM>().ToArray();
			Array.Sort(elems, (a, b) => dict[a].CompareTo(dict[b]));

			return new ExceptionsCtxMenuContext(vm, elems);
		}
	}

	[ExportContextMenuEntry(Header = "Cop_y", Order = 100, Category = "CopyEX", Icon = "Copy", InputGestureText = "Ctrl+C")]
	sealed class CopyCallExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		protected override void Execute(ExceptionsCtxMenuContext context) {
			var output = new PlainTextOutput();
			foreach (var vm in context.SelectedItems) {
				var printer = new ExceptionPrinter(output);
				printer.WriteName(vm);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0)
				Clipboard.SetText(s);
		}

		protected override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportContextMenuEntry(Header = "Select _All", Order = 110, Category = "CopyEX", Icon = "Select", InputGestureText = "Ctrl+A")]
	sealed class SelectAllExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		protected override void Execute(ExceptionsCtxMenuContext context) {
			ExceptionsControlCreator.ExceptionsControlInstance.listBox.SelectAll();
		}

		protected override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return ExceptionsControlCreator.ExceptionsControlInstance.listBox.Items.Count > 0;
		}
	}

	[ExportContextMenuEntry(Header = "Add E_xception", Order = 200, Category = "AddEX", Icon = "Add", InputGestureText = "Ins")]
	sealed class AddExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		protected override void Execute(ExceptionsCtxMenuContext context) {
			context.VM.AddException();
		}

		protected override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return context.VM.CanAddException;
		}
	}

	[ExportContextMenuEntry(Header = "_Remove", Order = 210, Category = "AddEX", Icon = "RemoveCommand", InputGestureText = "Del")]
	sealed class RemoveExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		protected override void Execute(ExceptionsCtxMenuContext context) {
			context.VM.RemoveExceptions();
		}

		protected override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return context.VM.CanRemoveExceptions;
		}
	}

	[ExportContextMenuEntry(Header = "Restore Defaults", Order = 220, Category = "AddEX", Icon = "UndoCheckBoxList")]
	sealed class RestoreDefaultsExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		protected override void Execute(ExceptionsCtxMenuContext context) {
			context.VM.RestoreDefaults();
		}

		protected override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return context.VM.CanRestoreDefaults;
		}
	}

	sealed class ToggleEnableExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		protected override void Execute(ExceptionsCtxMenuContext context) {
			using (ExceptionListSettings.Instance.TemporarilyDisableSave()) {
				foreach (var vm in context.SelectedItems)
					vm.BreakOnFirstChance = !vm.BreakOnFirstChance;
			}
		}
	}

	[ExportContextMenuEntry(Header = "_Enable All Filtered Exceptions", Order = 230, Category = "AddEX")]
	sealed class EnableAllExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		protected override void Execute(ExceptionsCtxMenuContext context) {
			context.VM.EnableAllFilteredExceptions();
		}

		protected override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return context.VM.CanEnableAllFilteredExceptions;
		}
	}

	[ExportContextMenuEntry(Header = "_Disable All Filtered Exceptions", Order = 240, Category = "AddEX")]
	sealed class DisableAllExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		protected override void Execute(ExceptionsCtxMenuContext context) {
			context.VM.DisableAllFilteredExceptions();
		}

		protected override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return context.VM.CanDisableAllFilteredExceptions;
		}
	}

	[ExportContextMenuEntry(Header = "Break When Thrown", Icon = "Add", Order = 800, Category = "Exceptions")]
	sealed class BreakWhenThrownExceptionContextMenuEntry : IContextMenuEntry {
		public void Execute(ContextMenuEntryContext context) {
			var name = GetExceptionTypeName(context);
			if (name == null)
				return;
			ExceptionsControlCreator.ExceptionsVM.AddException(ExceptionType.DotNet, name);
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			return true;
		}

		public bool IsVisible(ContextMenuEntryContext context) {
			var name = GetExceptionTypeName(context);
			if (name == null)
				return false;
			return !ExceptionsControlCreator.ExceptionsVM.Exists(ExceptionType.DotNet, name);
		}

		static string GetExceptionTypeName(ContextMenuEntryContext context) {
			var vm = ExceptionsControlCreator.ExceptionsVM;
			if (vm == null)
				return null;
			var td = GetTypeDef(context);
			if (td == null)
				return null;
			if (!TypeTreeNode.IsException(td))
				return null;
			var name = GetExceptionString(td);
			if (vm.Exists(ExceptionType.DotNet, name))
				return null;
			return name;
		}

		static string GetExceptionString(TypeDef td) {
			//TODO: HACK: do a proper replacement since a namespace/name could contain slashes
			return td.FullName.Replace('/', '.');
		}

		static TypeDef GetTypeDef(ContextMenuEntryContext context) {
			if (context.SelectedTreeNodes != null) {
				if (context.SelectedTreeNodes.Length != 1)
					return null;
				var node = context.SelectedTreeNodes[0] as IMemberTreeNode;
				if (node == null)
					return null;
				return (node.Member as ITypeDefOrRef).ResolveTypeDef();
			}

			if (context.Reference != null)
				return (context.Reference.Reference as ITypeDefOrRef).ResolveTypeDef();

			return null;
		}
	}
}
