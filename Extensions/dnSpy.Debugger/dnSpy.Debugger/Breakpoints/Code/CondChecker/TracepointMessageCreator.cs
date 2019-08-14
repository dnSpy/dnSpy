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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.Evaluation;

namespace dnSpy.Debugger.Breakpoints.Code.CondChecker {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class ClearParsedMessages : IDbgManagerStartListener {
		readonly TracepointMessageCreatorImpl tracepointMessageCreatorImpl;
		[ImportingConstructor]
		ClearParsedMessages(TracepointMessageCreatorImpl tracepointMessageCreatorImpl) => this.tracepointMessageCreatorImpl = tracepointMessageCreatorImpl;
		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
		void DbgManager_IsDebuggingChanged(object? sender, EventArgs e) {
			var dbgManager = (DbgManager)sender!;
			tracepointMessageCreatorImpl.OnIsDebuggingChanged(dbgManager.IsDebugging);
		}
	}

	sealed class StringBuilderTextColorWriter : IDbgTextWriter {
		StringBuilder? sb;
		public void SetStringBuilder(StringBuilder sb) => this.sb = sb;
		public void Write(DbgTextColor color, string? text) => sb!.Append(text);
	}

	abstract class TracepointMessageCreator {
		public abstract string Create(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread, DbgCodeBreakpointTrace trace);
		public abstract void Write(IDbgTextWriter output, DbgCodeBreakpointTrace trace);
	}

	[Export(typeof(TracepointMessageCreator))]
	[Export(typeof(TracepointMessageCreatorImpl))]
	sealed class TracepointMessageCreatorImpl : TracepointMessageCreator {
		const DbgStackFrameFormatterOptions AddressFormatterOptions =
			DbgStackFrameFormatterOptions.ParameterTypes |
			DbgStackFrameFormatterOptions.IP |
			DbgStackFrameFormatterOptions.DeclaringTypes |
			DbgStackFrameFormatterOptions.Namespaces |
			DbgStackFrameFormatterOptions.IntrinsicTypeKeywords;
		const DbgStackFrameFormatterOptions CallerFormatterOptions =
			DbgStackFrameFormatterOptions.DeclaringTypes |
			DbgStackFrameFormatterOptions.Namespaces |
			DbgStackFrameFormatterOptions.IntrinsicTypeKeywords;
		const DbgStackFrameFormatterOptions CallStackFormatterOptions =
			DbgStackFrameFormatterOptions.DeclaringTypes |
			DbgStackFrameFormatterOptions.Namespaces |
			DbgStackFrameFormatterOptions.IntrinsicTypeKeywords;
		const DbgStackFrameFormatterOptions FunctionFormatterOptions =
			DbgStackFrameFormatterOptions.ParameterTypes |
			DbgStackFrameFormatterOptions.DeclaringTypes |
			DbgStackFrameFormatterOptions.Namespaces |
			DbgStackFrameFormatterOptions.IntrinsicTypeKeywords;

		readonly object lockObj;
		readonly DbgLanguageService dbgLanguageService;
		readonly DebuggerSettings debuggerSettings;
		readonly DbgEvalFormatterSettings dbgEvalFormatterSettings;
		readonly TracepointMessageParser tracepointMessageParser;
		readonly StringBuilderTextColorWriter stringBuilderTextColorWriter;
		Dictionary<string, ParsedTracepointMessage> toParsedMessage;
		WeakReference? toParsedMessageWeakRef;
		StringBuilder output;

		DbgBoundCodeBreakpoint? boundBreakpoint;
		DbgThread? thread;
		DbgStackWalker? stackWalker;
		DbgStackFrame[]? stackFrames;

