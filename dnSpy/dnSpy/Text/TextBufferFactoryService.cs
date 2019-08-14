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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text {
	[Export(typeof(ITextBufferFactoryService))]
	sealed class TextBufferFactoryService : ITextBufferFactoryService3 {
		public IContentType InertContentType { get; }
		public IContentType PlaintextContentType { get; }
		public IContentType TextContentType { get; }
		public event EventHandler<TextBufferCreatedEventArgs>? TextBufferCreated;

		readonly IContentTypeRegistryService contentTypeRegistryService;

		[ImportingConstructor]
		TextBufferFactoryService(IContentTypeRegistryService contentTypeRegistryService) {
			this.contentTypeRegistryService = contentTypeRegistryService;
			InertContentType = contentTypeRegistryService.GetContentType(ContentTypes.Inert);
			PlaintextContentType = contentTypeRegistryService.GetContentType(ContentTypes.PlainText);
			TextContentType = contentTypeRegistryService.GetContentType(ContentTypes.Text);
			Debug2.Assert(!(InertContentType is null));
			Debug2.Assert(!(PlaintextContentType is null));
			Debug2.Assert(!(TextContentType is null));
		}

		public ITextBuffer CreateTextBuffer() => CreateTextBuffer(TextContentType);
		public ITextBuffer CreateTextBuffer(IContentType contentType) => CreateTextBuffer(string.Empty, contentType);
		public ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType) => CreateTextBuffer(ToString(reader), contentType);
		public ITextBuffer CreateTextBuffer(string text, IContentType contentType) {
			if (text is null)
				throw new ArgumentNullException(nameof(text));
			if (contentType is null)
				throw new ArgumentNullException(nameof(contentType));
			var textBuffer = new TextBuffer(contentType, text);
			TextBufferCreated?.Invoke(this, new TextBufferCreatedEventArgs(textBuffer));
			return textBuffer;
		}

		public ITextBuffer CreateTextBuffer(SnapshotSpan span, IContentType contentType) {
			if (span.Snapshot is null)
				throw new ArgumentException(nameof(span));
			return CreateTextBuffer(span.GetText(), contentType);
		}

		public ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType, long length, string traceId) {
			if (reader is null)
				throw new ArgumentNullException(nameof(reader));
			throw new NotImplementedException();
		}

		public ITextBuffer CreateTextBuffer(ITextImage image, IContentType contentType) {
			if (image is null)
				throw new ArgumentNullException(nameof(image));
			return CreateTextBuffer(image.GetText(), contentType);
		}

		static string ToString(TextReader reader) {
			if (reader is null)
				throw new ArgumentNullException(nameof(reader));
			var sb = new StringBuilder();
			var buf = Cache.GetReadBuffer();
			for (;;) {
				int len = reader.Read(buf, 0, buf.Length);
				if (len == 0)
					break;
				sb.Append(buf, 0, len);
			}
			Cache.FreeReadBuffer(buf);
			return sb.ToString();
		}

		static class Cache {
			public static void FreeReadBuffer(char[] buffer) => Interlocked.Exchange(ref __readBuffer, new WeakReference(buffer));
			public static char[] GetReadBuffer() => Interlocked.Exchange(ref __readBuffer, null)?.Target as char[] ?? new char[BUF_LENGTH];
			static WeakReference? __readBuffer;
			const int BUF_LENGTH = 4096;
		}
	}
}
