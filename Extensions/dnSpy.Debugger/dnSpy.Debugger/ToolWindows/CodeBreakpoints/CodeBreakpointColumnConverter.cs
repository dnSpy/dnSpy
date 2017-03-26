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
using System.Globalization;
using System.Windows.Data;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Text;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.CodeBreakpoints {
	sealed class CodeBreakpointColumnConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var obj = value as FormatterObject<CodeBreakpointVM>;
			if (obj == null)
				return null;

			var writer = obj.VM.Context.TextClassifierTextColorWriter;
			writer.Clear();
			var formatter = obj.VM.Context.Formatter;
			if (obj.Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowName)
				formatter.WriteName(writer, obj.VM.CodeBreakpoint);
			else if (obj.Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowCondition)
				formatter.WriteCondition(writer, obj.VM.CodeBreakpoint);
			else if (obj.Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowHitCount)
				formatter.WriteHitCount(writer, obj.VM.CodeBreakpoint);
			else if (obj.Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowFilter)
				formatter.WriteFilter(writer, obj.VM.CodeBreakpoint);
			else if (obj.Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowWhenHit)
				formatter.WriteWhenHit(writer, obj.VM.CodeBreakpoint);
			else if (obj.Tag == PredefinedTextClassifierTags.CodeBreakpointsWindowModule)
				formatter.WriteModule(writer, obj.VM.CodeBreakpoint);
			else
				return null;

			var context = new SearchTextClassifierContext(obj.VM.Context.SearchMatcher, writer.Text, obj.Tag, obj.VM.Context.SyntaxHighlight, writer.Colors);
			return obj.VM.Context.TextElementProvider.CreateTextElement(obj.VM.Context.ClassificationFormatMap, context, ContentTypes.CodeBreakpointsWindow, TextElementFlags.FilterOutNewLines | TextElementFlags.CharacterEllipsis);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
