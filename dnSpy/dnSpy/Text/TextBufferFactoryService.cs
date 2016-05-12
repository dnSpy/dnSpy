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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Threading;
using dnSpy.Contracts.Text;

namespace dnSpy.Text {
	[Export(typeof(ITextBufferFactoryService))]
	sealed class TextBufferFactoryService : ITextBufferFactoryService {
		public IContentType InertContentType { get; }
		public IContentType PlaintextContentType { get; }
		public IContentType TextContentType { get; }
		public event EventHandler<TextBufferCreatedEventArgs> TextBufferCreated;

		readonly IContentTypeRegistryService contentTypeRegistryService;

		[ImportingConstructor]
		TextBufferFactoryService(IContentTypeRegistryService contentTypeRegistryService) {
			this.contentTypeRegistryService = contentTypeRegistryService;
			InertContentType = contentTypeRegistryService.GetContentType(ContentTypes.INERT);
			PlaintextContentType = contentTypeRegistryService.GetContentType(ContentTypes.PLAIN_TEXT);
			TextContentType = contentTypeRegistryService.GetContentType(ContentTypes.TEXT);
			Debug.Assert(InertContentType != null);
			Debug.Assert(PlaintextContentType != null);
			Debug.Assert(TextContentType != null);
		}

		public ITextBuffer CreateTextBuffer() => CreateTextBuffer(TextContentType);
		public ITextBuffer CreateTextBuffer(Guid contentType) => CreateTextBuffer(contentTypeRegistryService.GetContentType(contentType));
		public ITextBuffer CreateTextBuffer(IContentType contentType) => CreateTextBuffer(string.Empty, contentType);
		public ITextBuffer CreateTextBuffer(string text, Guid contentType) => CreateTextBuffer(text, contentTypeRegistryService.GetContentType(contentType));
		public ITextBuffer CreateTextBuffer(TextReader reader, Guid contentType) => CreateTextBuffer(reader, contentTypeRegistryService.GetContentType(contentType));
		public ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType) => CreateTextBuffer(ToString(reader), contentType);
		public ITextBuffer CreateTextBuffer(string text, IContentType contentType) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			var textBuffer = new TextBuffer(Dispatcher.CurrentDispatcher, contentType, text);
			TextBufferCreated?.Invoke(this, new TextBufferCreatedEventArgs(textBuffer));
			return textBuffer;
		}

		static string ToString(TextReader reader) {
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));
			var sb = new StringBuilder();
			var buf = new char[1024];
			for (;;) {
				int len = reader.Read(buf, 0, buf.Length);
				if (len == 0)
					break;
				sb.Append(buf, 0, len);
			}
			return sb.ToString();
		}
	}
}
