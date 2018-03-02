using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.CodeBreakpoints {
	sealed class CodeBreakpointVMComparer : FormatterObjectVMComparer<CodeBreakpointVM> {
		public static readonly CodeBreakpointVMComparer Instance = new CodeBreakpointVMComparer(null, ListSortDirection.Ascending);

		public CodeBreakpointVMComparer(string vmPropertyName, ListSortDirection direction) : base(vmPropertyName, direction) {}

		protected override int doCompare(CodeBreakpointVM x, CodeBreakpointVM y) {
			if (String.IsNullOrEmpty(VMPropertyName)) {
				return x.Order - y.Order;
			}

			if (Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowName)
				return Comparer<DbgCodeLocation>.Default.Compare(x.CodeBreakpoint.Location, y.CodeBreakpoint.Location);
			else if (Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowLabels)
				return String.Compare(
					String.Join(",", x.CodeBreakpoint.Labels ?? Enumerable.Empty<string>()),
					String.Join(",", y.CodeBreakpoint.Labels ?? Enumerable.Empty<string>())
				);
			else if (Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowCondition)
				return String.Compare(x.CodeBreakpoint.Condition?.Condition, y.CodeBreakpoint.Condition?.Condition);
			else if (Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowHitCount)
				return Comparer<int?>.Default.Compare(x.CodeBreakpoint.HitCount?.Count, y.CodeBreakpoint.HitCount?.Count);
			else if (Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowFilter)
				return String.Compare(x.CodeBreakpoint.Filter?.Filter, y.CodeBreakpoint.Filter?.Filter);
			else if (Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowWhenHit)
				return String.Compare(x.CodeBreakpoint.Trace?.Message, y.CodeBreakpoint.Trace?.Message);
			else if (Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowModule)
				return Comparer<DbgCodeLocation>.Default.Compare(x.CodeBreakpoint.Location, y.CodeBreakpoint.Location);
			else
				Debug.Fail($"Unknown code breakpoint property: {Tag}");

			return 0;
		}
	}
}
