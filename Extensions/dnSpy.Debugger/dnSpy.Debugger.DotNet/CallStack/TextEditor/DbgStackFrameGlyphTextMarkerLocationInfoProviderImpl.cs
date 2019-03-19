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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.CallStack.TextEditor;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Debugger.DotNet.CallStack.TextEditor {
	[Export(typeof(DbgStackFrameGlyphTextMarkerLocationInfoProvider))]
	sealed class DbgStackFrameGlyphTextMarkerLocationInfoProviderImpl : DbgStackFrameGlyphTextMarkerLocationInfoProvider {
		public override GlyphTextMarkerLocationInfo Create(DbgStackFrame frame) {
			switch (frame.Location) {
			case DbgDotNetCodeLocation loc:
				switch (loc.ILOffsetMapping) {
				case DbgILOffsetMapping.Exact:
				case DbgILOffsetMapping.Approximate:
					return new DotNetMethodBodyGlyphTextMarkerLocationInfo(loc.Module, loc.Token, loc.Offset);

				case DbgILOffsetMapping.Prolog:
				case DbgILOffsetMapping.Epilog:
				case DbgILOffsetMapping.Unknown:
				case DbgILOffsetMapping.NoInfo:
				case DbgILOffsetMapping.UnmappedAddress:
				default:
					return new DotNetTokenGlyphTextMarkerLocationInfo(loc.Module, loc.Token);
				}

			default:
				return null;
			}
		}
	}
}
