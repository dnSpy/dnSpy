/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.CodeEditor {
	interface ICodeEditorOptions {
		IContentType ContentType { get; }
		Guid Guid { get; }
		string LanguageName { get; }
		bool UseVirtualSpace { get; set; }
		WordWrapStyles WordWrapStyle { get; set; }
		bool ShowLineNumbers { get; set; }
		int TabSize { get; set; }
		int IndentSize { get; set; }
		bool ConvertTabsToSpaces { get; set; }
		bool HorizontalScrollBar { get; set; }
		bool VerticalScrollBar { get; set; }
	}
}
