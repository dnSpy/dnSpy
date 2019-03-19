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
using System.Windows.Data;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Evaluation.ViewModel;
using dnSpy.Debugger.Evaluation.ViewModel.Impl;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Debugger.Evaluation.UI {
	sealed class VariablesWindowColumnConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var obj = value as FormatterObject<ValueNode>;
			if (obj == null)
				return null;
			bool isToolTip = parameter is string paramString && paramString == "ToolTip";

			var node = (ValueNodeImpl)obj.VM;
			var nodeCtx = node.Context;
			var writer = nodeCtx.TextClassifierTextColorWriter;
			writer.Clear();
			var formatter = nodeCtx.Formatter;
			bool textChanged = false;
			int textLen = -1;
			if (obj.Tag == nodeCtx.NameColumnName)
				formatter.WriteName(writer, obj.VM);
			else if (obj.Tag == nodeCtx.ValueColumnName) {
				formatter.WriteValue(writer, obj.VM, out textChanged);
				textLen = writer.Length;
				formatter.WriteObjectId(writer, obj.VM);
			}
			else if (obj.Tag == nodeCtx.TypeColumnName)
				formatter.WriteType(writer, obj.VM);
			else
				return null;

			if (!nodeCtx.HighlightChangedVariables)
				textChanged = false;
			var text = writer.Text;
			var textChangedSpan = new Span(0, textLen == -1 ? text.Length : textLen);
			var context = new ValueNodeTextClassifierContext(textChanged, textChangedSpan, text, obj.Tag, nodeCtx.SyntaxHighlight, writer.Colors);
			var flags = isToolTip ? TextElementFlags.Wrap : TextElementFlags.FilterOutNewLines | TextElementFlags.CharacterEllipsis;
			const double DISABLED_OPACITY = 0.5;
			double opacity = !isToolTip && node.IsDisabled ? DISABLED_OPACITY : 1.0;
			return nodeCtx.TextBlockContentInfoFactory.Create(nodeCtx.UIVersion, nodeCtx.ClassificationFormatMap, context, nodeCtx.WindowContentType, flags, opacity);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
