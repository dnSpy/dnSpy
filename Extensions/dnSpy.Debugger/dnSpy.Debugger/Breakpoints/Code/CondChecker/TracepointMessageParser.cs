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
using System.Diagnostics;
using System.Text;

namespace dnSpy.Debugger.Breakpoints.Code.CondChecker {
	sealed class TracepointMessageParser {
		// Max methods to show when $CALLSTACK is used (no argument)
		const int defaultCallStackCount = 50;

		readonly struct KeywordInfo {
			public string Name { get; }
			public TracepointMessageKind Kind { get; }
			public int Number { get; }
			public KeywordInfo(string name, TracepointMessageKind kind) {
				Name = name ?? throw new ArgumentNullException(nameof(name));
				Kind = kind;
				Number = -1;
			}
			public KeywordInfo(string name, TracepointMessageKind kind, int number) {
				Name = name ?? throw new ArgumentNullException(nameof(name));
				Kind = kind;
				Number = number;
			}
		}
		readonly KeywordInfo[] keywords;

		readonly List<TracepointMessagePart> partBuilder;
		readonly StringBuilder currentText;
		readonly StringBuilder tempStringBuilder;
		int currentTextLength;

		public TracepointMessageParser() {
			partBuilder = new List<TracepointMessagePart>();
			currentText = new StringBuilder();
			tempStringBuilder = new StringBuilder();
			currentTextLength = 0;
			keywords = new KeywordInfo[] {
				// NOTE: order is important, first match is picked
				// NOTE: When updating this table, also update the help message created by ShowCodeBreakpointSettingsVM
				new KeywordInfo("ADDRESS5", TracepointMessageKind.WriteAddress, 5),
				new KeywordInfo("ADDRESS4", TracepointMessageKind.WriteAddress, 4),
				new KeywordInfo("ADDRESS3", TracepointMessageKind.WriteAddress, 3),
				new KeywordInfo("ADDRESS2", TracepointMessageKind.WriteAddress, 2),
				new KeywordInfo("ADDRESS1", TracepointMessageKind.WriteAddress, 1),
				new KeywordInfo("ADDRESS", TracepointMessageKind.WriteAddress, 0),
				new KeywordInfo("ADID", TracepointMessageKind.WriteAppDomainId),
				new KeywordInfo("BPADDR", TracepointMessageKind.WriteBreakpointAddress),
				new KeywordInfo("CALLERMODULE5", TracepointMessageKind.WriteCallerModule, 5),
				new KeywordInfo("CALLERMODULE4", TracepointMessageKind.WriteCallerModule, 4),
				new KeywordInfo("CALLERMODULE3", TracepointMessageKind.WriteCallerModule, 3),
				new KeywordInfo("CALLERMODULE2", TracepointMessageKind.WriteCallerModule, 2),
				new KeywordInfo("CALLERMODULE1", TracepointMessageKind.WriteCallerModule, 1),
				new KeywordInfo("CALLERMODULE", TracepointMessageKind.WriteCallerModule, 1),
				new KeywordInfo("CALLEROFFSET5", TracepointMessageKind.WriteCallerOffset, 5),
				new KeywordInfo("CALLEROFFSET4", TracepointMessageKind.WriteCallerOffset, 4),
				new KeywordInfo("CALLEROFFSET3", TracepointMessageKind.WriteCallerOffset, 3),
				new KeywordInfo("CALLEROFFSET2", TracepointMessageKind.WriteCallerOffset, 2),
				new KeywordInfo("CALLEROFFSET1", TracepointMessageKind.WriteCallerOffset, 1),
				new KeywordInfo("CALLEROFFSET", TracepointMessageKind.WriteCallerOffset, 1),
				new KeywordInfo("CALLERTOKEN5", TracepointMessageKind.WriteCallerToken, 5),
				new KeywordInfo("CALLERTOKEN4", TracepointMessageKind.WriteCallerToken, 4),
				new KeywordInfo("CALLERTOKEN3", TracepointMessageKind.WriteCallerToken, 3),
				new KeywordInfo("CALLERTOKEN2", TracepointMessageKind.WriteCallerToken, 2),
				new KeywordInfo("CALLERTOKEN1", TracepointMessageKind.WriteCallerToken, 1),
				new KeywordInfo("CALLERTOKEN", TracepointMessageKind.WriteCallerToken, 1),
				new KeywordInfo("CALLSTACK20", TracepointMessageKind.WriteCallStack, 20),
				new KeywordInfo("CALLSTACK15", TracepointMessageKind.WriteCallStack, 15),
				new KeywordInfo("CALLSTACK10", TracepointMessageKind.WriteCallStack, 10),
				new KeywordInfo("CALLSTACK5", TracepointMessageKind.WriteCallStack, 5),
				new KeywordInfo("CALLSTACK", TracepointMessageKind.WriteCallStack, defaultCallStackCount),
				new KeywordInfo("CALLER5", TracepointMessageKind.WriteCaller, 5),
				new KeywordInfo("CALLER4", TracepointMessageKind.WriteCaller, 4),
				new KeywordInfo("CALLER3", TracepointMessageKind.WriteCaller, 3),
				new KeywordInfo("CALLER2", TracepointMessageKind.WriteCaller, 2),
				new KeywordInfo("CALLER1", TracepointMessageKind.WriteCaller, 1),
				new KeywordInfo("CALLER", TracepointMessageKind.WriteCaller, 1),
				new KeywordInfo("FUNCTION5", TracepointMessageKind.WriteFunction, 5),
				new KeywordInfo("FUNCTION4", TracepointMessageKind.WriteFunction, 4),
				new KeywordInfo("FUNCTION3", TracepointMessageKind.WriteFunction, 3),
				new KeywordInfo("FUNCTION2", TracepointMessageKind.WriteFunction, 2),
				new KeywordInfo("FUNCTION1", TracepointMessageKind.WriteFunction, 1),
				new KeywordInfo("FUNCTION", TracepointMessageKind.WriteFunction, 0),
				new KeywordInfo("MID", TracepointMessageKind.WriteManagedId),
				new KeywordInfo("PID", TracepointMessageKind.WriteProcessId),
				new KeywordInfo("PNAME", TracepointMessageKind.WriteProcessName),
				new KeywordInfo("TID", TracepointMessageKind.WriteThreadId),
				new KeywordInfo("TNAME", TracepointMessageKind.WriteThreadName),
			};
		}

