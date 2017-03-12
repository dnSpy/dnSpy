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

using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Text;
using dnSpy.Debugger.ToolWindows.Text;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	interface IExceptionContext {
		UIDispatcher UIDispatcher { get; }
		IClassificationFormatMap ClassificationFormatMap { get; }
		ITextElementProvider TextElementProvider { get; }
		TextClassifierTextColorWriter TextClassifierTextColorWriter { get; }
		IDebugOutputWriter DebugOutputWriter { get; }
		ExceptionFormatter Formatter { get; }
		bool SyntaxHighlight { get; }
		DbgExceptionSettingsService ExceptionSettingsService { get; }
		DbgExceptionFormatterService ExceptionFormatterService { get; }
		SearchMatcher SearchMatcher { get; }
	}

	sealed class ExceptionContext : IExceptionContext {
		public UIDispatcher UIDispatcher { get; }
		public IClassificationFormatMap ClassificationFormatMap { get; }
		public ITextElementProvider TextElementProvider { get; }
		public TextClassifierTextColorWriter TextClassifierTextColorWriter { get; }
		public IDebugOutputWriter DebugOutputWriter { get; }
		public ExceptionFormatter Formatter { get; set; }
		public bool SyntaxHighlight { get; set; }
		public DbgExceptionSettingsService ExceptionSettingsService { get; }
		public DbgExceptionFormatterService ExceptionFormatterService { get; }
		public SearchMatcher SearchMatcher { get; }

		public ExceptionContext(UIDispatcher uiDispatcher, IClassificationFormatMap classificationFormatMap, ITextElementProvider textElementProvider, DbgExceptionSettingsService exceptionSettingsService, DbgExceptionFormatterService exceptionFormatterService, SearchMatcher searchMatcher) {
			UIDispatcher = uiDispatcher;
			ClassificationFormatMap = classificationFormatMap;
			TextElementProvider = textElementProvider;
			TextClassifierTextColorWriter = new TextClassifierTextColorWriter();
			DebugOutputWriter = new DebugOutputWriterImpl(TextClassifierTextColorWriter);
			ExceptionSettingsService = exceptionSettingsService;
			ExceptionFormatterService = exceptionFormatterService;
			SearchMatcher = searchMatcher;
		}
	}
}
