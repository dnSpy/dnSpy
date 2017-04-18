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
				// NOTE: order is important, first match is picked
				// NOTE: When updating this table, also update the help message created by ShowCodeBreakpointSettingsVM
				new KeywordInfo("ADDRESS", WriteAddress),
				new KeywordInfo("ADID", WriteAppDomainId),
				new KeywordInfo("BPADDR", WriteBreakpointAddress),
				new KeywordInfo("CALLER5", WriteCaller5),
				new KeywordInfo("CALLER4", WriteCaller4),
				new KeywordInfo("CALLER3", WriteCaller3),
				new KeywordInfo("CALLER2", WriteCaller2),
				new KeywordInfo("CALLER1", WriteCaller1),
				new KeywordInfo("CALLER", WriteCaller1),
				new KeywordInfo("CALLERMODULE5", WriteCallerModule5),
				new KeywordInfo("CALLERMODULE4", WriteCallerModule4),
				new KeywordInfo("CALLERMODULE3", WriteCallerModule3),
				new KeywordInfo("CALLERMODULE2", WriteCallerModule2),
				new KeywordInfo("CALLERMODULE1", WriteCallerModule1),
				new KeywordInfo("CALLERMODULE", WriteCallerModule1),
				new KeywordInfo("CALLEROFFSET5", WriteCallerOffset5),
				new KeywordInfo("CALLEROFFSET4", WriteCallerOffset4),
				new KeywordInfo("CALLEROFFSET3", WriteCallerOffset3),
				new KeywordInfo("CALLEROFFSET2", WriteCallerOffset2),
				new KeywordInfo("CALLEROFFSET1", WriteCallerOffset1),
				new KeywordInfo("CALLEROFFSET", WriteCallerOffset1),
				new KeywordInfo("CALLERTOKEN5", WriteCallerToken5),
				new KeywordInfo("CALLERTOKEN4", WriteCallerToken4),
				new KeywordInfo("CALLERTOKEN3", WriteCallerToken3),
				new KeywordInfo("CALLERTOKEN2", WriteCallerToken2),
				new KeywordInfo("CALLERTOKEN1", WriteCallerToken1),
				new KeywordInfo("CALLERTOKEN", WriteCallerToken1),
				new KeywordInfo("CALLSTACK10", WriteCallStack10),
				new KeywordInfo("CALLSTACK5", WriteCallStack5),
				new KeywordInfo("CALLSTACK", WriteCallStack),
				new KeywordInfo("FUNCTION5", WriteFunction5),
				new KeywordInfo("FUNCTION4", WriteFunction4),
				new KeywordInfo("FUNCTION3", WriteFunction3),
				new KeywordInfo("FUNCTION2", WriteFunction2),
				new KeywordInfo("FUNCTION1", WriteFunction1),
				new KeywordInfo("FUNCTION", WriteFunction1),
				new KeywordInfo("MID", WriteManagedId),
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
						else {
							bool ok = true;
							switch (c) {
							case 'a': Write('\a'); break;
							case 'b': Write('\b'); break;
							case 'f': Write('\f'); break;
							case 'n': Write('\n'); break;
							case 'r': Write('\r'); break;
							case 't': Write('\t'); break;
							case 'v': Write('\v'); break;
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

		void WriteAppDomainId() {
			if (thread?.AppDomain?.Id is int adid)
				Write(adid.ToString());
			else
				WriteError();
		}

		void WriteBreakpointAddress() {
			Write("0x");
			Write(boundBreakpoint.Address.ToString("X"));
		}

		void WriteAddress() {
			//TODO:
			WriteError();
		}

		void WriteCaller1() {
			//TODO: Show caller #1
			WriteError();
		}

		void WriteCaller2() {
			//TODO: Show caller #2
			WriteError();
		}

		void WriteCaller3() {
			//TODO: Show caller #3
			WriteError();
		}

		void WriteCaller4() {
			//TODO: Show caller #4
			WriteError();
		}

		void WriteCaller5() {
			//TODO: Show caller #5
			WriteError();
		}

		void WriteCallStack() {
			//TODO:
			WriteError();
		}

		void WriteCallStack5() {
			//TODO: At most 5 frames
			WriteError();
		}

		void WriteCallStack10() {
			//TODO: At most 10 frames
			WriteError();
		}

		void WriteCallerModule1() {
			//TODO: Write module of caller #1
			WriteError();
		}

		void WriteCallerModule2() {
			//TODO: Write module of caller #2
			WriteError();
		}

		void WriteCallerModule3() {
			//TODO: Write module of caller #3
			WriteError();
		}

		void WriteCallerModule4() {
			//TODO: Write module of caller #4
			WriteError();
		}

		void WriteCallerModule5() {
			//TODO: Write module of caller #5
			WriteError();
		}

		void WriteCallerOffset1() {
			//TODO: Write caller #1 method offset
			WriteError();
		}

		void WriteCallerOffset2() {
			//TODO: Write caller #2 method offset
			WriteError();
		}

		void WriteCallerOffset3() {
			//TODO: Write caller #3 method offset
			WriteError();
		}

		void WriteCallerOffset4() {
			//TODO: Write caller #4 method offset
			WriteError();
		}

		void WriteCallerOffset5() {
			//TODO: Write caller #5 method offset
			WriteError();
		}

		void WriteCallerToken1() {
			//TODO: If it's a .NET method, write caller method token (eg. 0x06123456), else address (eg. 0xABCDEF)
			WriteError();
		}

		void WriteCallerToken2() {
			//TODO: caller #2 token
			WriteError();
		}

		void WriteCallerToken3() {
			//TODO: caller #3 token
			WriteError();
		}

		void WriteCallerToken4() {
			//TODO: caller #4 token
			WriteError();
		}

		void WriteCallerToken5() {
			//TODO: caller #5 token
			WriteError();
		}

		void WriteManagedId() {
			if (thread?.ManagedId is int mid)
				Write(mid.ToString());
			else
				WriteError();
		}

		void WriteFunction1() {
			//TODO: caller func #1
			WriteError();
		}

		void WriteFunction2() {
			//TODO: caller func #2
			WriteError();
		}

		void WriteFunction3() {
			//TODO: caller func #3
			WriteError();
		}

		void WriteFunction4() {
			//TODO: caller func #4
			WriteError();
		}

		void WriteFunction5() {
			//TODO: caller func #5
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
