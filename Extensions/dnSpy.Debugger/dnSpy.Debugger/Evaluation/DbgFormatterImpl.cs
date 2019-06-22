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
using System.Globalization;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.CallStack;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgFormatterImpl : DbgFormatter {
		public override DbgLanguage Language { get; }

		readonly Guid runtimeKindGuid;
		readonly DbgEngineFormatter engineFormatter;

		public DbgFormatterImpl(DbgLanguage language, Guid runtimeKindGuid, DbgEngineFormatter engineFormatter) {
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.runtimeKindGuid = runtimeKindGuid;
			this.engineFormatter = engineFormatter ?? throw new ArgumentNullException(nameof(engineFormatter));
		}

		static void WriteError(IDbgTextWriter output) => output.Write(DbgTextColor.Error, "???");

		public override void FormatExceptionName(DbgEvaluationContext context, IDbgTextWriter output, uint id) {
			if (context is null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (output is null)
				throw new ArgumentNullException(nameof(output));
			try {
				engineFormatter.FormatExceptionName(context, output, id);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				WriteError(output);
			}
		}

		public override void FormatStowedExceptionName(DbgEvaluationContext context, IDbgTextWriter output, uint id) {
			if (context is null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (output is null)
				throw new ArgumentNullException(nameof(output));
			try {
				engineFormatter.FormatStowedExceptionName(context, output, id);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				WriteError(output);
			}
		}

		public override void FormatReturnValueName(DbgEvaluationContext context, IDbgTextWriter output, uint id) {
			if (context is null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (output is null)
				throw new ArgumentNullException(nameof(output));
			try {
				engineFormatter.FormatReturnValueName(context, output, id);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				WriteError(output);
			}
		}

		public override void FormatObjectIdName(DbgEvaluationContext context, IDbgTextWriter output, uint id) {
			if (context is null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (output is null)
				throw new ArgumentNullException(nameof(output));
			try {
				engineFormatter.FormatObjectIdName(context, output, id);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				WriteError(output);
			}
		}

		public override void FormatFrame(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgStackFrameFormatterOptions options, DbgValueFormatterOptions valueOptions, CultureInfo? cultureInfo) {
			if (evalInfo is null)
				throw new ArgumentNullException(nameof(evalInfo));
			if (!(evalInfo.Context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (evalInfo.Context.Language != Language)
				throw new ArgumentException();
			if (evalInfo.Context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (output is null)
				throw new ArgumentNullException(nameof(output));
			var frameImpl = evalInfo.Frame as DbgStackFrameImpl;
			if (frameImpl is null)
				throw new ArgumentException();
			try {
				if (!frameImpl.TryFormat(evalInfo.Context, output, options, valueOptions, cultureInfo, evalInfo.CancellationToken))
					engineFormatter.FormatFrame(evalInfo, output, options, valueOptions, cultureInfo);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				WriteError(output);
			}
		}

		public override void FormatValue(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgValue value, DbgValueFormatterOptions options, CultureInfo? cultureInfo) {
			if (evalInfo is null)
				throw new ArgumentNullException(nameof(evalInfo));
			if (!(evalInfo.Context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (evalInfo.Context.Language != Language)
				throw new ArgumentException();
			if (evalInfo.Context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (output is null)
				throw new ArgumentNullException(nameof(output));
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			if (!(value is DbgValueImpl valueImpl))
				throw new ArgumentException();
			if (value.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			try {
				engineFormatter.FormatValue(evalInfo, output, valueImpl.EngineValue, options, cultureInfo);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				WriteError(output);
			}
		}

		public override void FormatType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgValue value, DbgValueFormatterTypeOptions options, CultureInfo? cultureInfo) {
			if (evalInfo is null)
				throw new ArgumentNullException(nameof(evalInfo));
			if (!(evalInfo.Context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (evalInfo.Context.Language != Language)
				throw new ArgumentException();
			if (evalInfo.Context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (output is null)
				throw new ArgumentNullException(nameof(output));
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			if (!(value is DbgValueImpl valueImpl))
				throw new ArgumentException();
			if (value.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			try {
				engineFormatter.FormatType(evalInfo, output, valueImpl.EngineValue, options, cultureInfo);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				WriteError(output);
			}
		}
	}
}
