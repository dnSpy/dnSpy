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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text {
	sealed class TextSnapshot : ITextSnapshot2 {
		public char this[int position] => textImage[position];
		public IContentType ContentType { get; }
		public int Length => textImage.Length;
		public int LineCount => textImage.LineCount;
		ITextBuffer ITextSnapshot.TextBuffer => TextBuffer;
		public ITextVersion Version { get; }
		public ITextImage TextImage => textImage;
		TextBuffer TextBuffer { get; }
		TextImage textImage;

		public TextSnapshot(TextImage textImage, IContentType contentType, TextBuffer textBuffer, ITextVersion textVersion) {
			this.textImage = textImage ?? throw new ArgumentNullException(nameof(textImage));
			ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
			TextBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
			Version = textVersion ?? throw new ArgumentNullException(nameof(textVersion));
		}

		public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => textImage.CopyTo(sourceIndex, destination, destinationIndex, count);
		public string GetText() => textImage.GetText();
		public string GetText(Span span) => textImage.GetText(span);
		public string GetText(int startIndex, int length) => textImage.GetText(startIndex, length);
		public char[] ToCharArray(int startIndex, int length) => textImage.ToCharArray(startIndex, length);
		public void Write(TextWriter writer) => textImage.Write(writer);
		public void Write(TextWriter writer, Span span) => textImage.Write(writer, span);

		public IEnumerable<ITextSnapshotLine> Lines {
			get {
				uint[] lineOffsets;
				if (TextBuffer.IsSafeToAccessDocumentFromSnapshot(textImage)) {
					int lineNo = 0;
					foreach (var docLine in TextBuffer.Document.Lines) {
						yield return new TextSnapshotLine(this, lineNo, docLine.Offset, docLine.Length, docLine.DelimiterLength);
						lineNo++;
						// Make sure text buffer wasn't edited
						if (!TextBuffer.IsSafeToAccessDocumentFromSnapshot(textImage)) {
							lineOffsets = textImage.GetOrCreateLineOffsets();
							for (; lineNo < lineOffsets.Length; lineNo++)
								yield return GetLineFromLineNumber(lineNo);
							yield break;
						}
					}
					yield break;
				}
				lineOffsets = textImage.GetOrCreateLineOffsets();
				for (int lineNo = 0; lineNo < lineOffsets.Length; lineNo++)
					yield return GetLineFromLineNumber(lineNo);
			}
		}

		public ITextSnapshotLine GetLineFromLineNumber(int lineNumber) {
			var line = textImage.GetLineFromLineNumber(lineNumber);
			return new TextSnapshotLine(this, line.LineNumber, line.Start, line.Length, line.LineBreakLength);
		}

		public ITextSnapshotLine GetLineFromPosition(int position) {
			var line = textImage.GetLineFromPosition(position);
			return new TextSnapshotLine(this, line.LineNumber, line.Start, line.Length, line.LineBreakLength);
		}

		public int GetLineNumberFromPosition(int position) => textImage.GetLineNumberFromPosition(position);

		public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode) =>
			Version.CreateTrackingPoint(position, trackingMode);
		public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) =>
			Version.CreateTrackingPoint(position, trackingMode, trackingFidelity);
		public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode) =>
			Version.CreateTrackingSpan(span, trackingMode);
		public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) =>
			Version.CreateTrackingSpan(span, trackingMode, trackingFidelity);
		public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode) =>
			Version.CreateTrackingSpan(start, length, trackingMode);
		public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity) =>
			Version.CreateTrackingSpan(start, length, trackingMode, trackingFidelity);

		public void SaveToFile(string filePath, bool replaceFile, Encoding encoding) {
			if (replaceFile && File.Exists(filePath))
				File.Delete(filePath);
			File.WriteAllText(filePath, GetText());
		}

		public override string ToString() => GetText();
	}
}
