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
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Debugger.DotNet.Code;

namespace dnSpy.Debugger.DotNet.Breakpoints.Code {
	[ExportDbgBreakpointLocationFormatterProvider(PredefinedDbgCodeLocationTypes.DotNet)]
	sealed class DbgBreakpointLocationFormatterProviderImpl : DbgBreakpointLocationFormatterProvider {
		readonly Lazy<BreakpointFormatterService> breakpointFormatterService;

		[ImportingConstructor]
		DbgBreakpointLocationFormatterProviderImpl(Lazy<BreakpointFormatterService> breakpointFormatterService) =>
			this.breakpointFormatterService = breakpointFormatterService;

		public override DbgBreakpointLocationFormatter? Create(DbgCodeLocation location) {
			switch (location) {
			case DbgDotNetCodeLocationImpl loc:
				var formatter = loc.Formatter;
				if (formatter is not null)
					return formatter;
				formatter = breakpointFormatterService.Value.Create(loc);
				loc.Formatter = formatter;
				return formatter;
			}
			return null;
		}
	}
}
