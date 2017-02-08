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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Debugger.Memory;
using dnSpy.Debugger.Old.Properties;

namespace dnSpy.Debugger.Locals {
	//[ExportAutoLoaded]
	sealed class AutoShowDebuggerWindowsLoader : IAutoLoaded {
		readonly IDebuggerSettings debuggerSettings;
		readonly ITheDebugger theDebugger;
		readonly IDsToolWindowService toolWindowService;

		[ImportingConstructor]
		AutoShowDebuggerWindowsLoader(IDebuggerSettings debuggerSettings, ITheDebugger theDebugger, IDsToolWindowService toolWindowService) {
			this.debuggerSettings = debuggerSettings;
			this.theDebugger = theDebugger;
			this.toolWindowService = toolWindowService;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
		}

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			if (debuggerSettings.AutoOpenLocalsWindow && theDebugger.ProcessState == DebuggerProcessState.Starting)
				toolWindowService.Show(LocalsToolWindowContent.THE_GUID);
		}
	}

	//[ExportAutoLoaded]
	sealed class LocalsContentCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		LocalsContentCommandLoader(IWpfCommandService wpfCommandService, Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent, IMemoryWindowService memoryWindowService, CopyLocalsCtxMenuCommand copyCmd, EditValueLocalsCtxMenuCommand editValueCmd, CopyValueLocalsCtxMenuCommand copyValueCmd, ToggleCollapsedLocalsCtxMenuCommand toggleCollapsedCmd, ShowInMemoryLocalsCtxMenuCommand showInMemCmd) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_LOCALS_LISTVIEW);
			cmds.Add(ApplicationCommands.Copy, new LocalsCtxMenuCommandProxy(copyCmd));
			cmds.Add(new LocalsCtxMenuCommandProxy(editValueCmd), ModifierKeys.None, Key.F2);
			cmds.Add(new LocalsCtxMenuCommandProxy(copyValueCmd), ModifierKeys.Control | ModifierKeys.Shift, Key.C);
			cmds.Add(new LocalsCtxMenuCommandProxy(toggleCollapsedCmd), ModifierKeys.None, Key.Enter);
			cmds.Add(new LocalsCtxMenuCommandProxy(showInMemCmd), ModifierKeys.Control, Key.X);
			for (int i = 0; i < Memory.MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS && i < 10; i++)
				cmds.Add(new LocalsCtxMenuCommandProxy(new ShowInMemoryWindowLocalsCtxMenuCommand(theDebugger, localsContent, memoryWindowService, i)), ModifierKeys.Control, Key.D0 + (i + 1) % 10);
		}
	}

	//[ExportAutoLoaded]
	sealed class CallStackCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CallStackCommandLoader(IWpfCommandService wpfCommandService, IDsToolWindowService toolWindowService) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(DebugRoutedCommands.ShowLocals, new RelayCommand(a => toolWindowService.Show(LocalsToolWindowContent.THE_GUID)));
			cmds.Add(DebugRoutedCommands.ShowLocals, ModifierKeys.Alt, Key.D4);
		}
	}

	sealed class LocalsCtxMenuContext {
		public readonly ILocalsVM VM;
		public readonly ValueVM[] SelectedItems;

		public LocalsCtxMenuContext(ILocalsVM vm, ValueVM[] selItems) {
			VM = vm;
			SelectedItems = selItems;
		}
	}

	sealed class LocalsCtxMenuCommandProxy : MenuItemCommandProxy<LocalsCtxMenuContext> {
		readonly LocalsCtxMenuCommand cmd;

		public LocalsCtxMenuCommandProxy(LocalsCtxMenuCommand cmd)
			: base(cmd) {
			this.cmd = cmd;
		}

		protected override LocalsCtxMenuContext CreateContext() => cmd.Create();
	}

	abstract class LocalsCtxMenuCommand : MenuItemBase<LocalsCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<ITheDebugger> theDebugger;
		protected readonly Lazy<ILocalsContent> localsContent;

		protected LocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent) {
			this.theDebugger = theDebugger;
			this.localsContent = localsContent;
		}

		protected sealed override LocalsCtxMenuContext CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (theDebugger.Value.ProcessState != DebuggerProcessState.Paused)
				return null;
			if (context.CreatorObject.Object != localsContent.Value.ListView)
				return null;
			return Create();
		}

		internal LocalsCtxMenuContext Create() {
			var lv = localsContent.Value.ListView;
			var vm = localsContent.Value.LocalsVM;

			var dict = new Dictionary<object, int>(lv.Items.Count);
			//TODO: This is slow if it contains tons of items since it reads every item
			for (int i = 0; i < lv.Items.Count; i++)
				dict[lv.Items[i]] = i;
			var elems = lv.SelectedItems.OfType<ValueVM>().ToArray();
			Array.Sort(elems, (a, b) => dict[a].CompareTo(dict[b]));

			return new LocalsCtxMenuContext(vm, elems);
		}
	}

	//[Export]
	sealed class ToggleCollapsedLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		[ImportingConstructor]
		ToggleCollapsedLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent)
			: base(theDebugger, localsContent) {
		}

		public override void Execute(LocalsCtxMenuContext context) {
			var vm = GetValueVM(context);
			if (vm != null)
				vm.IsExpanded = !vm.IsExpanded;
		}

		static ValueVM GetValueVM(LocalsCtxMenuContext context) {
			if (context.SelectedItems.Length != 1)
				return null;
			var vm = context.SelectedItems[0];
			if (vm.LazyLoading)
				return vm;
			if (vm.Children.Count > 0)
				return vm;
			return null;
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) => GetValueVM(context) != null;
	}

	//[Export, ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_COPY, Order = 0)]
	sealed class CopyLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		readonly IDebuggerSettings debuggerSettings;

		[ImportingConstructor]
		CopyLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent, IDebuggerSettings debuggerSettings)
			: base(theDebugger, localsContent) {
			this.debuggerSettings = debuggerSettings;
		}

		public override void Execute(LocalsCtxMenuContext context) {
			var output = new StringBuilderTextColorOutput();
			foreach (var vm in context.SelectedItems) {
				//TODO: Break if it takes too long and the user cancels
				var printer = new ValuePrinter(output, debuggerSettings.UseHexadecimal);
				printer.WriteExpander(vm);
				output.Write(BoxedTextColor.Text, "\t");
				// Add an extra here to emulate VS output
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteName(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteValue(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteType(vm);
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

		public override bool IsEnabled(LocalsCtxMenuContext context) => context.SelectedItems.Length > 0;
	}

	//[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_COPY, Order = 10)]
	sealed class SelectAllLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		[ImportingConstructor]
		SelectAllLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent)
			: base(theDebugger, localsContent) {
		}

		public override void Execute(LocalsCtxMenuContext context) => localsContent.Value.ListView.SelectAll();
		public override bool IsEnabled(LocalsCtxMenuContext context) => context.SelectedItems.Length > 0;
	}

	//[Export, ExportMenuItem(Header = "res:LocalsEditValueCommand", InputGestureText = "res:ShortCutKeyF2", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_VALUES, Order = 0)]
	sealed class EditValueLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		[ImportingConstructor]
		EditValueLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent)
			: base(theDebugger, localsContent) {
		}

		public override void Execute(LocalsCtxMenuContext context) {
			if (IsEnabled(context))
				context.SelectedItems[0].IsEditingValue = true;
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) =>
			context.SelectedItems.Length == 1 && context.SelectedItems[0].CanEdit;
	}

	//[Export, ExportMenuItem(Header = "res:LocalsCopyValueCommand", InputGestureText = "res:ShortCutKeyCtrlShiftC", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_VALUES, Order = 10)]
	sealed class CopyValueLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		readonly IDebuggerSettings debuggerSettings;

		[ImportingConstructor]
		CopyValueLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent, IDebuggerSettings debuggerSettings)
			: base(theDebugger, localsContent) {
			this.debuggerSettings = debuggerSettings;
		}

		public override void Execute(LocalsCtxMenuContext context) {
			var output = new StringBuilderTextColorOutput();
			foreach (var vm in context.SelectedItems) {
				//TODO: Break if it takes too long and the user cancels
				var printer = new ValuePrinter(output, debuggerSettings.UseHexadecimal);
				printer.WriteValue(vm);
				if (context.SelectedItems.Length > 1)
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

		public override bool IsEnabled(LocalsCtxMenuContext context) => context.SelectedItems.Length > 0;
	}

	//[ExportMenuItem(Header = "res:LocalsAddWatchCommand", Icon = DsImagesAttribute.Watch, Group = MenuConstants.GROUP_CTX_DBG_LOCALS_VALUES, Order = 20)]
	sealed class AddWatchLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		[ImportingConstructor]
		AddWatchLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent)
			: base(theDebugger, localsContent) {
		}

		public override void Execute(LocalsCtxMenuContext context) {
			//TODO:
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			return false;//TODO:
		}
	}

	//[ExportMenuItem(Header = "res:LocalsSaveCommand", Icon = DsImagesAttribute.Save, Group = MenuConstants.GROUP_CTX_DBG_LOCALS_VALUES, Order = 30)]
	sealed class SaveDataLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		SaveDataLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent, IMessageBoxService messageBoxService)
			: base(theDebugger, localsContent) {
			this.messageBoxService = messageBoxService;
		}

		public override void Execute(LocalsCtxMenuContext context) {
			var value = GetValue(context);
			if (value == null)
				return;

			var filename = new PickSaveFilename().GetFilename(string.Empty, "bin", null);
			if (string.IsNullOrEmpty(filename))
				return;

			byte[] data;
			int? dataIndex = null, dataSize = null;
			if (value.IsString) {
				var s = value.String;
				data = s == null ? null : Encoding.Unicode.GetBytes(s);
			}
			else if (value.IsArray) {
				if (value.ArrayCount == 0)
					data = Array.Empty<byte>();
				else {
					var elemValue = value.GetElementAtPosition(0);
					ulong elemSize = elemValue?.Size ?? 0;
					ulong elemAddr = elemValue?.Address ?? 0;
					ulong addr = value.Address;
					ulong totalSize = elemSize * value.ArrayCount;
					if (elemAddr == 0 || elemAddr < addr || elemAddr - addr > int.MaxValue || totalSize > int.MaxValue) {
						messageBoxService.Show(dnSpy_Debugger_Resources.LocalsSave_Error_CouldNotGetArrayData);
						return;
					}
					data = value.ReadGenericValue();
					dataIndex = (int)(elemAddr - addr);
					dataSize = (int)totalSize;
				}
			}
			else
				data = value.ReadGenericValue();
			if (data == null) {
				messageBoxService.Show(dnSpy_Debugger_Resources.LocalsSave_Error_CouldNotReadAnyData);
				return;
			}

			try {
				if (dataIndex == null)
					dataIndex = 0;
				if (dataSize == null)
					dataSize = data.Length - dataIndex.Value;
				using (var file = File.Create(filename))
					file.Write(data, dataIndex.Value, dataSize.Value);
			}
			catch (Exception ex) {
				messageBoxService.Show(string.Format(dnSpy_Debugger_Resources.LocalsSave_Error_CouldNotSaveDataToFilename, filename, ex.Message));
				return;
			}
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) => GetValue(context) != null;

		internal static CorValue GetValue(LocalsCtxMenuContext context) {
			if (context.SelectedItems.Length != 1)
				return null;
			var nv = context.SelectedItems[0] as NormalValueVM;
			var value = nv?.ReadOnlyCorValue;
			if (value == null)
				return null;
			for (int i = 0; i < 2; i++) {
				if (!value.IsReference)
					break;
				if (value.IsNull)
					return null;
				if (value.ElementType == CorElementType.Ptr || value.ElementType == CorElementType.FnPtr)
					return null;
				value = value.NeuterCheckDereferencedValue;
				if (value == null)
					return null;
			}
			if (value.IsReference)
				return null;
			if (value.IsBox) {
				value = value.BoxedValue;
				if (value == null)
					return null;
			}
			return value;
		}
	}

	static class Constants {
		public const string SHOW_IN_MEMORY_WINDOW_GUID = "10E1F865-8531-486F-86E2-071FB1B9E1B1";
		public const string GROUP_SHOW_IN_MEMORY_WINDOW = "0,CFAF7CC1-2289-436D-8EB6-C5F6E32DE253";
	}

	sealed class SimpleMenuItem : MenuItemBase {
		readonly Action<IMenuItemContext> action;

		public SimpleMenuItem(Action<IMenuItemContext> action) {
			this.action = action;
		}

		public override void Execute(IMenuItemContext context) => action(context);
	}

	//[ExportMenuItem(Header = "res:ShowInMemoryWindowCommand", Icon = DsImagesAttribute.MemoryWindow, Guid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_CTX_DBG_LOCALS_VALUES, Order = 40)]
	sealed class ShowInMemoryXLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		[ImportingConstructor]
		ShowInMemoryXLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent)
			: base(theDebugger, localsContent) {
		}

		public override void Execute(LocalsCtxMenuContext context) { }
	}

	//[ExportMenuItem(OwnerGuid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = Constants.GROUP_SHOW_IN_MEMORY_WINDOW, Order = 0)]
	sealed class ShowInMemoryXLocalsSubCtxMenuCommand : LocalsCtxMenuCommand, IMenuItemProvider {
		readonly Tuple<IMenuItem, string, string>[] subCmds;

		[ImportingConstructor]
		ShowInMemoryXLocalsSubCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent, IMemoryWindowService memoryWindowService)
			: base(theDebugger, localsContent) {
			subCmds = new Tuple<IMenuItem, string, string>[MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS];
			for (int i = 0; i < subCmds.Length; i++)
				subCmds[i] = Tuple.Create((IMenuItem)new ShowInMemoryWindowLocalsCtxMenuCommand(theDebugger, localsContent, memoryWindowService, i), MemoryWindowsHelper.GetHeaderText(i), MemoryWindowsHelper.GetCtrlInputGestureText(i));
		}

		public override void Execute(LocalsCtxMenuContext context) { }

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			var ctx = CreateContext(context);
			Debug.Assert(ctx != null);
			if (ctx == null)
				yield break;

			for (int i = 0; i < subCmds.Length; i++) {
				var info = subCmds[i];
				var attr = new ExportMenuItemAttribute { Header = info.Item2, Icon = DsImagesAttribute.MemoryWindow };
				if (!string.IsNullOrEmpty(info.Item3))
					attr.InputGestureText = info.Item3;
				yield return new CreatedMenuItem(attr, info.Item1);
			}
		}
	}

	//[Export]
	sealed class ShowInMemoryLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		readonly IMemoryWindowService memoryWindowService;

		[ImportingConstructor]
		ShowInMemoryLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent, IMemoryWindowService memoryWindowService)
			: base(theDebugger, localsContent) {
			this.memoryWindowService = memoryWindowService;
		}

		public override void Execute(LocalsCtxMenuContext context) {
			var addrRange = ShowInMemoryWindowLocalsCtxMenuCommand.GetValue(context);
			if (addrRange != null)
				memoryWindowService.Show(addrRange.Value);
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) => ShowInMemoryWindowLocalsCtxMenuCommand.GetValue(context) != null;
	}

	sealed class ShowInMemoryWindowLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		readonly IMemoryWindowService memoryWindowService;
		readonly int windowIndex;

		public ShowInMemoryWindowLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent, IMemoryWindowService memoryWindowService, int windowIndex)
			: base(theDebugger, localsContent) {
			this.memoryWindowService = memoryWindowService;
			this.windowIndex = windowIndex;
		}

		public override void Execute(LocalsCtxMenuContext context) {
			var addrRange = GetValue(context);
			if (addrRange != null)
				memoryWindowService.Show(addrRange.Value, windowIndex);
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) => GetValue(context) != null;

		internal static HexSpan? GetValue(LocalsCtxMenuContext context) {
			var value = SaveDataLocalsCtxMenuCommand.GetValue(context);

			if (value == null)
				return null;

			if (value.IsArray) {
				if (value.ArrayCount == 0)
					return new HexSpan(value.Address, 0);

				var elemValue = value.GetElementAtPosition(0);
				ulong elemSize = elemValue?.Size ?? 0;
				ulong elemAddr = elemValue?.Address ?? 0;
				ulong addr = value.Address;
				ulong totalSize = elemSize * value.ArrayCount;
				if (elemAddr == 0 || elemAddr < addr || elemAddr - addr > int.MaxValue || totalSize > int.MaxValue)
					return null;

				ulong dataIndex = elemAddr - addr;
				return new HexSpan(value.Address + dataIndex, totalSize);
			}

			return new HexSpan(value.Address, value.Size);
		}
	}

	//[ExportMenuItem(Header = "res:HexDisplayCommand", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_HEXOPTS, Order = 0)]
	sealed class HexadecimalDisplayLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		readonly DebuggerSettingsImpl debuggerSettings;

		[ImportingConstructor]
		HexadecimalDisplayLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent, DebuggerSettingsImpl debuggerSettings)
			: base(theDebugger, localsContent) {
			this.debuggerSettings = debuggerSettings;
		}

		public override void Execute(LocalsCtxMenuContext context) => debuggerSettings.UseHexadecimal = !debuggerSettings.UseHexadecimal;
		public override bool IsChecked(LocalsCtxMenuContext context) => debuggerSettings.UseHexadecimal;
	}

	//[ExportMenuItem(Header = "res:LocalsCollapseParentNodeCommand", Icon = DsImagesAttribute.OneLevelUp, Group = MenuConstants.GROUP_CTX_DBG_LOCALS_TREE, Order = 0)]
	sealed class CollapseParentLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		[ImportingConstructor]
		CollapseParentLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent)
			: base(theDebugger, localsContent) {
		}

		public override void Execute(LocalsCtxMenuContext context) {
			var vm = GetLocalParent(context);
			if (vm != null)
				vm.IsExpanded = false;
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			var p = GetLocalParent(context);
			return p != null && p.IsExpanded;
		}

		static ValueVM GetLocalParent(LocalsCtxMenuContext context) {
			if (context.SelectedItems.Length == 0)
				return null;
			return context.SelectedItems[0].Parent as ValueVM;
		}
	}

	//[ExportMenuItem(Header = "res:LocalsExpandChildrenNodesCommand", Icon = DsImagesAttribute.FolderOpened, Group = MenuConstants.GROUP_CTX_DBG_LOCALS_TREE, Order = 10)]
	sealed class ExpandChildrenLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		[ImportingConstructor]
		ExpandChildrenLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent)
			: base(theDebugger, localsContent) {
		}

		public override void Execute(LocalsCtxMenuContext context) {
			var vm = GetLocalParent(context);
			if (vm != null) {
				vm.IsExpanded = true;
				foreach (ValueVM child in vm.Children) {
					if (CanExpand(child))
						child.IsExpanded = true;
				}
			}
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			var p = GetLocalParent(context);
			if (p == null)
				return false;
			if (!p.IsExpanded)
				return true;
			return p.Children.Any(c => CanExpand((ValueVM)c));
		}

		static bool CanExpand(ValueVM vm) => vm != null && !vm.IsExpanded && (vm.LazyLoading || vm.Children.Count > 0);

		static ValueVM GetLocalParent(LocalsCtxMenuContext context) {
			if (context.SelectedItems.Length == 0)
				return null;
			var vm = context.SelectedItems[0];
			if (vm.LazyLoading)
				return vm;
			return vm.Children.Count == 0 ? null : vm;
		}
	}

	//[ExportMenuItem(Header = "res:LocalsCollapseChildrenNodesCommand", Icon = DsImagesAttribute.FolderClosed, Group = MenuConstants.GROUP_CTX_DBG_LOCALS_TREE, Order = 20)]
	sealed class CollapseChildrenLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		[ImportingConstructor]
		CollapseChildrenLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent)
			: base(theDebugger, localsContent) {
		}

		public override void Execute(LocalsCtxMenuContext context) {
			var vm = GetLocalParent(context);
			if (vm != null) {
				foreach (var child in vm.Children)
					child.IsExpanded = false;
			}
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			var p = GetLocalParent(context);
			if (p == null)
				return false;
			if (!p.IsExpanded)
				return false;
			return p.Children.Any(c => c.IsExpanded);
		}

		static ValueVM GetLocalParent(LocalsCtxMenuContext context) {
			if (context.SelectedItems.Length == 0)
				return null;
			var vm = context.SelectedItems[0];
			return vm.Children.Count == 0 ? null : vm;
		}
	}

	//[ExportMenuItem(Header = "res:ShowNamespacesCommand", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_OPTS, Order = 0)]
	sealed class ShowNamespacesLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		readonly LocalsSettingsImpl localsSettings;

		[ImportingConstructor]
		ShowNamespacesLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent, LocalsSettingsImpl localsSettings)
			: base(theDebugger, localsContent) {
			this.localsSettings = localsSettings;
		}

		public override void Execute(LocalsCtxMenuContext context) => localsSettings.ShowNamespaces = !localsSettings.ShowNamespaces;
		public override bool IsChecked(LocalsCtxMenuContext context) => localsSettings.ShowNamespaces;
	}

	//[ExportMenuItem(Header = "res:ShowTypeKeywordsCommand", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_OPTS, Order = 10)]
	sealed class ShowTypeKeywordsLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		readonly LocalsSettingsImpl localsSettings;

		[ImportingConstructor]
		ShowTypeKeywordsLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent, LocalsSettingsImpl localsSettings)
			: base(theDebugger, localsContent) {
			this.localsSettings = localsSettings;
		}

		public override void Execute(LocalsCtxMenuContext context) => localsSettings.ShowTypeKeywords = !localsSettings.ShowTypeKeywords;
		public override bool IsChecked(LocalsCtxMenuContext context) => localsSettings.ShowTypeKeywords;
	}

	//[ExportMenuItem(Header = "res:ShowTokensCommand", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_OPTS, Order = 20)]
	sealed class ShowTokensLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		readonly LocalsSettingsImpl localsSettings;

		[ImportingConstructor]
		ShowTokensLocalsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ILocalsContent> localsContent, LocalsSettingsImpl localsSettings)
			: base(theDebugger, localsContent) {
			this.localsSettings = localsSettings;
		}

		public override void Execute(LocalsCtxMenuContext context) => localsSettings.ShowTokens = !localsSettings.ShowTokens;
		public override bool IsChecked(LocalsCtxMenuContext context) => localsSettings.ShowTokens;
	}
}
