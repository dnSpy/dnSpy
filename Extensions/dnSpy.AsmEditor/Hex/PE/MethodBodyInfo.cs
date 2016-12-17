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

using dnlib.DotNet;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex.PE {
	struct MethodBodyInfo {
		public MDToken Token { get; }
		public HexSpan HeaderSpan { get; }
		public HexSpan InstructionsSpan { get; }
		public HexSpan ExceptionsSpan { get; }
		public bool IsSmallExceptionClauses { get; }
		public HexSpan Span => HexSpan.FromBounds(HeaderSpan.Start, ExceptionsSpan.Length == 0 ? InstructionsSpan.End : ExceptionsSpan.End);

		public MethodBodyInfo(MDToken token, HexSpan headerSpan, HexSpan instructionsSpan, HexSpan exceptionsSpan, bool isSmallExceptionClauses) {
			Token = token;
			HeaderSpan = headerSpan;
			InstructionsSpan = instructionsSpan;
			ExceptionsSpan = exceptionsSpan;
			IsSmallExceptionClauses = isSmallExceptionClauses;
		}
	}
}
