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
using System.ComponentModel.Composition;
using dndbg.Engine;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.DotNet.CorDebug.Breakpoints;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.CorDebug.Impl;

namespace dnSpy.Debugger.CorDebug.Breakpoints {
	abstract class DbgDotNetNativeBreakpointLocationFactory {
		public abstract DbgDotNetNativeBreakpointLocation Create(ModuleId module, uint token, uint ilOffset, DbgILOffsetMapping ilOffsetMapping, ulong nativeMethodAddress, uint nativeMethodOffset, DnDebuggerObjectHolder<CorCode> corCode);
	}

	[Export(typeof(DbgDotNetNativeBreakpointLocationFactory))]
	sealed class DbgDotNetNativeBreakpointLocationFactoryImpl : DbgDotNetNativeBreakpointLocationFactory {
		readonly BreakpointFormatterService breakpointFormatterService;

		[ImportingConstructor]
		DbgDotNetNativeBreakpointLocationFactoryImpl(BreakpointFormatterService breakpointFormatterService) =>
			this.breakpointFormatterService = breakpointFormatterService;

		public override DbgDotNetNativeBreakpointLocation Create(ModuleId module, uint token, uint ilOffset, DbgILOffsetMapping ilOffsetMapping, ulong nativeMethodAddress, uint nativeMethodOffset, DnDebuggerObjectHolder<CorCode> corCode) {
			if (corCode == null)
				throw new ArgumentNullException(nameof(corCode));
			var dnbp = new DbgDotNetNativeBreakpointLocationImpl(module, token, ilOffset, ilOffsetMapping, nativeMethodAddress, nativeMethodOffset, corCode);
			var formatter = breakpointFormatterService.Create(dnbp);
			dnbp.Formatter = formatter;
			return dnbp;
		}
	}
}
