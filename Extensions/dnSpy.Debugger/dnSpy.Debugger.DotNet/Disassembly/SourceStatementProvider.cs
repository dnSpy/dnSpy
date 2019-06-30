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
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Debugger.DotNet.Disassembly {
	struct SourceStatementProvider {
		readonly string text;
		readonly MethodDebugInfo debugInfo;
		SourceStatement? lastStatement;

		public SourceStatementProvider(string text, MethodDebugInfo debugInfo) {
			this.text = text ?? throw new ArgumentNullException(nameof(text));
			this.debugInfo = debugInfo ?? throw new ArgumentNullException(nameof(debugInfo));
			lastStatement = null;
		}

		public (string line, TextSpan span) GetStatement(int ilOffset) {
			var text = this.text;
			var debugInfo = this.debugInfo;
			if (text is null || debugInfo is null)
				return default;

			var lastStatement = this.lastStatement;
			var stmt = debugInfo.GetSourceStatementByCodeOffset((uint)ilOffset);
			this.lastStatement = stmt;
			if (stmt is null)
				return default;
			if (lastStatement == stmt)
				return default;
			return GetStatement(stmt.Value.TextSpan, text);
		}

		internal static (string line, TextSpan span) GetStatement(TextSpan span, string text) {
			int startPos = span.Start;
			while (startPos > 0 && text[startPos - 1] != '\n')
				startPos--;
			var endPos = span.End;
			while (endPos < text.Length) {
				var c = text[endPos];
				if (c == '\r' || c == '\n')
					break;
				endPos++;
			}
			var line = text.Substring(startPos, endPos - startPos);
			return (line, new TextSpan(span.Start - startPos, span.Length));
		}
	}
}