		public ParsedTracepointMessage Parse(string text) {
			try {
				Debug.Assert(partBuilder.Count == 0);
				ParseCore(text);
				return new ParsedTracepointMessage(partBuilder.ToArray());
			}
			finally {
				Debug.Assert(currentText.Length == 0);
				Debug.Assert(currentTextLength == 0);
				partBuilder.Clear();
				currentText.Clear();
				currentTextLength = 0;
				tempStringBuilder.Clear();
			}
		}

		void ParseCore(string text) {
			int textPos = 0;
			while (textPos < text.Length) {
				int index = text.IndexOfAny(specialChars, textPos);
				if (index < 0) {
					AddText(text, textPos, text.Length - textPos);
					textPos = text.Length;
					break;
				}

				AddText(text, textPos, index - textPos);
				textPos = index;
				switch (text[textPos]) {
				case '\\':
					if (textPos + 1 < text.Length) {
						var c = text[textPos + 1];
						if (Array.IndexOf(specialChars, c) >= 0) {
							AddEscapeChar(c);
							textPos += 2;
							break;
						}
						else {
							bool ok = true;
							switch (c) {
							case 'a': AddEscapeChar('\a'); break;
							case 'b': AddEscapeChar('\b'); break;
							case 'f': AddEscapeChar('\f'); break;
							case 'n': AddEscapeChar('\n'); break;
							case 'r': AddEscapeChar('\r'); break;
							case 't': AddEscapeChar('\t'); break;
							case 'v': AddEscapeChar('\v'); break;
							// If you add more cases, update help message in ShowCodeBreakpointSettingsVM
							default:
								ok = false;
								break;
							}
							if (ok) {
								textPos += 2;
								break;
							}
						}
					}
					goto default;

				case '$':
					bool foundKeyword = false;
					foreach (var info in keywords) {
						if (StartsWith(text, textPos + 1, info.Name)) {
							textPos += 1 + info.Name.Length;
							FlushPendingText();
							if (info.Number != -1)
								partBuilder.Add(new TracepointMessagePart(info.Kind, info.Number, info.Name.Length + 1));
							else
								partBuilder.Add(new TracepointMessagePart(info.Kind, null, info.Name.Length + 1));
							foundKeyword = true;
							break;
						}
					}
					if (foundKeyword)
						break;
					goto default;

				case '{':
					int pos = textPos + 1;
					var expression = ReadEvalText(text, ref pos);
					FlushPendingText();
					partBuilder.Add(new TracepointMessagePart(TracepointMessageKind.WriteEvaluatedExpression, expression, pos - textPos));
					textPos = pos;
					break;

				default:
					AddText(text[textPos++]);
					break;
				}
			}
			Debug.Assert(textPos == text.Length);
			FlushPendingText();
		}
		// If you add more chars, update help message in ShowCodeBreakpointSettingsVM
		static char[] specialChars = new char[] { '\\', '$', '{', '}' };

		static bool StartsWith(string s, int index, string other) {
			if (index + other.Length > s.Length)
				return false;
			for (int i = 0; i < other.Length; i++) {
				if (s[index + i] != other[i])
					return false;
			}
			return true;
		}

		string ReadEvalText(string s, ref int pos) {
			var sb = tempStringBuilder;
			sb.Clear();
			int braceCount = 1;
			while (pos < s.Length) {
				var c = s[pos++];
				if (c == '}') {
					if (braceCount <= 1)
						break;
					braceCount--;
				}
				else if (c == '{')
					braceCount++;
				sb.Append(c);
			}
			return sb.ToString();
		}

		void AddText(string text, int startIndex, int count) {
			currentText.Append(text, startIndex, count);
			currentTextLength += count;
		}

		void AddText(char c) {
			currentText.Append(c);
			currentTextLength++;
		}

		void AddEscapeChar(char c) {
			currentText.Append(c);
			currentTextLength += 2;
		}

		void FlushPendingText() {
			if (currentText.Length > 0) {
				Debug.Assert(partBuilder.Count == 0 || partBuilder[partBuilder.Count - 1].Kind != TracepointMessageKind.WriteText);
				partBuilder.Add(new TracepointMessagePart(TracepointMessageKind.WriteText, currentText.ToString(), currentTextLength));
				currentText.Clear();
				currentTextLength = 0;
			}
		}
	}
}
