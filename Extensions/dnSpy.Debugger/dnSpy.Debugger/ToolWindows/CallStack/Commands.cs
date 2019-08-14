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
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.CallStack {
	[ExportAutoLoaded]
	sealed class CallStackCommandsLoader : IAutoLoaded {
		[ImportingConstructor]
		CallStackCommandsLoader(IWpfCommandService wpfCommandService, Lazy<ICallStackContent> callStackContent) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_CALLSTACK_LISTVIEW);
			cmds.Add(new RelayCommand(a => callStackContent.Value.Operations.Copy(), a => callStackContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.C);
			cmds.Add(new RelayCommand(a => callStackContent.Value.Operations.Copy(), a => callStackContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.Insert);
			cmds.Add(new RelayCommand(a => callStackContent.Value.Operations.SwitchToFrame(false), a => callStackContent.Value.Operations.CanSwitchToFrame), ModifierKeys.None, Key.Enter);
			cmds.Add(new RelayCommand(a => callStackContent.Value.Operations.SwitchToFrame(true), a => callStackContent.Value.Operations.CanSwitchToFrame), ModifierKeys.Control, Key.Enter);
			cmds.Add(new RelayCommand(a => callStackContent.Value.Operations.SwitchToFrame(true), a => callStackContent.Value.Operations.CanSwitchToFrame), ModifierKeys.Shift, Key.Enter);
			cmds.Add(new RelayCommand(a => callStackContent.Value.Operations.RunToCursor(), a => callStackContent.Value.Operations.CanRunToCursor), ModifierKeys.Control, Key.F10);
			cmds.Add(new RelayCommand(a => callStackContent.Value.Operations.AddBreakpoint(), a => callStackContent.Value.Operations.CanAddBreakpoint), ModifierKeys.None, Key.F9);
			cmds.Add(new RelayCommand(a => callStackContent.Value.Operations.EnableBreakpoint(), a => callStackContent.Value.Operations.CanEnableBreakpoint), ModifierKeys.Control, Key.F9);
		}
	}

	sealed class CallStackCtxMenuContext {
		public CallStackOperations Operations { get; }
		public CallStackCtxMenuContext(CallStackOperations operations) => Operations = operations;
	}

	abstract class CallStackCtxMenuCommand : MenuItemBase<CallStackCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<ICallStackContent> callStackContent;

		protected CallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent) => this.callStackContent = callStackContent;

		protected sealed override CallStackCtxMenuContext? CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (context.CreatorObject.Object != callStackContent.Value.ListView)
				return null;
			return Create();
		}

		CallStackCtxMenuContext Create() => new CallStackCtxMenuContext(callStackContent.Value.Operations);
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_COPY, Order = 0)]
	sealed class CopyCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		CopyCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.Copy();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanCopy;
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_COPY, Order = 10)]
	sealed class SelectAllCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		SelectAllCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.SelectAll();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanSelectAll;
	}

	[ExportMenuItem(Header = "res:SwitchToFrameCommand", InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 0)]
	sealed class SwitchToFrameCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		SwitchToFrameCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.SwitchToFrame(false);
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanSwitchToFrame;
	}

	[ExportMenuItem(Header = "res:GoToSourceCodeCommand", Icon = DsImagesAttribute.GoToSourceCode, Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 10)]
	sealed class GoToSourceCodeCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		GoToSourceCodeCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.GoToSourceCode(false);
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanGoToSourceCode;
	}

	[ExportMenuItem(Header = "res:GoToDisassemblyCommand2", Icon = DsImagesAttribute.DisassemblyWindow, Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 20)]
	sealed class GoToDisassemblyCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		GoToDisassemblyCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.GoToDisassembly();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanGoToDisassembly;
	}

	[ExportMenuItem(Header = "res:RunToCursorCommand", Icon = DsImagesAttribute.Cursor, InputGestureText = "res:ShortCutKeyCtrlF10", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 30)]
	sealed class RunToCursorCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		RunToCursorCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.RunToCursor();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanRunToCursor;
	}

	[ExportMenuItem(Header = "res:UnwindToThisFrameCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 40)]
	sealed class UnwindToThisFrameCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		UnwindToThisFrameCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.UnwindToFrame();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanUnwindToFrame;
	}

	[ExportMenuItem(Header = "res:LanguageCommand", Guid = Constants.LANGUAGE_GUID, Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 100)]
	sealed class LanguageCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		LanguageCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) { }
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.GetLanguages().Any(a => a.Name != PredefinedDbgLanguageNames.None);
	}

	[ExportMenuItem(OwnerGuid = Constants.LANGUAGE_GUID, Group = Constants.GROUP_LANGUAGE, Order = 0)]
	sealed class LanguageXCallStackCtxMenuCommand : CallStackCtxMenuCommand, IMenuItemProvider {
		[ImportingConstructor]
		LanguageXCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) { }

		IEnumerable<CreatedMenuItem> IMenuItemProvider.Create(IMenuItemContext context) {
			var ctx = CreateContext(context);
			Debug2.Assert(!(ctx is null));
			if (ctx is null)
				yield break;

			var languages = ctx.Operations.GetLanguages();
			if (languages.Count == 0)
				yield break;

			var currentLanguage = ctx.Operations.GetCurrentLanguage();
			foreach (var language in languages.OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase)) {
				var attr = new ExportMenuItemAttribute { Header = UIUtilities.EscapeMenuItemHeader(language.DisplayName) };
				var cmd = new SetLanguageWindowModulesCtxMenuCommand(callStackContent, language, language == currentLanguage);
				yield return new CreatedMenuItem(attr, cmd);
			}
		}
	}

	sealed class SetLanguageWindowModulesCtxMenuCommand : CallStackCtxMenuCommand {
		readonly DbgLanguage language;
		readonly bool isChecked;
		public SetLanguageWindowModulesCtxMenuCommand(Lazy<ICallStackContent> callStackContent, DbgLanguage language, bool isChecked)
			: base(callStackContent) {
			this.language = language;
			this.isChecked = isChecked;
		}
		public override void Execute(CallStackCtxMenuContext context) => context.Operations.SetCurrentLanguage(language);
		public override bool IsChecked(CallStackCtxMenuContext context) => isChecked;
	}

	static class Constants {
		public const string BREAKPOINTS_GUID = "30BC5C80-9029-4031-9C18-12735CD2894A";
		public const string GROUP_BREAKPOINTS = "0,049B3FDE-E355-447E-8D38-C31FD3563C57";
		public const string GROUP_BREAKPOINTS_SETTINGS = "1000,9653A837-BE49-4E6D-AB17-4B0C004BE33F";
		public const string GROUP_BREAKPOINTS_EDIT = "2000,D49A6DD7-54CD-439F-910A-DD251FA87826";
		public const string GROUP_BREAKPOINTS_EXPORT = "3000,B942A045-1490-485B-A923-B65C27898ED2";
		public const string LANGUAGE_GUID = "F8AFC617-943F-4D7A-ACE0-3F7244DB71DE";
		public const string GROUP_LANGUAGE = "0,68720804-8255-4663-B4DA-DAF328AD48F8";
	}

	[ExportMenuItem(Header = "res:CallStackBreakpointCommand", Guid = Constants.BREAKPOINTS_GUID, Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_BPS, Order = 0)]
	sealed class BreakpointCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		BreakpointCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) { }
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.BreakpointsCommandKind != BreakpointsCommandKind.None;
	}

	[ExportMenuItem(OwnerGuid = Constants.BREAKPOINTS_GUID, Header = "res:InsertBreakpointCommand", Icon = DsImagesAttribute.CheckDot, InputGestureText = "res:ShortCutKeyF9", Group = Constants.GROUP_BREAKPOINTS, Order = 0)]
	sealed class InsertBreakpointCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		InsertBreakpointCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.AddBreakpoint();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanAddBreakpoint;
		public override bool IsVisible(CallStackCtxMenuContext context) => context.Operations.BreakpointsCommandKind == BreakpointsCommandKind.Add;
	}

	[ExportMenuItem(OwnerGuid = Constants.BREAKPOINTS_GUID, Header = "res:InsertTracepointCommand", Group = Constants.GROUP_BREAKPOINTS, Order = 10)]
	sealed class InsertTracepointCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		InsertTracepointCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.AddTracepoint();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanAddTracepoint;
		public override bool IsVisible(CallStackCtxMenuContext context) => context.Operations.BreakpointsCommandKind == BreakpointsCommandKind.Add;
	}

	[ExportMenuItem(OwnerGuid = Constants.BREAKPOINTS_GUID, Header = "res:DeleteBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = Constants.GROUP_BREAKPOINTS_SETTINGS, Order = 0)]
	sealed class DeleteBreakpointCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		DeleteBreakpointCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.RemoveBreakpoint();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanRemoveBreakpoint;
		public override bool IsVisible(CallStackCtxMenuContext context) => context.Operations.BreakpointsCommandKind == BreakpointsCommandKind.Edit;
	}

	[ExportMenuItem(OwnerGuid = Constants.BREAKPOINTS_GUID, Icon = DsImagesAttribute.ToggleAllBreakpoints, InputGestureText = "res:ShortCutKeyCtrlF9", Group = Constants.GROUP_BREAKPOINTS_SETTINGS, Order = 10)]
	sealed class ToggleBreakpointCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		ToggleBreakpointCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.EnableBreakpoint();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanEnableBreakpoint;
		public override bool IsVisible(CallStackCtxMenuContext context) => context.Operations.BreakpointsCommandKind == BreakpointsCommandKind.Edit;
		public override string? GetHeader(CallStackCtxMenuContext context) => context.Operations.IsBreakpointEnabled ? dnSpy_Debugger_Resources.DisableBreakpointCommand2 : dnSpy_Debugger_Resources.EnableBreakpointCommand2;
	}

	[ExportMenuItem(OwnerGuid = Constants.BREAKPOINTS_GUID, Header = "res:SettingsCommand2", Icon = DsImagesAttribute.Settings, Group = Constants.GROUP_BREAKPOINTS_EDIT, Order = 0)]
	sealed class EditBreakpointSettingsCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		EditBreakpointSettingsCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.EditBreakpointSettings();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanEditBreakpointSettings;
		public override bool IsVisible(CallStackCtxMenuContext context) => context.Operations.BreakpointsCommandKind == BreakpointsCommandKind.Edit;
	}

	[ExportMenuItem(OwnerGuid = Constants.BREAKPOINTS_GUID, Header = "res:ExportCommand", Icon = DsImagesAttribute.Open, Group = Constants.GROUP_BREAKPOINTS_EXPORT, Order = 0)]
	sealed class ExportBreakpointCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		ExportBreakpointCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ExportBreakpoint();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanExportBreakpoint;
		public override bool IsVisible(CallStackCtxMenuContext context) => context.Operations.BreakpointsCommandKind == BreakpointsCommandKind.Edit;
	}

	[ExportMenuItem(Header = "res:HexDisplayCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_HEXOPTS, Order = 0)]
	sealed class UseHexadecimalCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		UseHexadecimalCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ToggleUseHexadecimal();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanToggleUseHexadecimal;
		public override bool IsChecked(CallStackCtxMenuContext context) => context.Operations.UseHexadecimal;
	}

	[ExportMenuItem(Header = "res:DigitSeparatorsCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_HEXOPTS, Order = 10)]
	sealed class UseDigitSeparatorsCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		UseDigitSeparatorsCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ToggleUseDigitSeparators();
		public override bool IsEnabled(CallStackCtxMenuContext context) => context.Operations.CanToggleUseDigitSeparators;
		public override bool IsChecked(CallStackCtxMenuContext context) => context.Operations.UseDigitSeparators;
	}

	[ExportMenuItem(Header = "res:ShowModuleNamesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 0)]
	sealed class ShowModuleNamesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		ShowModuleNamesCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ShowModuleNames = !context.Operations.ShowModuleNames;
		public override bool IsChecked(CallStackCtxMenuContext context) => context.Operations.ShowModuleNames;
	}

	[ExportMenuItem(Header = "res:ShowParameterTypesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 10)]
	sealed class ShowParameterTypesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		ShowParameterTypesCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ShowParameterTypes = !context.Operations.ShowParameterTypes;
		public override bool IsChecked(CallStackCtxMenuContext context) => context.Operations.ShowParameterTypes;
	}

	[ExportMenuItem(Header = "res:ShowParameterNamesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 20)]
	sealed class ShowParameterNamesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		ShowParameterNamesCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ShowParameterNames = !context.Operations.ShowParameterNames;
		public override bool IsChecked(CallStackCtxMenuContext context) => context.Operations.ShowParameterNames;
	}

	[ExportMenuItem(Header = "res:ShowParameterValuesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 30)]
	sealed class ShowParameterValuesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		ShowParameterValuesCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ShowParameterValues = !context.Operations.ShowParameterValues;
		public override bool IsChecked(CallStackCtxMenuContext context) => context.Operations.ShowParameterValues;
	}

	[ExportMenuItem(Header = "res:ShowInstructionPointerCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 40)]
	sealed class ShowFunctionOffsetCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		ShowFunctionOffsetCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ShowFunctionOffset = !context.Operations.ShowFunctionOffset;
		public override bool IsChecked(CallStackCtxMenuContext context) => context.Operations.ShowFunctionOffset;
	}

	[ExportMenuItem(Header = "res:ShowDeclaringTypesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 50)]
	sealed class ShowDeclaringTypesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		ShowDeclaringTypesCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ShowDeclaringTypes = !context.Operations.ShowDeclaringTypes;
		public override bool IsChecked(CallStackCtxMenuContext context) => context.Operations.ShowDeclaringTypes;
	}

	[ExportMenuItem(Header = "res:ShowNamespacesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 60)]
	sealed class ShowNamespacesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		ShowNamespacesCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ShowNamespaces = !context.Operations.ShowNamespaces;
		public override bool IsChecked(CallStackCtxMenuContext context) => context.Operations.ShowNamespaces;
	}

	[ExportMenuItem(Header = "res:ShowReturnTypesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 70)]
	sealed class ShowReturnTypesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		ShowReturnTypesCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ShowReturnTypes = !context.Operations.ShowReturnTypes;
		public override bool IsChecked(CallStackCtxMenuContext context) => context.Operations.ShowReturnTypes;
	}

	[ExportMenuItem(Header = "res:ShowIntrinsicTypeKeywordsCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 80)]
	sealed class ShowTypeKeywordsCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		ShowTypeKeywordsCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ShowIntrinsicTypeKeywords = !context.Operations.ShowIntrinsicTypeKeywords;
		public override bool IsChecked(CallStackCtxMenuContext context) => context.Operations.ShowIntrinsicTypeKeywords;
	}

	[ExportMenuItem(Header = "res:ShowTokensCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 90)]
	sealed class ShowTokensCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		ShowTokensCallStackCtxMenuCommand(Lazy<ICallStackContent> callStackContent)
			: base(callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) => context.Operations.ShowTokens = !context.Operations.ShowTokens;
		public override bool IsChecked(CallStackCtxMenuContext context) => context.Operations.ShowTokens;
	}
}
