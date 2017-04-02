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

using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Breakpoints.Code.TextEditor;
using dnSpy.Contracts.Debugger.DotNet.Breakpoints.Code;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Debugger.DotNet.Breakpoints.Code.TextEditor {
	[ExportBreakpointModuleLocationProvider]
	sealed class BreakpointModuleLocationProviderImpl : BreakpointModuleLocationProvider {
		public override GlyphTextMarkerLocationInfo GetLocation(DbgCodeBreakpoint breakpoint) {
			if (breakpoint.EngineBreakpoint is DbgDotNetEngineCodeBreakpoint dnbp)
				return new GlyphTextMethodMarkerLocationInfo(new ModuleTokenId(dnbp.Module, dnbp.Token), dnbp.Offset);
			return null;
		}
	}
}
