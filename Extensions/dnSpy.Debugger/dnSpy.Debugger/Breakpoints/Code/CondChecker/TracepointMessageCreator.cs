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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;

namespace dnSpy.Debugger.Breakpoints.Code.CondChecker {
	[ExportDbgManagerStartListener]
	sealed class ClearParsedMessages : IDbgManagerStartListener {
		readonly TracepointMessageCreatorImpl tracepointMessageCreatorImpl;
		[ImportingConstructor]
		ClearParsedMessages(TracepointMessageCreatorImpl tracepointMessageCreatorImpl) => this.tracepointMessageCreatorImpl = tracepointMessageCreatorImpl;
		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
		void DbgManager_IsDebuggingChanged(object sender, EventArgs e) {
			var dbgManager = (DbgManager)sender;
			tracepointMessageCreatorImpl.OnIsDebuggingChanged(dbgManager.IsDebugging);
		}
	}

	abstract class TracepointMessageCreator {
		public abstract string Create(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread, DbgCodeBreakpointTrace trace);
	}

	[Export(typeof(TracepointMessageCreator))]
	[Export(typeof(TracepointMessageCreatorImpl))]
	sealed class TracepointMessageCreatorImpl : TracepointMessageCreator {
		readonly object lockObj;
		readonly TracepointMessageParser tracepointMessageParser;
		Dictionary<string, ParsedTracepointMessage> toParsedMessage;
		WeakReference toParsedMessageWeakRef;
		StringBuilder output;

		DbgBoundCodeBreakpoint boundBreakpoint;
		DbgThread thread;

		[ImportingConstructor]
		TracepointMessageCreatorImpl() {
			lockObj = new object();
			output = new StringBuilder();
			tracepointMessageParser = new TracepointMessageParser();
			toParsedMessage = CreateCachedParsedMessageDict();
		}

		static Dictionary<string, ParsedTracepointMessage> CreateCachedParsedMessageDict() => new Dictionary<string, ParsedTracepointMessage>(StringComparer.Ordinal);

		internal void OnIsDebuggingChanged(bool isDebugging) {
			lock (lockObj) {
				// Keep the parsed messages if possible (eg. user presses Restart button)
				if (isDebugging) {
					toParsedMessage = toParsedMessageWeakRef?.Target as Dictionary<string, ParsedTracepointMessage> ?? toParsedMessage ?? CreateCachedParsedMessageDict();
					toParsedMessageWeakRef = null;
				}
				else {
					toParsedMessageWeakRef = new WeakReference(toParsedMessage);
					toParsedMessage = CreateCachedParsedMessageDict();
				}
			}
		}

		public override string Create(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread, DbgCodeBreakpointTrace trace) {
			if (boundBreakpoint == null)
				throw new ArgumentNullException(nameof(boundBreakpoint));
			var text = trace.Message;
			if (text == null)
				return string.Empty;
			try {
				output.Clear();
				this.boundBreakpoint = boundBreakpoint;
				this.thread = thread;
				Write(GetOrCreate(text));
				return output.ToString();
			}
			finally {
				if (output.Capacity >= 1024)
					output = new StringBuilder();
				this.boundBreakpoint = null;
				this.thread = null;
			}
		}

		ParsedTracepointMessage GetOrCreate(string text) {
			lock (lockObj) {
				if (toParsedMessage.TryGetValue(text, out var parsed))
					return parsed;
				parsed = tracepointMessageParser.Parse(text);
				toParsedMessage.Add(text, parsed);
				return parsed;
			}
		}

		void Write(ParsedTracepointMessage parsed) {
			foreach (var part in parsed.Parts) {
				switch (part.Kind) {
				case TracepointMessageKind.WriteText:
					Write(part.String);
					break;

				case TracepointMessageKind.WriteEvaluatedExpression:
					//TODO:
					WriteError();
					break;

				case TracepointMessageKind.WriteAddress:
					//TODO:
					WriteError();
					break;

				case TracepointMessageKind.WriteAppDomainId:
					if (thread?.AppDomain?.Id is int adid)
						Write(adid.ToString());
					else
						WriteError();
					break;

				case TracepointMessageKind.WriteBreakpointAddress:
					Write("0x");
					Write(boundBreakpoint.Address.ToString("X"));
					break;

				case TracepointMessageKind.WriteCaller:
					//TODO:
					WriteError();
					break;

				case TracepointMessageKind.WriteCallerModule:
					//TODO:
					WriteError();
					break;

				case TracepointMessageKind.WriteCallerOffset:
					//TODO:
					WriteError();
					break;

				case TracepointMessageKind.WriteCallerToken:
					//TODO:
					WriteError();
					break;

				case TracepointMessageKind.WriteCallStack:
					//TODO:
					WriteError();
					break;

				case TracepointMessageKind.WriteFunction:
					//TODO:
					WriteError();
					break;

				case TracepointMessageKind.WriteManagedId:
					if (thread?.ManagedId is int mid)
						Write(mid.ToString());
					else
						WriteError();
					break;

				case TracepointMessageKind.WriteProcessId:
					Write("0x");
					Write(boundBreakpoint.Process.Id.ToString("X"));
					break;

				case TracepointMessageKind.WriteProcessName:
					var filename = boundBreakpoint.Process.Filename;
					if (!string.IsNullOrEmpty(filename))
						Write(filename);
					else
						goto case TracepointMessageKind.WriteProcessId;
					break;

				case TracepointMessageKind.WriteThreadId:
					if (thread == null)
						WriteError();
					else {
						Write("0x");
						Write(thread.Id.ToString("X"));
					}
					break;

				case TracepointMessageKind.WriteThreadName:
					var name = thread?.UIName;
					if (name == null)
						WriteError();
					else
						Write(name);
					break;

				default: throw new InvalidOperationException();
				}
			}
		}

		void Write(string s) => output.Append(s);
		void WriteError() => Write("???");
	}
}
