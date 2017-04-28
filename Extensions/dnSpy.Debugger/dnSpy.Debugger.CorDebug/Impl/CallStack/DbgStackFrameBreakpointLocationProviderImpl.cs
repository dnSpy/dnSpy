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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Debugger.CorDebug.Breakpoints;

namespace dnSpy.Debugger.CorDebug.Impl.CallStack {
	[ExportDbgStackFrameBreakpointLocationProvider]
	sealed class DbgStackFrameBreakpointLocationProviderImpl : DbgStackFrameBreakpointLocationProvider {
		readonly Lazy<DbgDotNetNativeBreakpointLocationFactory> dbgDotNetNativeBreakpointLocationFactory;

		[ImportingConstructor]
		DbgStackFrameBreakpointLocationProviderImpl(Lazy<DbgDotNetNativeBreakpointLocationFactory> dbgDotNetNativeBreakpointLocationFactory) =>
			this.dbgDotNetNativeBreakpointLocationFactory = dbgDotNetNativeBreakpointLocationFactory;

		public override DbgBreakpointLocation Create(DbgStackFrameLocation location) {
			switch (location) {
			case DbgDotNetStackFrameLocationImpl frameLoc:
				return dbgDotNetNativeBreakpointLocationFactory.Value.Create(frameLoc.Module, frameLoc.Token, frameLoc.ILOffset, frameLoc.ILOffsetMapping, frameLoc.NativeMethodAddress, frameLoc.NativeMethodOffset, frameLoc.CorCode);
			}
			return null;
		}
	}
}
