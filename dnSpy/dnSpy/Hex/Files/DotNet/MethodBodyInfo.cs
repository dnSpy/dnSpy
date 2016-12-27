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

using System;
using System.Collections.Generic;
using dnSpy.Contracts.Hex;

namespace dnSpy.Hex.Files.DotNet {
	[Flags]
	enum MethodBodyInfoFlags {
		None							= 0,
		SmallExceptionClauses			= 0x00000001,
		Invalid							= 0x00000002,
	}

	struct MethodBodyInfo {
		public IList<uint> Tokens { get; }
		public HexSpan HeaderSpan { get; }
		public HexSpan InstructionsSpan { get; }
		public HexSpan ExceptionsSpan { get; }
		public MethodBodyInfoFlags Flags { get; }
		public bool IsSmallExceptionClauses => (Flags & MethodBodyInfoFlags.SmallExceptionClauses) != 0;
		public bool IsInvalid => (Flags & MethodBodyInfoFlags.Invalid) != 0;

		public HexSpan Span {
			get {
				HexPosition end;
				if (ExceptionsSpan.Length != 0)
					end = ExceptionsSpan.End;
				else if (InstructionsSpan.Length != 0)
					end = InstructionsSpan.End;
				else
					end = HeaderSpan.End;
				return HexSpan.FromBounds(HeaderSpan.Start, end);
			}
		}

		public MethodBodyInfo(IList<uint> tokens, HexSpan headerSpan, HexSpan instructionsSpan, HexSpan exceptionsSpan, MethodBodyInfoFlags flags) {
			Tokens = tokens;
			HeaderSpan = headerSpan;
			InstructionsSpan = instructionsSpan;
			ExceptionsSpan = exceptionsSpan;
			Flags = flags;
		}
	}
}
