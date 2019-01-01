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
using System.Diagnostics;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Debugger.DotNet.Disassembly {
	struct ILSourceStatementProvider {
		readonly string text;
		readonly MethodDebugInfo debugInfo;

		public bool IsDefault => text == null;

		public ILSourceStatementProvider(string text, MethodDebugInfo debugInfo) {
			this.text = text ?? throw new ArgumentNullException(nameof(text));
			this.debugInfo = debugInfo ?? throw new ArgumentNullException(nameof(debugInfo));
		}

		public (string line, TextSpan span) GetStatement(int ilOffset, int endILOffset) {
			Debug.Assert(ilOffset <= endILOffset);
			var text = this.text;
			var debugInfo = this.debugInfo;
			if (text == null || debugInfo == null)
				return default;

			var stmt = debugInfo.GetSourceStatementByCodeOffset((uint)ilOffset);
			SourceStatement? stmtEnd;
			if (endILOffset == int.MaxValue) {
				if (debugInfo.Statements.Length == 0)
					stmtEnd = null;
				else
					stmtEnd = debugInfo.Statements[debugInfo.Statements.Length - 1];
			}
			else if (ilOffset < endILOffset)
				stmtEnd = debugInfo.GetSourceStatementByCodeOffset((uint)endILOffset - 1);
			else if (ilOffset == endILOffset)
				stmtEnd = stmt;
			else
				stmtEnd = null;
			if (stmt == null || stmtEnd == null)
				return default;
			Debug.Assert(stmt.Value.TextSpan.Start <= stmtEnd.Value.TextSpan.End);
			if (stmt.Value.TextSpan.Start > stmtEnd.Value.TextSpan.End)
				return default;
			var fullSpan = TextSpan.FromBounds(stmt.Value.TextSpan.Start, stmtEnd.Value.TextSpan.End);
			return SourceStatementProvider.GetStatement(fullSpan, text);
		}
	}
}
