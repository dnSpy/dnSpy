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
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineValueNodeImpl : DbgEngineValueNode {
		public override string? ErrorMessage => dnValueNode.ErrorMessage;
		public override DbgEngineValue? Value => value;
		public override string Expression => dnValueNode.Expression;
		public override string ImageName => dnValueNode.ImageName;
		public override bool IsReadOnly => value is null || dnValueNode.IsReadOnly;
		public override bool CausesSideEffects => dnValueNode.CausesSideEffects;
		public override bool? HasChildren => dnValueNode.HasChildren;

		readonly DbgDotNetEngineValueNodeFactoryImpl owner;
		readonly DbgDotNetValueNode dnValueNode;
		readonly DbgEngineValueImpl? value;

		public DbgEngineValueNodeImpl(DbgDotNetEngineValueNodeFactoryImpl owner, DbgDotNetValueNode dnValueNode) {
			if (dnValueNode is null)
				throw new ArgumentNullException(nameof(dnValueNode));
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			var dnValue = dnValueNode.Value;
			value = dnValue is null ? null : new DbgEngineValueImpl(dnValue);
			this.dnValueNode = dnValueNode;
		}

		public override ulong GetChildCount(DbgEvaluationInfo evalInfo) {
			var dispatcher = evalInfo.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return GetChildCountCore(evalInfo);
			return GetChildCount(dispatcher, evalInfo);

			ulong GetChildCount(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2) {
				if (!dispatcher2.TryInvokeRethrow(() => GetChildCountCore(evalInfo2), out var result))
					result = 0;
				return result;
			}
		}

		ulong GetChildCountCore(DbgEvaluationInfo evalInfo) =>
			dnValueNode.GetChildCount(evalInfo);

		public override DbgEngineValueNode[] GetChildren(DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options) {
			var dispatcher = evalInfo.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return GetChildrenCore(evalInfo, index, count, options);
			return GetChildren(dispatcher, evalInfo, index, count, options);

			DbgEngineValueNode[] GetChildren(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2, ulong index2, int count2, DbgValueNodeEvaluationOptions options2) {
				if (!dispatcher2.TryInvokeRethrow(() => GetChildrenCore(evalInfo2, index2, count2, options2), out var result))
					result = Array.Empty<DbgEngineValueNode>();
				return result;
			}
		}

		DbgEngineValueNode[] GetChildrenCore(DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options) {
			DbgEngineValueNode[]? res = null;
			DbgDotNetValueNode[]? dnNodes = null;
			try {
				dnNodes = dnValueNode.GetChildren(evalInfo, index, count, options);
				res = new DbgEngineValueNode[dnNodes.Length];
				for (int i = 0; i < res.Length; i++) {
					evalInfo.CancellationToken.ThrowIfCancellationRequested();
					res[i] = owner.Create(dnNodes[i]);
				}
			}
			catch (Exception ex) {
				if (!(res is null))
					evalInfo.Runtime.Process.DbgManager.Close(res.Where(a => !(a is null)));
				if (!(dnNodes is null))
					evalInfo.Runtime.Process.DbgManager.Close(dnNodes);
				if (!ExceptionUtils.IsInternalDebuggerError(ex))
					throw;
				res = new DbgEngineValueNode[count];
				for (int i = 0; i < res.Length; i++)
					res[i] = owner.CreateError(evalInfo, DbgDotNetEngineValueNodeFactoryExtensions.errorName, PredefinedEvaluationErrorMessages.InternalDebuggerError, "<expression>", false);
				return res;
			}
			return res;
		}

		public override void Format(DbgEvaluationInfo evalInfo, IDbgValueNodeFormatParameters options, CultureInfo? cultureInfo) {
			var dispatcher = evalInfo.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				FormatCore(evalInfo, options, cultureInfo);
			else
				Format2(dispatcher, evalInfo, options, cultureInfo);

			void Format2(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2, IDbgValueNodeFormatParameters options2, CultureInfo? cultureInfo2) =>
				dispatcher2.TryInvokeRethrow(() => FormatCore(evalInfo2, options2, cultureInfo2));
		}

		void FormatCore(DbgEvaluationInfo evalInfo, IDbgValueNodeFormatParameters options, CultureInfo? cultureInfo) {
			evalInfo.Runtime.GetDotNetRuntime().Dispatcher.VerifyAccess();
			DbgValueFormatterOptions formatterOptions;
			DbgValueFormatterTypeOptions typeFormatterOptions;
			var formatter = owner.Formatter;
			var dnValue = value?.DotNetValue;
			if (!(options.NameOutput is null)) {
				formatterOptions = PredefinedFormatSpecifiers.GetValueFormatterOptions(dnValueNode.FormatSpecifiers, options.NameFormatterOptions);
				if (dnValueNode.FormatName(evalInfo, options.NameOutput, formatter, formatterOptions, cultureInfo)) {
					// Nothing
				}
				else
					dnValueNode.Name.WriteTo(options.NameOutput);
				evalInfo.CancellationToken.ThrowIfCancellationRequested();
			}
			if (!(options.ExpectedTypeOutput is null)) {
				formatterOptions = PredefinedFormatSpecifiers.GetValueFormatterOptions(dnValueNode.FormatSpecifiers, options.TypeFormatterOptions);
				typeFormatterOptions = PredefinedFormatSpecifiers.GetValueFormatterTypeOptions(dnValueNode.FormatSpecifiers, options.ExpectedTypeFormatterOptions);
				if (dnValueNode.FormatExpectedType(evalInfo, options.ExpectedTypeOutput, formatter, typeFormatterOptions, formatterOptions, cultureInfo)) {
					// Nothing
				}
				else if (dnValueNode.ExpectedType is DmdType expectedType)
					formatter.FormatType(evalInfo, options.ExpectedTypeOutput, expectedType, null, typeFormatterOptions, cultureInfo);
				evalInfo.CancellationToken.ThrowIfCancellationRequested();
			}
			if (!(options.ActualTypeOutput is null)) {
				formatterOptions = PredefinedFormatSpecifiers.GetValueFormatterOptions(dnValueNode.FormatSpecifiers, options.TypeFormatterOptions);
				typeFormatterOptions = PredefinedFormatSpecifiers.GetValueFormatterTypeOptions(dnValueNode.FormatSpecifiers, options.ActualTypeFormatterOptions);
				if (dnValueNode.FormatActualType(evalInfo, options.ActualTypeOutput, formatter, typeFormatterOptions, formatterOptions, cultureInfo)) {
					// Nothing
				}
				else if (dnValueNode.ActualType is DmdType actualType)
					formatter.FormatType(evalInfo, options.ActualTypeOutput, actualType, dnValue, typeFormatterOptions, cultureInfo);
				evalInfo.CancellationToken.ThrowIfCancellationRequested();
			}
			if (!(options.ValueOutput is null)) {
				formatterOptions = PredefinedFormatSpecifiers.GetValueFormatterOptions(dnValueNode.FormatSpecifiers, options.ValueFormatterOptions);
				if (dnValueNode.FormatValue(evalInfo, options.ValueOutput, formatter, formatterOptions, cultureInfo)) {
					// Nothing
				}
				else if (!(dnValue is null))
					formatter.FormatValue(evalInfo, options.ValueOutput, dnValue, formatterOptions, cultureInfo);
				else if (ErrorMessage is string errorMessage)
					options.ValueOutput.Write(DbgTextColor.Error, owner.ErrorMessagesHelper.GetErrorMessage(errorMessage));
				evalInfo.CancellationToken.ThrowIfCancellationRequested();
			}
		}

		public override DbgEngineValueNodeAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options) {
			var dispatcher = evalInfo.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return AssignCore(evalInfo, expression, options);
			return Assign(dispatcher, evalInfo, expression, options);

			DbgEngineValueNodeAssignmentResult Assign(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2, string expression2, DbgEvaluationOptions options2) {
				if (!dispatcher2.TryInvokeRethrow(() => AssignCore(evalInfo2, expression2, options2), out var result))
					result = new DbgEngineValueNodeAssignmentResult(DbgEEAssignmentResultFlags.None, DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgEngineValueNodeAssignmentResult AssignCore(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options) {
			try {
				var ee = evalInfo.Context.Language.ExpressionEvaluator;
				var res = ee.Assign(evalInfo, Expression, expression, options);
				return new DbgEngineValueNodeAssignmentResult(res.Flags, res.Error);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgEngineValueNodeAssignmentResult(DbgEEAssignmentResultFlags.None, PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			dnValueNode.Close(dispatcher);
			Value?.Close(dispatcher);
		}
	}
}
