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
using System.Diagnostics;
using System.Text;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;

namespace dnSpy.Debugger.Breakpoints.Code.CondChecker {
	abstract class TracepointMessageCreator {
		public abstract string Create(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread, DbgCodeBreakpointTrace trace);
	}

	[Export(typeof(TracepointMessageCreator))]
	sealed class TracepointMessageCreatorImpl : TracepointMessageCreator {
		StringBuilder output;

		DbgBoundCodeBreakpoint boundBreakpoint;
		DbgThread thread;

		struct KeywordInfo {
			public string Name { get; }
			public Action Handler { get; }
			public KeywordInfo(string name, Action handler) {
				Name = name ?? throw new ArgumentNullException(nameof(name));
				Handler = handler ?? throw new ArgumentNullException(nameof(handler));
			}
		}
		readonly KeywordInfo[] keywords;

		[ImportingConstructor]
		TracepointMessageCreatorImpl() {
			output = new StringBuilder();
			keywords = new KeywordInfo[] {
				new KeywordInfo("ADDRESS", WriteAddress),
				new KeywordInfo("CALLER", WriteCaller),
				new KeywordInfo("CALLSTACK", WriteCallStack),
				new KeywordInfo("FUNCTION", WriteFunction),
				new KeywordInfo("PID", WriteProcessId),
				new KeywordInfo("PNAME", WriteProcessName),
				new KeywordInfo("TID", WriteThreadId),
				new KeywordInfo("TNAME", WriteThreadName),
			};
		}

		public override string Create(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread, DbgCodeBreakpointTrace trace) {
			if (boundBreakpoint == null)
				throw new ArgumentNullException(nameof(boundBreakpoint));
			if (trace.Message == null)
				return string.Empty;
			try {
				output.Clear();
				this.boundBreakpoint = boundBreakpoint;
				this.thread = thread;
				CreateCore(trace.Message);
				return output.ToString();
			}
			finally {
				if (output.Capacity >= 1024)
					output = new StringBuilder();
				this.boundBreakpoint = null;
				this.thread = null;
			}
		}

		void CreateCore(string text) {
			int textPos = 0;
			while (textPos < text.Length) {
				int index = text.IndexOfAny(specialChars, textPos);
				if (index < 0) {
					Write(text.Substring(textPos));
					textPos = text.Length;
					break;
				}

				Write(text.Substring(textPos, index - textPos));
				textPos = index;
				switch (text[textPos]) {
				case '\\':
					if (textPos + 1 < text.Length) {
						var c = text[textPos + 1];
						if (Array.IndexOf(specialChars, c) >= 0) {
							Write(c);
							textPos += 2;
							break;
						}
					}
					goto default;

				case '$':
					bool foundKeyword = false;
					foreach (var info in keywords) {
						if (StartsWith(text, textPos + 1, info.Name)) {
							textPos += 1 + info.Name.Length;
							info.Handler();
							foundKeyword = true;
							break;
						}
					}
					if (foundKeyword)
						break;
					goto default;

				case '{':
					int exprEndIndex = text.IndexOf('}', textPos + 1);
					if (exprEndIndex >= 0) {
						WriteExpressionValue(text.Substring(textPos + 1, exprEndIndex - textPos - 1));
						textPos = exprEndIndex + 1;
						break;
					}
					goto default;

				default:
					Write(text[textPos++]);
					break;
				}
			}
			Debug.Assert(textPos == text.Length);
		}
		static char[] specialChars = new char[] { '\\', '$', '{' };

		static bool StartsWith(string s, int index, string other) {
			if (index + other.Length > s.Length)
				return false;
			for (int i = 0; i < other.Length; i++) {
				if (s[index + i] != other[i])
					return false;
			}
			return true;
		}

		void Write(string s) => output.Append(s);
		void Write(char c) => output.Append(c);
		void WriteError() => Write("???");

		void WriteAddress() {
			//TODO:
			WriteError();
		}

		void WriteCaller() {
			//TODO:
			WriteError();
		}

		void WriteCallStack() {
			//TODO:
			WriteError();
		}

		void WriteFunction() {
			//TODO:
			WriteError();
		}

		void WriteProcessId() {
			Write("0x");
			Write(boundBreakpoint.Process.Id.ToString("X"));
		}

		void WriteProcessName() {
			var filename = boundBreakpoint.Process.Filename;
			if (!string.IsNullOrEmpty(filename))
				Write(filename);
			else
				WriteProcessId();
		}

		void WriteThreadId() {
			if (thread == null)
				WriteError();
			else {
				Write("0x");
				Write(thread.Id.ToString("X"));
			}
		}

		void WriteThreadName() {
			var name = thread?.UIName;
			if (name == null)
				WriteError();
			else
				Write(name);
		}

		void WriteExpressionValue(string exprString) {
			//TODO:
			WriteError();
		}
	}
}
