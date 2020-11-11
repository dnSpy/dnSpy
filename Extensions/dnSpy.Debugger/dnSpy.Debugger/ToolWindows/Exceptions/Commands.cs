/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	[ExportAutoLoaded]
	sealed class ExceptionsCommandsLoader : IAutoLoaded {
		[ImportingConstructor]
		ExceptionsCommandsLoader(IWpfCommandService wpfCommandService, Lazy<IExceptionsContent> exceptionsContent) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_EXCEPTIONS_LISTVIEW);
			cmds.Add(new RelayCommand(a => exceptionsContent.Value.Operations.Copy(), a => exceptionsContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.C);
			cmds.Add(new RelayCommand(a => exceptionsContent.Value.Operations.Copy(), a => exceptionsContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.Insert);
			cmds.Add(new RelayCommand(a => exceptionsContent.Value.Operations.AddException(), a => exceptionsContent.Value.Operations.CanAddException), ModifierKeys.None, Key.Insert);
			cmds.Add(new RelayCommand(a => exceptionsContent.Value.Operations.RemoveExceptions(), a => exceptionsContent.Value.Operations.CanRemoveExceptions), ModifierKeys.None, Key.Delete);
			cmds.Add(new RelayCommand(a => exceptionsContent.Value.Operations.EditConditions(), a => exceptionsContent.Value.Operations.CanEditConditions), ModifierKeys.None, Key.Enter);
			cmds.Add(new RelayCommand(a => exceptionsContent.Value.Operations.EditConditions(), a => exceptionsContent.Value.Operations.CanEditConditions), ModifierKeys.Alt, Key.Enter);
			cmds.Add(new RelayCommand(a => exceptionsContent.Value.Operations.ToggleBreakWhenThrown(), a => exceptionsContent.Value.Operations.CanToggleBreakWhenThrown), ModifierKeys.None, Key.Space);

			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_EXCEPTIONS_CONTROL);
			cmds.Add(new RelayCommand(a => exceptionsContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.F);
			cmds.Add(new RelayCommand(a => exceptionsContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.E);
		}
	}

	sealed class ExceptionsCtxMenuContext {
		public ExceptionsOperations Operations { get; }
		public ExceptionsCtxMenuContext(ExceptionsOperations operations) => Operations = operations;
	}

	abstract class ExceptionsCtxMenuCommand : MenuItemBase<ExceptionsCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<IExceptionsContent> exceptionsContent;

		protected ExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent) => this.exceptionsContent = exceptionsContent;

		protected sealed override ExceptionsCtxMenuContext? CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (context.CreatorObject.Object != exceptionsContent.Value.ListView)
				return null;
			return Create();
		}

		ExceptionsCtxMenuContext Create() => new ExceptionsCtxMenuContext(exceptionsContent.Value.Operations);
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_COPY, Order = 0)]
	sealed class CopyExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		CopyExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => context.Operations.Copy();
		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.Operations.CanCopy;
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_COPY, Order = 10)]
	sealed class SelectAllExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		SelectAllExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => context.Operations.SelectAll();
		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.Operations.CanSelectAll;
	}

	[ExportMenuItem(Header = "res:AddExceptionCommand", Icon = DsImagesAttribute.Add, InputGestureText = "res:ShortCutKeyInsert", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 0)]
	sealed class AddExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		AddExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => context.Operations.AddException();
		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.Operations.CanAddException;
	}

	[ExportMenuItem(Header = "res:RemoveExceptionCommand", Icon = DsImagesAttribute.RemoveCommand, InputGestureText = "res:ShortCutKeyDelete", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 10)]
	sealed class RemoveExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		RemoveExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => context.Operations.RemoveExceptions();
		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.Operations.CanRemoveExceptions;
	}

	[ExportMenuItem(Header = "res:EditConditionsCommand", Icon = DsImagesAttribute.Edit, InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 20)]
	sealed class EditConditionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		EditConditionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => context.Operations.EditConditions();
		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.Operations.CanEditConditions;
	}

	[ExportMenuItem(Header = "res:RestoreDefaultExceptionSettingsCommand", Icon = DsImagesAttribute.UndoCheckBoxList, Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 1000)]
	sealed class RestoreDefaultsExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		RestoreDefaultsExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => context.Operations.RestoreSettings();
		public override bool IsEnabled(ExceptionsCtxMenuContext context) => context.Operations.CanRestoreSettings;
	}

	static class Constants {
		public const string EXCEPTIONCATEGORIES_GUID = "BD58CAFA-AEA4-4157-88FF-810933F73B42";
		public const string GROUP_EXCEPTIONCATEGORIES = "0,7D584240-4999-4956-A4E4-744F7846746E";
	}

	[Export(typeof(ExceptionsCommandContext))]
	sealed class ExceptionsCommandContext {
		public Lazy<IExceptionsVM> VM { get; }
		public Lazy<IExceptionsContent> ExceptionsContent { get; }
		public Lazy<DbgExceptionSettingsService> ExceptionSettingsService { get; }

		[ImportingConstructor]
		ExceptionsCommandContext(Lazy<IExceptionsVM> vm, Lazy<IExceptionsContent> exceptionsContent, Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService) {
			VM = vm;
			ExceptionsContent = exceptionsContent;
			ExceptionSettingsService = dbgExceptionSettingsService;
		}
	}

	[ExportMenuItem(Header = "res:ExceptionsCommand", Icon = DsImagesAttribute.ExceptionPublic, Guid = Constants.EXCEPTIONCATEGORIES_GUID, Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_OPTIONS, Order = 0)]
	sealed class ExceptionsContextMenuEntry : MenuItemBase<ExceptionsContextMenuEntry.Context> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		internal sealed class Context { }

		readonly ExceptionsCommandContext exceptionsCommandContext;

		[ImportingConstructor]
		ExceptionsContextMenuEntry(ExceptionsCommandContext exceptionsCommandContext) => this.exceptionsCommandContext = exceptionsCommandContext;

		public override void Execute(Context context) { }
		protected override Context? CreateContext(IMenuItemContext context) => CreateContext(exceptionsCommandContext, context);

		internal static Context? CreateContext(ExceptionsCommandContext exceptionsCommandContext, IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (context.CreatorObject.Object != exceptionsCommandContext.ExceptionsContent.Value.ListView)
				return null;
			return new Context();
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.EXCEPTIONCATEGORIES_GUID, Group = Constants.GROUP_EXCEPTIONCATEGORIES, Order = 0)]
	sealed class ExceptionsSubContextMenuEntry : MenuItemBase, IMenuItemProvider {
		readonly ExceptionsCommandContext exceptionsCommandContext;

		[ImportingConstructor]
		ExceptionsSubContextMenuEntry(ExceptionsCommandContext exceptionsCommandContext) => this.exceptionsCommandContext = exceptionsCommandContext;

		public override void Execute(IMenuItemContext context) { }

		IEnumerable<CreatedMenuItem> IMenuItemProvider.Create(IMenuItemContext context) {
			var ctx = ExceptionsContextMenuEntry.CreateContext(exceptionsCommandContext, context);
			Debug2.Assert(ctx is not null);
			if (ctx is null)
				yield break;

			var currentCategory = exceptionsCommandContext.VM.Value.SelectedCategory;
			foreach (var category in exceptionsCommandContext.VM.Value.ExceptionCategoryCollection) {
				var attr = new ExportMenuItemAttribute { Header = UIUtilities.EscapeMenuItemHeader(category.DisplayName) };
				bool isChecked = category == currentCategory;
				var item = new DynamicCheckableMenuItem(ctx2 => exceptionsCommandContext.VM.Value.SelectedCategory = category, isChecked);
				yield return new CreatedMenuItem(attr, item);
			}
		}
	}

	[ExportMenuItem(Header = "res:FindCommand", Icon = DsImagesAttribute.Search, InputGestureText = "res:ShortCutKeyCtrlF", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_OPTIONS, Order = 1000)]
	sealed class FindExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		[ImportingConstructor]
		FindExceptionsCtxMenuCommand(Lazy<IExceptionsContent> exceptionsContent)
			: base(exceptionsContent) {
		}

		public override void Execute(ExceptionsCtxMenuContext context) => exceptionsContent.Value.FocusSearchTextBox();
	}
}
