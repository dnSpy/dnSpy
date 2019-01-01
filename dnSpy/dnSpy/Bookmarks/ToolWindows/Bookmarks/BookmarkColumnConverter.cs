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
using dnSpy.Bookmarks.UI;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.ToolWindows.Search;

namespace dnSpy.Bookmarks.ToolWindows.Bookmarks {
	sealed class BookmarkColumnConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var obj = value as FormatterObject<BookmarkVM>;
			if (obj == null)
				return null;

			var writer = obj.VM.Context.TextClassifierTextColorWriter;
			writer.Clear();
			var formatter = obj.VM.Context.Formatter;
			if (obj.Tag == PredefinedTextClassifierTags.BookmarksWindowName)
				formatter.WriteName(writer, obj.VM);
			else if (obj.Tag == PredefinedTextClassifierTags.BookmarksWindowLabels)
				formatter.WriteLabels(writer, obj.VM);
			else if (obj.Tag == PredefinedTextClassifierTags.BookmarksWindowLocation)
				formatter.WriteLocation(writer, obj.VM);
			else if (obj.Tag == PredefinedTextClassifierTags.BookmarksWindowModule)
				formatter.WriteModule(writer, obj.VM);
			else
				return null;

			var context = new SearchTextClassifierContext(obj.VM.Context.SearchMatcher, writer.Text, obj.Tag, obj.VM.Context.SyntaxHighlight, writer.Colors);
			return obj.VM.Context.TextElementProvider.CreateTextElement(obj.VM.Context.ClassificationFormatMap, context, ContentTypes.BookmarksWindow, TextElementFlags.FilterOutNewLines | TextElementFlags.CharacterEllipsis);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
