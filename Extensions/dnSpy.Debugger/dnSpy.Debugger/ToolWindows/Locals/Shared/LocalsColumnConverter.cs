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
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Evaluation.ViewModel;
using dnSpy.Debugger.Evaluation.ViewModel.Impl;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Locals.Shared {
	sealed class LocalsColumnConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var obj = value as FormatterObject<ValueNode>;
			if (obj == null)
				return null;

			var nodeCtx = ((ValueNodeImpl)obj.VM).Context;
			var writer = nodeCtx.TextClassifierTextColorWriter;
			writer.Clear();
			var formatter = nodeCtx.Formatter;
			bool textChanged = false;
			if (obj.Tag == nodeCtx.NameColumnName)
				formatter.WriteName(writer, obj.VM);
			else if (obj.Tag == nodeCtx.ValueColumnName)
				formatter.WriteValue(writer, obj.VM, out textChanged);
			else if (obj.Tag == nodeCtx.TypeColumnName)
				formatter.WriteType(writer, obj.VM);
			else
				return null;

			if (!nodeCtx.HighlightChangedVariables)
				textChanged = false;
			var context = new ValueNodeTextClassifierContext(textChanged, writer.Text, obj.Tag, nodeCtx.SyntaxHighlight, writer.Colors);
			return nodeCtx.TextElementProvider.CreateTextElement(nodeCtx.ClassificationFormatMap, context, nodeCtx.WindowContentType, TextElementFlags.FilterOutNewLines | TextElementFlags.CharacterEllipsis);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
