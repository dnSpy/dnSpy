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
using dnSpy.Contracts.Settings.Dialog;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.CodeEditor {
	sealed class DefaultCodeEditorOptionsImpl : ICodeEditorOptions {
		public static readonly DefaultCodeEditorOptionsImpl Instance = new DefaultCodeEditorOptionsImpl();
		public IContentType ContentType {
			get { throw new NotSupportedException(); }
		}
		public Guid Guid => Guid.Empty;
		public string LanguageName => string.Empty;
		public bool UseVirtualSpace {
			get { return DefaultCodeEditorOptions.UseVirtualSpace; }
			set { }
		}
		public WordWrapStyles WordWrapStyle {
			get { return DefaultCodeEditorOptions.WordWrapStyle; }
			set { }
		}
		public bool ShowLineNumbers {
			get { return DefaultCodeEditorOptions.ShowLineNumbers; }
			set { }
		}
		public int TabSize {
			get { return DefaultCodeEditorOptions.TabSize; }
			set { }
		}
		public int IndentSize {
			get { return DefaultCodeEditorOptions.IndentSize; }
			set { }
		}
		public bool ConvertTabsToSpaces {
			get { return DefaultCodeEditorOptions.ConvertTabsToSpaces; }
			set { }
		}
	}
}
