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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class WpfTextViewLineCollection : IWpfTextViewLineCollection {
		readonly IWpfTextView textView;
		readonly ReadOnlyCollection<IWpfTextViewLine> lines;
		readonly ITextSnapshot snapshot;

		public WpfTextViewLineCollection(IWpfTextView textView, ITextSnapshot snapshot, IList<IWpfTextViewLine> lines) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			if (lines == null)
				throw new ArgumentNullException(nameof(lines));
			this.textView = textView;
			this.snapshot = snapshot;
			this.lines = new ReadOnlyCollection<IWpfTextViewLine>(lines);
			this.IsValid = true;
			if (lines.Count == 0)
				this.formattedSpan = new SnapshotSpan(snapshot, new Span(0, 0));
			else
				this.formattedSpan = new SnapshotSpan(lines[0].Start, lines[lines.Count - 1].EndIncludingLineBreak);
			Debug.Assert(this.lines.Count > 0);
		}

		public IWpfTextViewLine this[int index] => lines[index];
		ITextViewLine IList<ITextViewLine>.this[int index] {
			get { return this[index]; }
			set { throw new NotSupportedException(); }
		}

		public int Count => lines.Count;
		public bool IsReadOnly => true;

		public ReadOnlyCollection<IWpfTextViewLine> WpfTextViewLines {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
				return lines;
			}
		}

		ITextViewLine ITextViewLineCollection.FirstVisibleLine => FirstVisibleLine;
		ITextViewLine ITextViewLineCollection.LastVisibleLine => LastVisibleLine;

		public IWpfTextViewLine FirstVisibleLine {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
				foreach (var l in lines) {
					if (l.IsVisible())
						return l;
				}
				return lines[0];
			}
		}

		public IWpfTextViewLine LastVisibleLine {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
				for (int i = lines.Count - 1; i >= 0; i--) {
					var l = lines[i];
					if (l.IsVisible())
						return l;
				}
				return lines[lines.Count - 1];
			}
		}

		public SnapshotSpan FormattedSpan {
			get {
				if (!IsValid)
					throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
				return formattedSpan;
			}
		}
		readonly SnapshotSpan formattedSpan;

		public bool IsValid { get; private set; }

		public bool ContainsBufferPosition(SnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
			if (bufferPosition.Snapshot != snapshot)
				throw new ArgumentException();
			if (FormattedSpan.Contains(bufferPosition))
				return true;
			if (lines.Count > 0 && lines[lines.Count - 1].ContainsBufferPosition(bufferPosition))
				return true;
			return false;
		}

		public bool IntersectsBufferSpan(SnapshotSpan bufferSpan) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
			if (bufferSpan.Snapshot != snapshot)
				throw new ArgumentException();
			if (FormattedSpan.IntersectsWith(bufferSpan))
				return true;
			if (lines.Count > 0 && lines[lines.Count - 1].IntersectsBufferSpan(bufferSpan))
				return true;
			return false;
		}

		public TextBounds GetCharacterBounds(SnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
			if (bufferPosition.Snapshot != snapshot)
				throw new ArgumentException();
			var line = GetTextViewLineContainingBufferPosition(bufferPosition);
			if (line == null)
				throw new ArgumentOutOfRangeException(nameof(bufferPosition));
			return line.GetCharacterBounds(bufferPosition);
		}

		public int GetIndexOfTextLine(ITextViewLine textLine) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
			if (textLine == null)
				throw new ArgumentNullException(nameof(textLine));
			for (int i = 0; i < lines.Count; i++) {
				if (lines[i] == textLine)
					return i;
			}
			return -1;
		}

		public Geometry GetLineMarkerGeometry(SnapshotSpan bufferSpan) =>
			GetMarkerGeometry(bufferSpan, false, MarkerHelper.LineMarkerPadding, true);
		public Geometry GetLineMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding) =>
			GetMarkerGeometry(bufferSpan, clipToViewport, padding, true);

		public Geometry GetTextMarkerGeometry(SnapshotSpan bufferSpan) =>
			GetMarkerGeometry(bufferSpan, false, MarkerHelper.TextMarkerPadding, false);
		public Geometry GetTextMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding) =>
			GetMarkerGeometry(bufferSpan, clipToViewport, padding, false);
	
		Geometry GetMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding, bool isLineGeometry) {
			if (bufferSpan.Snapshot != snapshot)
				throw new ArgumentException();

			bool createOutlinedPath = false;
			PathGeometry geo = null;
			var textBounds = GetNormalizedTextBounds(bufferSpan);
			MarkerHelper.AddGeometries(textView, textBounds, isLineGeometry, clipToViewport, padding, 0, ref geo, ref createOutlinedPath);
			if (createOutlinedPath)
				geo = geo.GetOutlinedPathGeometry();
			if (geo != null && geo.CanFreeze)
				geo.Freeze();
			return geo;
		}

		public Geometry GetMarkerGeometry(SnapshotSpan bufferSpan) {
			if (bufferSpan.Snapshot != snapshot)
				throw new ArgumentException();
			if (MarkerHelper.IsMultiLineSpan(textView, bufferSpan))
				return GetLineMarkerGeometry(bufferSpan);
			return GetTextMarkerGeometry(bufferSpan);
		}

		public Geometry GetMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding) {
			if (bufferSpan.Snapshot != snapshot)
				throw new ArgumentException();
			if (MarkerHelper.IsMultiLineSpan(textView, bufferSpan))
				return GetLineMarkerGeometry(bufferSpan, clipToViewport, padding);
			return GetTextMarkerGeometry(bufferSpan, clipToViewport, padding);
		}

		public Collection<TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
			if (bufferSpan.Snapshot != snapshot)
				throw new ArgumentException();
			var span = FormattedSpan.Overlap(bufferSpan);
			var list = new List<TextBounds>();
			if (span == null)
				return new Collection<TextBounds>(list);

			bool found = false;
			for (int i = 0; i < lines.Count; i++) {
				var line = lines[i];
				if (line.IntersectsBufferSpan(span.Value)) {
					found = true;
					if (line.Start >= span.Value.Start && line.EndIncludingLineBreak <= span.Value.End)
						list.Add(new TextBounds(line.Left, line.Top, line.Width, line.Height, line.TextTop, line.TextHeight));
					else
						list.AddRange(line.GetNormalizedTextBounds(span.Value));
				}
				else if (found)
					break;
			}

			return new Collection<TextBounds>(list);
		}

		public SnapshotSpan GetTextElementSpan(SnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
			if (bufferPosition.Snapshot != snapshot)
				throw new ArgumentException();
			var line = GetTextViewLineContainingBufferPosition(bufferPosition);
			if (line == null)
				throw new ArgumentOutOfRangeException(nameof(bufferPosition));
			return line.GetTextElementSpan(bufferPosition);
		}

		ITextViewLine ITextViewLineCollection.GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition) =>
			GetTextViewLineContainingBufferPosition(bufferPosition);
		public IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
			if (bufferPosition.Snapshot != snapshot)
				throw new ArgumentException();
			foreach (var line in lines) {
				if (line.ContainsBufferPosition(bufferPosition))
					return line;
			}
			return null;
		}

		public ITextViewLine GetTextViewLineContainingYCoordinate(double y) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
			if (double.IsNaN(y))
				throw new ArgumentOutOfRangeException(nameof(y));
			foreach (var line in lines) {
				if (line.Top <= y && y < line.Bottom)
					return line;
			}
			return null;// Documented to return null, so don't throw AOOR
		}

		public Collection<ITextViewLine> GetTextViewLinesIntersectingSpan(SnapshotSpan bufferSpan) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
			if (bufferSpan.Snapshot != snapshot)
				throw new ArgumentException();
			var coll = new Collection<ITextViewLine>();
			for (int i = 0; i < lines.Count; i++) {
				var line = lines[i];
				if (line.IntersectsBufferSpan(bufferSpan))
					coll.Add(line);
				else if (coll.Count > 0)
					break;
			}
			return coll;
		}

		public void CopyTo(ITextViewLine[] array, int arrayIndex) {
			if (array == null)
				throw new ArgumentNullException(nameof(array));
			for (int i = 0; i < lines.Count; i++)
				array[arrayIndex + i] = lines[i];
		}

		public bool Contains(ITextViewLine item) => IndexOf(item) >= 0;
		public int IndexOf(ITextViewLine item) => lines.IndexOf(item as IWpfTextViewLine);

		public void Add(ITextViewLine item) {
			throw new NotSupportedException();
		}

		public void Clear() {
			throw new NotSupportedException();
		}

		public void Insert(int index, ITextViewLine item) {
			throw new NotSupportedException();
		}

		public bool Remove(ITextViewLine item) {
			throw new NotSupportedException();
		}

		public void RemoveAt(int index) {
			throw new NotSupportedException();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<ITextViewLine> GetEnumerator() => lines.GetEnumerator();

		public void Invalidate() {
			IsValid = false;
		}
	}
}