		[ImportingConstructor]
		TracepointMessageCreatorImpl(DbgLanguageService dbgLanguageService, DebuggerSettings debuggerSettings, DbgEvalFormatterSettings dbgEvalFormatterSettings) {
			lockObj = new object();
			output = new StringBuilder();
			this.dbgLanguageService = dbgLanguageService;
			this.debuggerSettings = debuggerSettings;
			this.dbgEvalFormatterSettings = dbgEvalFormatterSettings;
			tracepointMessageParser = new TracepointMessageParser();
			stringBuilderTextColorWriter = new StringBuilderTextColorWriter();
			stringBuilderTextColorWriter.SetStringBuilder(output);
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
			if (boundBreakpoint is null)
				throw new ArgumentNullException(nameof(boundBreakpoint));
			var text = trace.Message;
			if (text is null)
				return string.Empty;
			try {
				output.Clear();
				this.boundBreakpoint = boundBreakpoint;
				this.thread = thread;
				var parsed = GetOrCreate(text);
				int maxFrames = parsed.MaxFrames;
				if (parsed.Evaluates && maxFrames < 1)
					maxFrames = 1;
				if (maxFrames > 0 && !(thread is null)) {
					stackWalker = thread.CreateStackWalker();
					stackFrames = stackWalker.GetNextStackFrames(maxFrames);
				}
				Write(parsed, text);
				return output.ToString();
			}
			finally {
				this.boundBreakpoint = null;
				this.thread = null;
				if (!(stackWalker is null)) {
					stackWalker.Close();
					boundBreakpoint.Process.DbgManager.Close(stackFrames!);
					stackWalker = null;
					stackFrames = null;
				}
				if (output.Capacity >= 1024) {
					output = new StringBuilder();
					stringBuilderTextColorWriter.SetStringBuilder(output);
				}
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

		DbgStackFrame? TryGetFrame(int i) {
			var frames = stackFrames;
			if (frames is null || (uint)i >= (uint)frames.Length)
				return null;
			return frames[i];
		}

		void Write(ParsedTracepointMessage parsed, string tracepointMessage) {
			Debug2.Assert(!(boundBreakpoint is null));
			DbgStackFrame? frame;
			foreach (var part in parsed.Parts) {
				switch (part.Kind) {
				case TracepointMessageKind.WriteText:
					Write(part.String!);
					break;

				case TracepointMessageKind.WriteEvaluatedExpression:
					frame = TryGetFrame(0);
					if (frame is null)
						WriteError();
					else {
						var language = dbgLanguageService.GetCurrentLanguage(thread!.Runtime.RuntimeKindGuid);
						var cancellationToken = CancellationToken.None;
						var state = GetTracepointEvalState(boundBreakpoint, language, frame, tracepointMessage, cancellationToken);
						Debug2.Assert(!(part.String is null));
						var eeState = state.GetExpressionEvaluatorState(part.String);
						var evalInfo = new DbgEvaluationInfo(state.Context!, frame, cancellationToken);
						var evalRes = language.ExpressionEvaluator.Evaluate(evalInfo, part.String, DbgEvaluationOptions.Expression, eeState);
						Write(evalInfo, language, evalRes);
					}
					break;

				case TracepointMessageKind.WriteAddress:
					WriteFrame(part.Number, AddressFormatterOptions);
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
					WriteFrame(part.Number, CallerFormatterOptions);
					break;

				case TracepointMessageKind.WriteCallerModule:
					var module = TryGetFrame(part.Number)?.Module;
					if (!(module is null))
						Write(module.Filename);
					else
						WriteError();
					break;

				case TracepointMessageKind.WriteCallerOffset:
					frame = TryGetFrame(part.Number);
					if (!(frame is null)) {
						Write("0x");
						Write(frame.FunctionOffset.ToString("X8"));
					}
					else
						WriteError();
					break;

				case TracepointMessageKind.WriteCallerToken:
					frame = TryGetFrame(part.Number);
					if (!(frame is null) && frame.HasFunctionToken) {
						Write("0x");
						Write(frame.FunctionToken.ToString("X8"));
					}
					else
						WriteError();
					break;

				case TracepointMessageKind.WriteCallStack:
					int maxFrames = part.Number;
					for (int i = 0; i < maxFrames; i++) {
						frame = TryGetFrame(i);
						if (frame is null)
							break;
						Write("\t");
						WriteFrame(frame, CallStackFormatterOptions);
						Write(Environment.NewLine);
					}
					Write("\t");
					break;

				case TracepointMessageKind.WriteFunction:
					WriteFrame(part.Number, FunctionFormatterOptions);
					break;

				case TracepointMessageKind.WriteManagedId:
					if (thread?.ManagedId is ulong mid)
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
					if (thread is null)
						WriteError();
					else {
						Write("0x");
						Write(thread.Id.ToString("X"));
					}
					break;

				case TracepointMessageKind.WriteThreadName:
					var name = thread?.UIName;
					if (name is null)
						WriteError();
					else
						Write(name);
					break;

				default: throw new InvalidOperationException();
				}
			}
		}

		void WriteFrame(int index, DbgStackFrameFormatterOptions frameOptions) => WriteFrame(TryGetFrame(index), frameOptions);

		void WriteFrame(DbgStackFrame? frame, DbgStackFrameFormatterOptions frameOptions) {
			if (!(frame is null)) {
				if (!debuggerSettings.UseHexadecimal)
					frameOptions |= DbgStackFrameFormatterOptions.Decimal;
				if (debuggerSettings.UseDigitSeparators)
					frameOptions |= DbgStackFrameFormatterOptions.DigitSeparators;
				if (debuggerSettings.FullString)
					frameOptions |= DbgStackFrameFormatterOptions.FullString;

				var language = dbgLanguageService.GetCurrentLanguage(thread!.Runtime.RuntimeKindGuid);
				const DbgValueFormatterOptions valueOptions = DbgValueFormatterOptions.None;
				const CultureInfo? cultureInfo = null;
				var cancellationToken = CancellationToken.None;
				DbgEvaluationContext? context = null;
				try {
					const DbgEvaluationContextOptions ctxOptions = DbgEvaluationContextOptions.NoMethodBody;
					context = language.CreateContext(frame, options: ctxOptions, cancellationToken: cancellationToken);
					var evalInfo = new DbgEvaluationInfo(context, frame, cancellationToken);
					language.Formatter.FormatFrame(evalInfo, stringBuilderTextColorWriter, frameOptions, valueOptions, cultureInfo);
				}
				finally {
					context?.Close();
				}
			}
			else
				WriteError();
		}

		void Write(string s) => output.Append(s);
		void WriteError() => Write("???");

		public override void Write(IDbgTextWriter output, DbgCodeBreakpointTrace trace) {
			if (output is null)
				throw new ArgumentNullException(nameof(output));
			var msg = trace.Message ?? string.Empty;
			var parsed = tracepointMessageParser.Parse(msg);
			int pos = 0;
			foreach (var part in parsed.Parts) {
				switch (part.Kind) {
				case TracepointMessageKind.WriteText:
					output.Write(DbgTextColor.String, msg.Substring(pos, part.Length));
					break;

				case TracepointMessageKind.WriteEvaluatedExpression:
					output.Write(DbgTextColor.Punctuation, msg.Substring(pos, 1));
					output.Write(DbgTextColor.Text, msg.Substring(pos + 1, part.Length - 2));
					output.Write(DbgTextColor.Punctuation, msg.Substring(pos + part.Length - 1, 1));
					break;

				case TracepointMessageKind.WriteAddress:
				case TracepointMessageKind.WriteAppDomainId:
				case TracepointMessageKind.WriteBreakpointAddress:
				case TracepointMessageKind.WriteCaller:
				case TracepointMessageKind.WriteCallerModule:
				case TracepointMessageKind.WriteCallerOffset:
				case TracepointMessageKind.WriteCallerToken:
				case TracepointMessageKind.WriteCallStack:
				case TracepointMessageKind.WriteFunction:
				case TracepointMessageKind.WriteManagedId:
				case TracepointMessageKind.WriteProcessId:
				case TracepointMessageKind.WriteProcessName:
				case TracepointMessageKind.WriteThreadId:
				case TracepointMessageKind.WriteThreadName:
					output.Write(DbgTextColor.Keyword, msg.Substring(pos, part.Length));
					break;

				default: throw new InvalidOperationException();
				}
				pos += part.Length;
			}
			Debug.Assert(pos == msg.Length);
		}

		sealed class TracepointEvalState : IDisposable {
			public DbgLanguage? Language;
			public string? TracepointMessage;

			public DbgEvaluationContext? Context {
				get => context;
				set {
					context?.Close();
					context = value;
				}
			}
			DbgEvaluationContext? context;

			public readonly Dictionary<string, object?> ExpressionEvaluatorStates = new Dictionary<string, object?>(StringComparer.Ordinal);

			public object? GetExpressionEvaluatorState(string expression) {
				if (ExpressionEvaluatorStates.TryGetValue(expression, out var state))
					return state;
				state = Language!.ExpressionEvaluator.CreateExpressionEvaluatorState();
				ExpressionEvaluatorStates[expression] = state;
				return state;
			}

			public void Dispose() {
				Language = null;
				TracepointMessage = null;
				Context = null;
				ExpressionEvaluatorStates.Clear();
			}
		}

		TracepointEvalState GetTracepointEvalState(DbgBoundCodeBreakpoint boundBreakpoint, DbgLanguage language, DbgStackFrame frame, string tracepointMessage, CancellationToken cancellationToken) {
			var state = boundBreakpoint.GetOrCreateData<TracepointEvalState>();
			if (state.Language != language || state.TracepointMessage != tracepointMessage) {
				state.Language = language;
				state.TracepointMessage = tracepointMessage;
				state.Context = language.CreateContext(frame, cancellationToken: cancellationToken);
				state.ExpressionEvaluatorStates.Clear();
			}
			return state;
		}

		void Write(DbgEvaluationInfo evalInfo, DbgLanguage language, in DbgEvaluationResult evalRes) {
			if (!(evalRes.Error is null)) {
				Write("<<<");
				Write(PredefinedEvaluationErrorMessagesHelper.GetErrorMessage(evalRes.Error));
				Write(">>>");
			}
			else {
				var options = GetValueFormatterOptions(evalRes.FormatSpecifiers, isEdit: false);
				const CultureInfo? cultureInfo = null;
				Debug2.Assert(!(evalRes.Value is null));
				language.Formatter.FormatValue(evalInfo, stringBuilderTextColorWriter, evalRes.Value, options, cultureInfo);
				evalRes.Value.Close();
			}
		}

		DbgValueFormatterOptions GetValueFormatterOptions(ReadOnlyCollection<string> formatSpecifiers, bool isEdit) {
			var options = DbgValueFormatterOptions.FuncEval | DbgValueFormatterOptions.ToString;
			if (isEdit)
				options |= DbgValueFormatterOptions.Edit;
			if (!debuggerSettings.UseHexadecimal)
				options |= DbgValueFormatterOptions.Decimal;
			if (debuggerSettings.UseDigitSeparators)
				options |= DbgValueFormatterOptions.DigitSeparators;
			if (debuggerSettings.FullString)
				options |= DbgValueFormatterOptions.FullString;
			if (dbgEvalFormatterSettings.ShowNamespaces)
				options |= DbgValueFormatterOptions.Namespaces;
			if (dbgEvalFormatterSettings.ShowIntrinsicTypeKeywords)
				options |= DbgValueFormatterOptions.IntrinsicTypeKeywords;
			if (dbgEvalFormatterSettings.ShowTokens)
				options |= DbgValueFormatterOptions.Tokens;
			return PredefinedFormatSpecifiers.GetValueFormatterOptions(formatSpecifiers, options);
		}
	}
}
