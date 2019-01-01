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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Disassembly;
using dnSpy.Contracts.Disassembly.Viewer;

namespace dnSpy.Debugger.Disassembly {
	abstract class DbgShowNativeCodeService {
		public abstract bool CanShowNativeCode(DbgStackFrame frame);
		public abstract bool ShowNativeCode(DbgStackFrame frame, string title = null);
		public abstract bool CanShowNativeCode(DbgBoundCodeBreakpoint boundBreakpoint);
		public abstract bool ShowNativeCode(DbgBoundCodeBreakpoint boundBreakpoint, string title = null);
		public abstract bool CanShowNativeCode(DbgRuntime runtime, DbgCodeLocation location);
		public abstract bool ShowNativeCode(DbgRuntime runtime, DbgCodeLocation location, string title = null);
	}

	[Export(typeof(DbgShowNativeCodeService))]
	sealed class DbgShowNativeCodeServiceImpl : DbgShowNativeCodeService {
		readonly Lazy<DbgNativeCodeProvider> dbgNativeCodeProvider;
		readonly Lazy<DisassemblyContentSettings> disassemblyContentSettings;
		readonly Lazy<DisassemblyContentProviderFactory> disassemblyContentProviderFactory;
		readonly Lazy<DisassemblyViewerService> disassemblyViewerService;

		[ImportingConstructor]
		DbgShowNativeCodeServiceImpl(Lazy<DbgNativeCodeProvider> dbgNativeCodeProvider, Lazy<DisassemblyContentSettings> disassemblyContentSettings, Lazy<DisassemblyContentProviderFactory> disassemblyContentProviderFactory, Lazy<DisassemblyViewerService> disassemblyViewerService) {
			this.dbgNativeCodeProvider = dbgNativeCodeProvider;
			this.disassemblyContentSettings = disassemblyContentSettings;
			this.disassemblyContentProviderFactory = disassemblyContentProviderFactory;
			this.disassemblyViewerService = disassemblyViewerService;
		}

		DbgNativeCodeOptions GetNativeCodeOptions() {
			var options = DbgNativeCodeOptions.None;
			if (disassemblyContentSettings.Value.ShowILCode)
				options |= DbgNativeCodeOptions.ShowILCode;
			if (disassemblyContentSettings.Value.ShowCode)
				options |= DbgNativeCodeOptions.ShowCode;
			return options;
		}

		DisassemblyContentFormatterOptions GetDisassemblyContentFormatterOptions() => DisassemblyContentFormatterOptions.None;

		void Show(GetNativeCodeResult result, string title) {
			var content = disassemblyContentProviderFactory.Value.Create(result.Code, GetDisassemblyContentFormatterOptions(), result.SymbolResolver, result.Header);
			disassemblyViewerService.Value.Show(content, title);
		}

		public override bool CanShowNativeCode(DbgStackFrame frame) =>
			dbgNativeCodeProvider.Value.CanGetNativeCode(frame);

		public override bool ShowNativeCode(DbgStackFrame frame, string title) {
			if (!dbgNativeCodeProvider.Value.TryGetNativeCode(frame, GetNativeCodeOptions(), out var result))
				return false;
			Show(result, title);
			return true;
		}

		public override bool CanShowNativeCode(DbgBoundCodeBreakpoint boundBreakpoint) =>
			dbgNativeCodeProvider.Value.CanGetNativeCode(boundBreakpoint);

		public override bool ShowNativeCode(DbgBoundCodeBreakpoint boundBreakpoint, string title) {
			if (!dbgNativeCodeProvider.Value.TryGetNativeCode(boundBreakpoint, GetNativeCodeOptions(), out var result))
				return false;
			Show(result, title);
			return true;
		}

		public override bool CanShowNativeCode(DbgRuntime runtime, DbgCodeLocation location) =>
			dbgNativeCodeProvider.Value.CanGetNativeCode(runtime, location);

		public override bool ShowNativeCode(DbgRuntime runtime, DbgCodeLocation location, string title) {
			if (!dbgNativeCodeProvider.Value.TryGetNativeCode(runtime, location, GetNativeCodeOptions(), out var result))
				return false;
			Show(result, title);
			return true;
		}
	}
}
