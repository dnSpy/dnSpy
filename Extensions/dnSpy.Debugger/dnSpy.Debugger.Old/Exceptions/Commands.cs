/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Debugger.Exceptions {
	//[ExportAutoLoaded]
	sealed class ExceptionsContentCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		ExceptionsContentCommandLoader(IWpfCommandService wpfCommandService, Lazy<IExceptionsContent> exceptionsContent, CopyCallExceptionsCtxMenuCommand copyCmd, AddExceptionsCtxMenuCommand addExCmd, RemoveExceptionsCtxMenuCommand removeExCmd, ToggleEnableExceptionsCtxMenuCommand toggleExCmd) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_EXCEPTIONS_LISTVIEW);
			cmds.Add(ApplicationCommands.Copy, new ExceptionsCtxMenuCommandProxy(copyCmd));
			cmds.Add(new ExceptionsCtxMenuCommandProxy(addExCmd), ModifierKeys.None, Key.Insert);
			cmds.Add(new ExceptionsCtxMenuCommandProxy(removeExCmd), ModifierKeys.None, Key.Delete);
			cmds.Add(new ExceptionsCtxMenuCommandProxy(toggleExCmd), ModifierKeys.None, Key.Space);

			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_EXCEPTIONS_CONTROL);
			cmds.Add(new RelayCommand(a => exceptionsContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.F);
			cmds.Add(new RelayCommand(a => exceptionsContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.E);
		}
	}

	//[ExportAutoLoaded]
	sealed class CallStackCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CallStackCommandLoader(IWpfCommandService wpfCommandService, IDsToolWindowService toolWindowService) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);

			cmds.Add(DebugRoutedCommands.ShowExceptions, new RelayCommand(a => toolWindowService.Show(ExceptionsToolWindowContent.THE_GUID)));
			cmds.Add(DebugRoutedCommands.ShowExceptions, ModifierKeys.Control | ModifierKeys.Alt, Key.E);
		}
	}

	sealed class ExceptionsCtxMenuContext {
		public IExceptionsVM VM { get; }
		public ExceptionVM[] SelectedItems { get; }

		public ExceptionsCtxMenuContext(IExceptionsVM vm, ExceptionVM[] selItems) {
			VM = vm;
			SelectedItems = selItems;
		}
	}

	sealed class ExceptionsCtxMenuCommandProxy : MenuItemCommandProxy<ExceptionsCtxMenuContext> {
		readonly ExceptionsCtxMenuCommand cmd;

		public ExceptionsCtxMenuCommandProxy(ExceptionsCtxMenuCommand cmd)
			: base(cmd) {
			this.cmd = cmd;
		}

		protected override ExceptionsCtxMenuContext CreateContext() => cmd.Create();
	}

	abstract class ExceptionsCtxMenuCommand : MenuItemBase<ExceptionsCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<IExceptionsContent> exceptionsContent;

		protected ExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent) {
			this.exceptionsContent = exceptionsContent;
		}

		protected sealed override ExceptionsCtxMenuContext CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object != null && context.CreatorObject.Object.GetType() == typeof(ListBox)))
				return null;
			if (context.CreatorObject.Object != exceptionsContent.Value.ListBox)
				return null;
			return Create();
		}

		internal ExceptionsCtxMenuContext Create() {
			var ui = exceptionsContent.Value;
			var vm = exceptionsContent.Value.ExceptionsVM;

			var dict = new Dictionary<object, int>(ui.ListBox.Items.Count);
			for (int i = 0; i < ui.ListBox.Items.Count; i++)
				dict[ui.ListBox.Items[i]] = i;
			var elems = ui.ListBox.SelectedItems.OfType<ExceptionVM>().ToArray();
			Array.Sort(elems, (a, b) => dict[a].CompareTo(dict[b]));

			return new ExceptionsCtxMenuContext(vm, elems);
		}
	}

	//[Export, ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_COPY, Order = 0)]
	sealed class CopyCallExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		CopyCallExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) {
			var output = new StringBuilderTextColorOutput();
			foreach (var vm in context.SelectedItems) {
				var printer = new ExceptionPrinter(output);
				printer.WriteName(vm);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.SelectedItems.Length > 0;
	}

	//[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_COPY, Order = 10)]
	sealed class SelectAllExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		SelectAllExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => exceptionsContent.Value.ListBox.SelectAll();
		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.SelectedItems.Length > 0;
	}

	//[Export, ExportMenuItem(Header = "res:AddExceptionCommand", Icon = DsImagesAttribute.Add, InputGestureText = "res:ShortCutKeyInsert", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 0)]
	sealed class AddExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		AddExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => context.VM.AddException();
		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.VM.CanAddException;
	}

	//[Export, ExportMenuItem(Header = "res:RemoveExceptionCommand", Icon = DsImagesAttribute.RemoveCommand, InputGestureText = "res:ShortCutKeyDelete", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 10)]
	sealed class RemoveExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		RemoveExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => context.VM.RemoveExceptions();
		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.VM.CanRemoveExceptions;
	}

	//[ExportMenuItem(Header = "res:RestoreDefaultExceptionSettingsCommand", Icon = DsImagesAttribute.UndoCheckBoxList, Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 20)]
	sealed class RestoreDefaultsExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		RestoreDefaultsExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => context.VM.RestoreDefaults();
		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.VM.CanRestoreDefaults;
	}

	//[Export]
	sealed class ToggleEnableExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		readonly IExceptionListSettings exceptionListSettings;

		[ImportingConstructor]
		ToggleEnableExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent, IExceptionListSettings exceptionListSettings)
			: base(exceptionsContent) {
			this.exceptionListSettings = exceptionListSettings;
		}

		public override void Execute(ExceptionsCtxMenuContext context) {
			using (exceptionListSettings.TemporarilyDisableSave()) {
				foreach (var vm in context.SelectedItems)
					vm.BreakOnFirstChance = !vm.BreakOnFirstChance;
			}
		}
	}

	//[ExportMenuItem(Header = "res:EnableAllFilteredExceptionsCommand", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 30)]
	sealed class EnableAllExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		EnableAllExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => context.VM.EnableAllFilteredExceptions();
		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.VM.CanEnableAllFilteredExceptions;
	}

	//[ExportMenuItem(Header = "res:DisableAllFilteredExceptionsCommand", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 40)]
	sealed class DisableAllExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		DisableAllExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => context.VM.DisableAllFilteredExceptions();
		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.VM.CanDisableAllFilteredExceptions;
	}

	static class BreakWhenThrownExceptionCommand {
		abstract class CommandBase : MenuItemBase<string> {
			protected readonly Lazy<IExceptionsContent> exceptionsContent;

			protected CommandBase(Lazy<IExceptionsContent> exceptionsContent) {
				this.exceptionsContent = exceptionsContent;
			}

			protected sealed override string CreateContext(IMenuItemContext context) => GetExceptionTypeName(context);

			public override void Execute(string context) {
				if (context == null)
					return;
				exceptionsContent.Value.ExceptionsVM.BreakWhenThrown(ExceptionType.DotNet, context);
			}

			string GetExceptionTypeName(IMenuItemContext context) {
				var vm = exceptionsContent.Value.ExceptionsVM;
				var td = GetTypeDef(context);
				if (td == null)
					return null;
				if (!IsException(td))
					return null;
				return GetExceptionString(td);
			}

			static bool IsException(TypeDef type) {
				if (IsSystemException(type))
					return true;
				while (type != null) {
					if (IsSystemException(type.BaseType))
						return true;
					var bt = type.BaseType;
					type = bt == null ? null : bt.ScopeType.ResolveTypeDef();
				}
				return false;
			}

			static bool IsSystemException(ITypeDefOrRef type) =>
				type != null &&
				type.DeclaringType == null &&
				type.Namespace == "System" &&
				type.Name == "Exception" &&
				type.DefinitionAssembly.IsCorLib();

			//TODO: HACK: do a proper replacement since a namespace/name could contain slashes
			static string GetExceptionString(TypeDef td) => td.FullName.Replace('/', '.');

			protected abstract TypeDef GetTypeDef(IMenuItemContext context);

			protected TypeDef GetTypeDefFromTreeNodes(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					return null;
				var nodes = context.Find<TreeNodeData[]>();
				if (nodes == null || nodes.Length != 1)
					return null;
				var node = nodes[0] as IMDTokenNode;
				if (node == null)
					return null;
				return (node.Reference as ITypeDefOrRef).ResolveTypeDef();
			}

			protected TypeDef GetTypeDefFromReference(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					return null;

				var @ref = context.Find<TextReference>();
				if (@ref == null || @ref.Reference == null)
					return null;

				return (@ref.Reference as ITypeDefOrRef).ResolveTypeDef();
			}
		}

		//[ExportMenuItem(Header = "res:BreakWhenExceptionThrownCommand", Icon = DsImagesAttribute.Add, Group = MenuConstants.GROUP_CTX_DOCUMENTS_DEBUG, Order = 0)]
		sealed class FilesCommand : CommandBase {
			protected sealed override object CachedContextKey => ContextKey;
			static readonly object ContextKey = new object();

			[ImportingConstructor]
			FilesCommand(Lazy<IExceptionsContent> exceptionsContent)
				: base(exceptionsContent) {
			}

			protected override TypeDef GetTypeDef(IMenuItemContext context) => GetTypeDefFromTreeNodes(context, MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID);
		}

		//[ExportMenuItem(Header = "res:BreakWhenExceptionThrownCommand", Icon = DsImagesAttribute.Add, Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 1000)]
		sealed class CodeCommand : CommandBase {
			protected sealed override object CachedContextKey => ContextKey;
			static readonly object ContextKey = new object();

			[ImportingConstructor]
			CodeCommand(Lazy<IExceptionsContent> exceptionsContent)
				: base(exceptionsContent) {
			}

			protected override TypeDef GetTypeDef(IMenuItemContext context) => GetTypeDefFromReference(context, MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID);
		}
	}
}
