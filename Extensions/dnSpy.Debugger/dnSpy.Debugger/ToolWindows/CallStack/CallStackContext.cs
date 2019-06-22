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

using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text.DnSpy;
using dnSpy.Debugger.Text;
using dnSpy.Debugger.UI;
using dnSpy.Debugger.UI.Wpf;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.CallStack {
	interface ICallStackContext {
		UIDispatcher UIDispatcher { get; }
		IClassificationFormatMap ClassificationFormatMap { get; }
		ITextBlockContentInfoFactory TextBlockContentInfoFactory { get; }
		DbgTextClassifierTextColorWriter TextClassifierTextColorWriter { get; }
		int UIVersion { get; }
		CallStackFormatter Formatter { get; }
		bool SyntaxHighlight { get; }
		ClassifiedTextWriter ClassifiedTextWriter { get; }
		DbgStackFrameFormatterOptions StackFrameFormatterOptions { get; }
		DbgValueFormatterOptions ValueFormatterOptions { get; }
	}

	sealed class CallStackContext : ICallStackContext {
		public UIDispatcher UIDispatcher { get; }
		public IClassificationFormatMap ClassificationFormatMap { get; }
		public ITextBlockContentInfoFactory TextBlockContentInfoFactory { get; }
		public DbgTextClassifierTextColorWriter TextClassifierTextColorWriter { get; }
		public int UIVersion { get; set; }
		public CallStackFormatter Formatter { get; set; }
		public bool SyntaxHighlight { get; set; }
		public ClassifiedTextWriter ClassifiedTextWriter { get; }
		public DbgStackFrameFormatterOptions StackFrameFormatterOptions { get; set; }
		public DbgValueFormatterOptions ValueFormatterOptions { get; set; }

		public CallStackContext(UIDispatcher uiDispatcher, IClassificationFormatMap classificationFormatMap, ITextBlockContentInfoFactory textBlockContentInfoFactory, CallStackFormatter formatter) {
			UIDispatcher = uiDispatcher;
			ClassificationFormatMap = classificationFormatMap;
			TextBlockContentInfoFactory = textBlockContentInfoFactory;
			TextClassifierTextColorWriter = new DbgTextClassifierTextColorWriter();
			ClassifiedTextWriter = new ClassifiedTextWriter();
			Formatter = formatter;
		}
	}
}
