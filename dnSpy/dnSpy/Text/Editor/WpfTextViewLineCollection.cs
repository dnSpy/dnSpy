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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class WpfTextViewLineCollection : IWpfTextViewLineCollection {
		readonly ReadOnlyCollection<IWpfTextViewLine> lines;
		readonly ITextSnapshot snapshot;

		public WpfTextViewLineCollection() {
			this.lines = new ReadOnlyCollection<IWpfTextViewLine>(Array.Empty<IWpfTextViewLine>());
			this.snapshot = null;
		}

		public WpfTextViewLineCollection(ITextSnapshot snapshot, IList<IWpfTextViewLine> lines) {
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			if (lines == null)
				throw new ArgumentNullException(nameof(lines));
			this.snapshot = snapshot;
			this.lines = new ReadOnlyCollection<IWpfTextViewLine>(lines);
			this.isValid = true;
			Debug.Assert(this.lines.Count > 0);
		}

		public IWpfTextViewLine this[int index] => lines[index];
		ITextViewLine IList<ITextViewLine>.this[int index] {
			get { return this[index]; }
			set { throw new NotSupportedException(); }
		}

		public int Count => lines.Count;
		public bool IsReadOnly => true;

		public ReadOnlyCollection<IWpfTextViewLine> WpfTextViewLines => lines;
		ITextViewLine ITextViewLineCollection.FirstVisibleLine => FirstVisibleLine;
		ITextViewLine ITextViewLineCollection.LastVisibleLine => LastVisibleLine;

		public IWpfTextViewLine FirstVisibleLine {
			get {
				foreach (var l in lines) {
					if (l.VisibilityState == VisibilityState.FullyVisible || l.VisibilityState == VisibilityState.PartiallyVisible)
						return l;
				}
				Debug.Fail("No visible line");
				return lines[0];
			}
		}

		public IWpfTextViewLine LastVisibleLine {
			get {
				for (int i = lines.Count - 1; i >= 0; i--) {
					var l = lines[i];
					if (l.VisibilityState == VisibilityState.FullyVisible || l.VisibilityState == VisibilityState.PartiallyVisible)
						return l;
				}
				Debug.Fail("No visible line");
				return lines[lines.Count - 1];
			}
		}

		public SnapshotSpan FormattedSpan {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public bool IsValid => isValid;
		bool isValid;

		internal void SetIsInvalid() {
			isValid = false;
		}

		public bool ContainsBufferPosition(SnapshotPoint bufferPosition) {
			throw new NotImplementedException();//TODO:
		}

		public TextBounds GetCharacterBounds(SnapshotPoint bufferPosition) {
			throw new NotImplementedException();//TODO:
		}

		public int GetIndexOfTextLine(ITextViewLine textLine) {
			throw new NotImplementedException();//TODO:
		}

		public Geometry GetLineMarkerGeometry(SnapshotSpan bufferSpan) {
			throw new NotImplementedException();//TODO:
		}

		public Geometry GetLineMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding) {
			throw new NotImplementedException();//TODO:
		}

		public Geometry GetMarkerGeometry(SnapshotSpan bufferSpan) {
			throw new NotImplementedException();//TODO:
		}

		public Geometry GetMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding) {
			throw new NotImplementedException();//TODO:
		}

		public Collection<TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan) {
			throw new NotImplementedException();//TODO:
		}

		public SnapshotSpan GetTextElementSpan(SnapshotPoint bufferPosition) {
			throw new NotImplementedException();//TODO:
		}

		public Geometry GetTextMarkerGeometry(SnapshotSpan bufferSpan) {
			throw new NotImplementedException();//TODO:
		}

		public Geometry GetTextMarkerGeometry(SnapshotSpan bufferSpan, bool clipToViewport, Thickness padding) {
			throw new NotImplementedException();//TODO:
		}

		ITextViewLine ITextViewLineCollection.GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition) =>
			GetTextViewLineContainingBufferPosition(bufferPosition);
		public IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition) {
			if (!IsValid)
				throw new ObjectDisposedException(nameof(WpfTextViewLineCollection));
			if (bufferPosition.Snapshot != snapshot)
				throw new ArgumentException();
			foreach (var line in WpfTextViewLines) {
				if (line.ContainsBufferPosition(bufferPosition))
					return line;
			}
			return null;
		}

		public ITextViewLine GetTextViewLineContainingYCoordinate(double y) {
			throw new NotImplementedException();//TODO:
		}

		public Collection<ITextViewLine> GetTextViewLinesIntersectingSpan(SnapshotSpan bufferSpan) {
			throw new NotImplementedException();//TODO:
		}

		public bool IntersectsBufferSpan(SnapshotSpan bufferSpan) {
			throw new NotImplementedException();//TODO:
		}

		public void CopyTo(ITextViewLine[] array, int arrayIndex) {
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
	}
}
