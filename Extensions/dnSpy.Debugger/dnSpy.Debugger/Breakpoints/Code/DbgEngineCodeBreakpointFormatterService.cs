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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Debugger.Breakpoints.Code {
	abstract class DbgEngineCodeBreakpointFormatterService {
		public abstract DbgEngineCodeBreakpointFormatter GetFormatter(DbgEngineCodeBreakpoint breakpoint);
	}

	[Export(typeof(DbgEngineCodeBreakpointFormatterService))]
	sealed class DbgEngineCodeBreakpointFormatterServiceImpl : DbgEngineCodeBreakpointFormatterService {
		readonly Lazy<DbgEngineCodeBreakpointFormatterProvider, IDbgEngineCodeBreakpointFormatterProviderMetadata>[] dbgEngineCodeBreakpointFormatterProviders;

		[ImportingConstructor]
		DbgEngineCodeBreakpointFormatterServiceImpl([ImportMany] IEnumerable<Lazy<DbgEngineCodeBreakpointFormatterProvider, IDbgEngineCodeBreakpointFormatterProviderMetadata>> dbgEngineCodeBreakpointFormatterProviders) =>
			this.dbgEngineCodeBreakpointFormatterProviders = dbgEngineCodeBreakpointFormatterProviders.ToArray();

		public override DbgEngineCodeBreakpointFormatter GetFormatter(DbgEngineCodeBreakpoint breakpoint) {
			if (breakpoint == null)
				throw new ArgumentNullException(nameof(breakpoint));
			var type = breakpoint.Type;
			foreach (var lz in dbgEngineCodeBreakpointFormatterProviders) {
				if (Array.IndexOf(lz.Metadata.Types, type) >= 0) {
					var formatter = lz.Value.Create(breakpoint);
					if (formatter != null)
						return formatter;
				}
			}
			return NullDbgEngineCodeBreakpointFormatter.Instance;
		}
	}

	sealed class NullDbgEngineCodeBreakpointFormatter : DbgEngineCodeBreakpointFormatter {
		public static readonly NullDbgEngineCodeBreakpointFormatter Instance = new NullDbgEngineCodeBreakpointFormatter();
		NullDbgEngineCodeBreakpointFormatter() { }
		public override void WriteName(IDebugOutputWriter output) { }
		public override void WriteModule(IDebugOutputWriter output) { }
	}
}
