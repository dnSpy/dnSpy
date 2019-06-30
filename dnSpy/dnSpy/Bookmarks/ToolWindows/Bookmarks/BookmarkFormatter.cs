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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Text;

namespace dnSpy.Bookmarks.ToolWindows.Bookmarks {
	[Export(typeof(BookmarkFormatterProvider))]
	sealed class BookmarkFormatterProvider {
		public BookmarkFormatter Create() =>
			BookmarkFormatter.Create_DONT_USE();
	}

	sealed class BookmarkFormatter {
		BookmarkFormatter() { }

		internal static BookmarkFormatter Create_DONT_USE() => new BookmarkFormatter();

		public const char LabelsSeparatorChar = ',';
		static readonly string LabelsSeparatorString = LabelsSeparatorChar.ToString();

		internal void WriteLabels(ITextColorWriter output, BookmarkVM vm) {
			bool needSep = false;
			foreach (var label in vm.Bookmark.Labels ?? emptyLabels) {
				if (needSep) {
					output.Write(BoxedTextColor.Text, LabelsSeparatorString);
					output.WriteSpace();
				}
				needSep = true;
				output.Write(BoxedTextColor.Text, label);
			}
		}
		static readonly ReadOnlyCollection<string> emptyLabels = new ReadOnlyCollection<string>(Array.Empty<string>());

		internal void WriteName(ITextColorWriter output, BookmarkVM vm) {
			var color = vm.IsActive ? BoxedTextColor.ActiveBookmarkName : BoxedTextColor.BookmarkName;
			output.Write(color, vm.Bookmark.Name ?? string.Empty);
		}

		internal void WriteLocation(ITextColorWriter output, BookmarkVM vm) => vm.BookmarkLocationFormatter.WriteLocation(output, vm.Context.FormatterOptions);
		internal void WriteModule(ITextColorWriter output, BookmarkVM vm) => vm.BookmarkLocationFormatter.WriteModule(output);
	}
}
