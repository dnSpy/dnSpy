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

using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Text.DnSpy;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.ToolWindows.Search;
using dnSpy.Debugger.Breakpoints.Code;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.CodeBreakpoints {
	interface ICodeBreakpointContext {
		UIDispatcher UIDispatcher { get; }
		IClassificationFormatMap ClassificationFormatMap { get; }
		ITextElementProvider TextElementProvider { get; }
		DbgTextClassifierTextColorWriter TextClassifierTextColorWriter { get; }
		CodeBreakpointFormatter Formatter { get; }
		BreakpointConditionsFormatter BreakpointConditionsFormatter { get; }
		DbgBreakpointLocationFormatterOptions BreakpointLocationFormatterOptions { get; }
		bool SyntaxHighlight { get; }
		DbgCodeBreakpointHitCountService2 DbgCodeBreakpointHitCountService { get; }
		SearchMatcher SearchMatcher { get; }
	}

	sealed class CodeBreakpointContext : ICodeBreakpointContext {
		public UIDispatcher UIDispatcher { get; }
		public IClassificationFormatMap ClassificationFormatMap { get; }
		public ITextElementProvider TextElementProvider { get; }
		public DbgTextClassifierTextColorWriter TextClassifierTextColorWriter { get; }
		public CodeBreakpointFormatter Formatter { get; set; }
		public BreakpointConditionsFormatter BreakpointConditionsFormatter { get; }
		public DbgBreakpointLocationFormatterOptions BreakpointLocationFormatterOptions { get; set; }
		public bool SyntaxHighlight { get; set; }
		public DbgCodeBreakpointHitCountService2 DbgCodeBreakpointHitCountService { get; }
		public SearchMatcher SearchMatcher { get; }

		public CodeBreakpointContext(UIDispatcher uiDispatcher, IClassificationFormatMap classificationFormatMap, ITextElementProvider textElementProvider, BreakpointConditionsFormatter breakpointConditionsFormatter, DbgCodeBreakpointHitCountService2 dbgCodeBreakpointHitCountService, SearchMatcher searchMatcher, CodeBreakpointFormatter formatter) {
			UIDispatcher = uiDispatcher;
			ClassificationFormatMap = classificationFormatMap;
			TextElementProvider = textElementProvider;
			TextClassifierTextColorWriter = new DbgTextClassifierTextColorWriter();
			BreakpointConditionsFormatter = breakpointConditionsFormatter;
			DbgCodeBreakpointHitCountService = dbgCodeBreakpointHitCountService;
			SearchMatcher = searchMatcher;
			Formatter = formatter;
		}
	}
}
