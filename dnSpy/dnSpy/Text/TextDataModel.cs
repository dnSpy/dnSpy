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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text {
	sealed class TextDataModel : ITextDataModel {
		public IContentType ContentType => textBuffer.ContentType;
		public ITextBuffer DataBuffer => textBuffer;
		public ITextBuffer DocumentBuffer => textBuffer;
		readonly ITextBuffer textBuffer;

		EventHandler<TextDataModelContentTypeChangedEventArgs> realContentTypeChanged;
		public event EventHandler<TextDataModelContentTypeChangedEventArgs> ContentTypeChanged {
			add {
				if (realContentTypeChanged == null)
					textBuffer.ContentTypeChanged += TextBuffer_ContentTypeChanged;
				realContentTypeChanged += value;
			}
			remove {
				realContentTypeChanged -= value;
				if (realContentTypeChanged == null)
					textBuffer.ContentTypeChanged -= TextBuffer_ContentTypeChanged;
			}
		}

		void TextBuffer_ContentTypeChanged(object sender, ContentTypeChangedEventArgs e) =>
			realContentTypeChanged?.Invoke(this, new TextDataModelContentTypeChangedEventArgs(e.BeforeContentType, e.AfterContentType));

		public TextDataModel(ITextBuffer textBuffer) => this.textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
	}
}
