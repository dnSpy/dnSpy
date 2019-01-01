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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.Breakpoints.Code.CondChecker;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Breakpoints.Code {
	abstract class BreakpointConditionsFormatter {
		public abstract void Write(IDbgTextWriter output, DbgCodeBreakpointCondition? condition);
		public abstract void Write(IDbgTextWriter output, DbgCodeBreakpointHitCount? hitCount, int? currentHitCount);
		public abstract void Write(IDbgTextWriter output, DbgCodeBreakpointFilter? filter);
		public abstract void Write(IDbgTextWriter output, DbgCodeBreakpointTrace? trace);

		public abstract void WriteToolTip(IDbgTextWriter output, DbgCodeBreakpointCondition condition);
		public abstract void WriteToolTip(IDbgTextWriter output, DbgCodeBreakpointHitCount hitCount, int? currentHitCount);
		public abstract void WriteToolTip(IDbgTextWriter output, DbgCodeBreakpointFilter filter);
		public abstract void WriteToolTip(IDbgTextWriter output, DbgCodeBreakpointTrace trace);
	}

	[Export(typeof(BreakpointConditionsFormatter))]
	sealed class BreakpointConditionsFormatterImpl : BreakpointConditionsFormatter {
		readonly Lazy<DbgFilterExpressionEvaluatorService> dbgFilterExpressionEvaluatorService;
		readonly Lazy<TracepointMessageCreatorImpl> tracepointMessageCreatorImpl;

		[ImportingConstructor]
		BreakpointConditionsFormatterImpl(Lazy<DbgFilterExpressionEvaluatorService> dbgFilterExpressionEvaluatorService, Lazy<TracepointMessageCreatorImpl> tracepointMessageCreatorImpl) {
			this.dbgFilterExpressionEvaluatorService = dbgFilterExpressionEvaluatorService;
			this.tracepointMessageCreatorImpl = tracepointMessageCreatorImpl;
		}

		public override void Write(IDbgTextWriter output, DbgCodeBreakpointCondition? condition) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (condition == null)
				output.Write(DbgTextColor.Text, dnSpy_Debugger_Resources.Breakpoint_Condition_NoCondition);
			else {
				switch (condition.Value.Kind) {
				case DbgCodeBreakpointConditionKind.IsTrue:
					WriteArgumentAndText(output, DbgTextColor.String, dnSpy_Debugger_Resources.Breakpoint_Condition_WhenConditionIsTrue, condition.Value.Condition);
					break;

				case DbgCodeBreakpointConditionKind.WhenChanged:
					WriteArgumentAndText(output, DbgTextColor.String, dnSpy_Debugger_Resources.Breakpoint_Condition_WhenConditionHasChanged, condition.Value.Condition);
					break;

				default:
					Debug.Fail($"Unknown kind: {condition.Value.Kind}");
					break;
				}
			}
		}

		public override void Write(IDbgTextWriter output, DbgCodeBreakpointHitCount? hitCount, int? currentHitCount) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (hitCount == null)
				output.Write(DbgTextColor.Text, dnSpy_Debugger_Resources.Breakpoint_HitCount_NoHitCount);
			else {
				switch (hitCount.Value.Kind) {
				case DbgCodeBreakpointHitCountKind.Equals:
					WriteArgumentAndText(output, DbgTextColor.Number, dnSpy_Debugger_Resources.Breakpoint_HitCount_HitCountIsEqualTo, hitCount.Value.Count.ToString());
					break;

				case DbgCodeBreakpointHitCountKind.MultipleOf:
					WriteArgumentAndText(output, DbgTextColor.Number, dnSpy_Debugger_Resources.Breakpoint_HitCount_HitCountIsAMultipleOf, hitCount.Value.Count.ToString());
					break;

				case DbgCodeBreakpointHitCountKind.GreaterThanOrEquals:
					WriteArgumentAndText(output, DbgTextColor.Number, dnSpy_Debugger_Resources.Breakpoint_HitCount_HitCountIsGreaterThanOrEqualTo, hitCount.Value.Count.ToString());
					break;

				default:
					Debug.Fail($"Unknown kind: {hitCount.Value.Kind}");
					break;
				}
			}
			WriteCurrentHitCountValue(output, currentHitCount);
		}

		void WriteCurrentHitCountValue(IDbgTextWriter output, int? currentHitCount) {
			if (currentHitCount != null) {
				output.Write(DbgTextColor.Comment, " ");
				output.Write(DbgTextColor.Punctuation, "(");
				WriteArgumentAndText(output, DbgTextColor.Number, dnSpy_Debugger_Resources.Breakpoint_HitCount_CurrentHitCountValue, currentHitCount.Value.ToString());
				output.Write(DbgTextColor.Punctuation, ")");
			}
		}

		public override void Write(IDbgTextWriter output, DbgCodeBreakpointFilter? filter) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (filter == null)
				output.Write(DbgTextColor.Text, dnSpy_Debugger_Resources.Breakpoint_Filter_NoFilter);
			else
				dbgFilterExpressionEvaluatorService.Value.Write(output, filter.Value.Filter ?? string.Empty);
		}

		public override void Write(IDbgTextWriter output, DbgCodeBreakpointTrace? trace) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (trace == null)
				output.Write(DbgTextColor.Text, dnSpy_Debugger_Resources.Breakpoint_Tracepoint_NoTraceMessage);
			else {
				var traceTmp = trace.Value;
				if (traceTmp.Continue)
					WriteArgumentAndText(output, DbgTextColor.Comment, dnSpy_Debugger_Resources.Breakpoint_Tracepoint_PrintMessage, () => tracepointMessageCreatorImpl.Value.Write(output, traceTmp));
				else
					WriteArgumentAndText(output, DbgTextColor.Comment, dnSpy_Debugger_Resources.Breakpoint_Tracepoint_BreakPrintMessage, () => tracepointMessageCreatorImpl.Value.Write(output, traceTmp));
			}
		}

		void WriteArgumentAndText(IDbgTextWriter output, DbgTextColor valueColor, string formatString, string formatValue) =>
			WriteArgumentAndText(output, DbgTextColor.Comment, valueColor, formatString, formatValue);

		void WriteArgumentAndText(IDbgTextWriter output, DbgTextColor defaultColor, DbgTextColor valueColor, string formatString, string formatValue) =>
			WriteArgumentAndText(output, defaultColor, formatString, () => output.Write(valueColor, formatValue ?? string.Empty));

		void WriteArgumentAndText(IDbgTextWriter output, DbgTextColor defaultColor, string formatString, Action callback) {
			const string pattern = "{0}";
			var index = formatString.IndexOf(pattern);
			if (index < 0)
				output.Write(DbgTextColor.Error, "???");
			else {
				if (index != 0)
					output.Write(defaultColor, formatString.Substring(0, index));
				callback();
				if (index + pattern.Length != formatString.Length)
					output.Write(defaultColor, formatString.Substring(index + pattern.Length));
			}
		}

		public override void WriteToolTip(IDbgTextWriter output, DbgCodeBreakpointCondition condition) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			var defaultColor = DbgTextColor.Text;
			output.Write(defaultColor, dnSpy_Debugger_Resources.Breakpoint_Condition_ConditionalExpression);
			output.Write(DbgTextColor.Text, " ");
			switch (condition.Kind) {
			case DbgCodeBreakpointConditionKind.IsTrue:
				WriteArgumentAndText(output, defaultColor, DbgTextColor.String, dnSpy_Debugger_Resources.Breakpoint_Condition_WhenConditionIsTrue2, condition.Condition);
				break;

			case DbgCodeBreakpointConditionKind.WhenChanged:
				WriteArgumentAndText(output, defaultColor, DbgTextColor.String, dnSpy_Debugger_Resources.Breakpoint_Condition_WhenConditionHasChanged2, condition.Condition);
				break;

			default:
				Debug.Fail($"Unknown kind: {condition.Kind}");
				break;
			}
		}

		public override void WriteToolTip(IDbgTextWriter output, DbgCodeBreakpointHitCount hitCount, int? currentHitCount) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			var defaultColor = DbgTextColor.Text;
			switch (hitCount.Kind) {
			case DbgCodeBreakpointHitCountKind.Equals:
				WriteArgumentAndText(output, defaultColor, DbgTextColor.Number, dnSpy_Debugger_Resources.Breakpoint_HitCount_HitCountIsEqualTo2, hitCount.Count.ToString());
				break;

			case DbgCodeBreakpointHitCountKind.MultipleOf:
				WriteArgumentAndText(output, defaultColor, DbgTextColor.Number, dnSpy_Debugger_Resources.Breakpoint_HitCount_HitCountIsAMultipleOf2, hitCount.Count.ToString());
				break;

			case DbgCodeBreakpointHitCountKind.GreaterThanOrEquals:
				WriteArgumentAndText(output, defaultColor, DbgTextColor.Number, dnSpy_Debugger_Resources.Breakpoint_HitCount_HitCountIsGreaterThanOrEqualTo2, hitCount.Count.ToString());
				break;

			default:
				Debug.Fail($"Unknown kind: {hitCount.Kind}");
				break;
			}
			WriteCurrentHitCountValue(output, currentHitCount);
		}

		public override void WriteToolTip(IDbgTextWriter output, DbgCodeBreakpointFilter filter) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			var defaultColor = DbgTextColor.Text;
			var filterTmp = filter;
			WriteArgumentAndText(output, defaultColor, dnSpy_Debugger_Resources.Breakpoint_Filter_Filter, () => dbgFilterExpressionEvaluatorService.Value.Write(output, filterTmp.Filter ?? string.Empty));
		}

		public override void WriteToolTip(IDbgTextWriter output, DbgCodeBreakpointTrace trace) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			var defaultColor = DbgTextColor.Text;
			var traceTmp = trace;
			WriteArgumentAndText(output, defaultColor, dnSpy_Debugger_Resources.Breakpoint_Tracepoint_PrintMessage2, () => tracepointMessageCreatorImpl.Value.Write(output, traceTmp));
		}
	}
}
