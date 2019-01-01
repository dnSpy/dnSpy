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
using System.Linq;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Debugger.Breakpoints.Code {
	abstract class DbgBreakpointLocationFormatterService {
		public abstract DbgBreakpointLocationFormatter GetFormatter(DbgCodeLocation location);
	}

	[Export(typeof(DbgBreakpointLocationFormatterService))]
	sealed class DbgBreakpointLocationFormatterServiceImpl : DbgBreakpointLocationFormatterService {
		readonly Lazy<DbgBreakpointLocationFormatterProvider, IDbgBreakpointLocationFormatterProviderMetadata>[] dbgBreakpointLocationFormatterProviders;

		[ImportingConstructor]
		DbgBreakpointLocationFormatterServiceImpl([ImportMany] IEnumerable<Lazy<DbgBreakpointLocationFormatterProvider, IDbgBreakpointLocationFormatterProviderMetadata>> dbgBreakpointLocationFormatterProviders) =>
			this.dbgBreakpointLocationFormatterProviders = dbgBreakpointLocationFormatterProviders.ToArray();

		public override DbgBreakpointLocationFormatter GetFormatter(DbgCodeLocation location) {
			if (location == null)
				throw new ArgumentNullException(nameof(location));
			var type = location.Type;
			foreach (var lz in dbgBreakpointLocationFormatterProviders) {
				if (Array.IndexOf(lz.Metadata.Types, type) >= 0) {
					var formatter = lz.Value.Create(location);
					if (formatter != null)
						return formatter;
				}
			}
			return NullDbgBreakpointLocationFormatter.Instance;
		}
	}

	sealed class NullDbgBreakpointLocationFormatter : DbgBreakpointLocationFormatter {
		public static readonly NullDbgBreakpointLocationFormatter Instance = new NullDbgBreakpointLocationFormatter();
		NullDbgBreakpointLocationFormatter() { }
		public override void WriteName(IDbgTextWriter output, DbgBreakpointLocationFormatterOptions options) { }
		public override void WriteModule(IDbgTextWriter output) { }
		public override void Dispose() { }
	}
}
