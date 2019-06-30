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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Breakpoints.Code.Dialogs;

namespace dnSpy.Debugger.Breakpoints.Code.TextEditor {
	abstract class GlyphMarginOperations {
		public abstract void Remove(DbgCodeBreakpoint breakpoint);
		public abstract void Toggle(DbgCodeBreakpoint breakpoint);
		public abstract void EditSettings(DbgCodeBreakpoint breakpoint);
		public abstract void Export(DbgCodeBreakpoint breakpoint);
	}

	[Export(typeof(GlyphMarginOperations))]
	sealed class GlyphMarginOperationsImpl : GlyphMarginOperations {
		readonly Lazy<ShowCodeBreakpointSettingsService> showCodeBreakpointSettingsService;
		readonly Lazy<DbgCodeBreakpointSerializerService> dbgCodeBreakpointSerializerService;

		[ImportingConstructor]
		GlyphMarginOperationsImpl(Lazy<ShowCodeBreakpointSettingsService> showCodeBreakpointSettingsService, Lazy<DbgCodeBreakpointSerializerService> dbgCodeBreakpointSerializerService) {
			this.showCodeBreakpointSettingsService = showCodeBreakpointSettingsService;
			this.dbgCodeBreakpointSerializerService = dbgCodeBreakpointSerializerService;
		}

		public override void Remove(DbgCodeBreakpoint breakpoint) => breakpoint.Remove();
		public override void Toggle(DbgCodeBreakpoint breakpoint) => breakpoint.IsEnabled = !breakpoint.IsEnabled;
		public override void EditSettings(DbgCodeBreakpoint breakpoint) => showCodeBreakpointSettingsService.Value.Edit(breakpoint);
		public override void Export(DbgCodeBreakpoint breakpoint) => dbgCodeBreakpointSerializerService.Value.Save(new[] { breakpoint });
	}
}
