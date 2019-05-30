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
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.ToolWindows.Threads {
	[ExportAutoLoaded]
	sealed class ThreadsCommandsLoader : IAutoLoaded {
		[ImportingConstructor]
		ThreadsCommandsLoader(IWpfCommandService wpfCommandService, Lazy<IThreadsContent> threadsContent) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_THREADS_LISTVIEW);
			cmds.Add(new RelayCommand(a => threadsContent.Value.Operations.Copy(), a => threadsContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.C);
			cmds.Add(new RelayCommand(a => threadsContent.Value.Operations.Copy(), a => threadsContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.Insert);
			cmds.Add(new RelayCommand(a => threadsContent.Value.Operations.SwitchToThread(newTab: false), a => threadsContent.Value.Operations.CanSwitchToThread), ModifierKeys.None, Key.Enter);
			cmds.Add(new RelayCommand(a => threadsContent.Value.Operations.SwitchToThread(newTab: true), a => threadsContent.Value.Operations.CanSwitchToThread), ModifierKeys.Control, Key.Enter);
			cmds.Add(new RelayCommand(a => threadsContent.Value.Operations.SwitchToThread(newTab: true), a => threadsContent.Value.Operations.CanSwitchToThread), ModifierKeys.Shift, Key.Enter);
			cmds.Add(new RelayCommand(a => threadsContent.Value.Operations.RenameThread(), a => threadsContent.Value.Operations.CanRenameThread), ModifierKeys.None, Key.F2);

			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_THREADS_CONTROL);
			cmds.Add(new RelayCommand(a => threadsContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.F);
			cmds.Add(new RelayCommand(a => threadsContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.E);
		}
	}

	sealed class ThreadsCtxMenuContext {
		public ThreadsOperations Operations { get; }
		public ThreadsCtxMenuContext(ThreadsOperations operations) => Operations = operations;
	}

	abstract class ThreadsCtxMenuCommand : MenuItemBase<ThreadsCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<IThreadsContent> threadsContent;

		protected ThreadsCtxMenuCommand(Lazy<IThreadsContent> threadsContent) => this.threadsContent = threadsContent;

		protected sealed override ThreadsCtxMenuContext? CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (context.CreatorObject.Object != threadsContent.Value.ListView)
				return null;
			return Create();
		}

		ThreadsCtxMenuContext Create() => new ThreadsCtxMenuContext(threadsContent.Value.Operations);
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_THREADS_COPY, Order = 0)]
	sealed class CopyThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		[ImportingConstructor]
		CopyThreadsCtxMenuCommand(Lazy<IThreadsContent> threadsContent)
			: base(threadsContent) {
		}

		public override void Execute(ThreadsCtxMenuContext context) => context.Operations.Copy();
		public override bool IsEnabled(ThreadsCtxMenuContext context) => context.Operations.CanCopy;
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_THREADS_COPY, Order = 10)]
	sealed class SelectAllThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		[ImportingConstructor]
		SelectAllThreadsCtxMenuCommand(Lazy<IThreadsContent> threadsContent)
			: base(threadsContent) {
		}

		public override void Execute(ThreadsCtxMenuContext context) => context.Operations.SelectAll();
		public override bool IsEnabled(ThreadsCtxMenuContext context) => context.Operations.CanSelectAll;
	}

	[ExportMenuItem(Header = "res:HexDisplayCommand", Group = MenuConstants.GROUP_CTX_DBG_THREADS_HEXOPTS, Order = 0)]
	sealed class UseHexadecimalThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		[ImportingConstructor]
		UseHexadecimalThreadsCtxMenuCommand(Lazy<IThreadsContent> threadsContent)
			: base(threadsContent) {
		}

		public override void Execute(ThreadsCtxMenuContext context) => context.Operations.ToggleUseHexadecimal();
		public override bool IsEnabled(ThreadsCtxMenuContext context) => context.Operations.CanToggleUseHexadecimal;
		public override bool IsChecked(ThreadsCtxMenuContext context) => context.Operations.UseHexadecimal;
	}

	[Export, ExportMenuItem(Header = "res:SwitchToThreadCommand", InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 10)]
	sealed class SwitchToThreadThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		[ImportingConstructor]
		SwitchToThreadThreadsCtxMenuCommand(Lazy<IThreadsContent> threadsContent)
			: base(threadsContent) {
		}

		public override void Execute(ThreadsCtxMenuContext context) => context.Operations.SwitchToThread(newTab: false);
		public override bool IsEnabled(ThreadsCtxMenuContext context) => context.Operations.CanSwitchToThread;
	}

	[ExportMenuItem(Header = "res:RenameThreadCommand", InputGestureText = "res:ShortCutKeyF2", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 20)]
	sealed class RenameThreadCtxMenuCommand : ThreadsCtxMenuCommand {
		[ImportingConstructor]
		RenameThreadCtxMenuCommand(Lazy<IThreadsContent> threadsContent)
			: base(threadsContent) {
		}

		public override void Execute(ThreadsCtxMenuContext context) => context.Operations.RenameThread();
		public override bool IsEnabled(ThreadsCtxMenuContext context) => context.Operations.CanRenameThread;
	}

	[ExportMenuItem(Header = "res:FreezeThreadCommand", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 30)]
	sealed class FreezeThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		[ImportingConstructor]
		FreezeThreadsCtxMenuCommand(Lazy<IThreadsContent> threadsContent)
			: base(threadsContent) {
		}

		public override void Execute(ThreadsCtxMenuContext context) => context.Operations.FreezeThread();
		public override bool IsEnabled(ThreadsCtxMenuContext context) => context.Operations.CanFreezeThread;
	}

	[ExportMenuItem(Header = "res:ThawThreadCommand", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 40)]
	sealed class ThawThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		[ImportingConstructor]
		ThawThreadsCtxMenuCommand(Lazy<IThreadsContent> threadsContent)
			: base(threadsContent) {
		}

		public override void Execute(ThreadsCtxMenuContext context) => context.Operations.ThawThread();
		public override bool IsEnabled(ThreadsCtxMenuContext context) => context.Operations.CanThawThread;
	}
}
