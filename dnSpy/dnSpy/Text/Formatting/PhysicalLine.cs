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
using System.Collections.ObjectModel;
using System.Diagnostics;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Formatting {
	/// <summary>
	/// Contains one or more visual lines. One physical line can have more than one visual line if
	/// word wrap is enabled or if the line was split up for some other reason, eg. it is too long.
	/// </summary>
	sealed class PhysicalLine {
		public Collection<IFormattedLine> Lines { get; }
		public SnapshotSpan BufferSpan { get; private set; }
		public bool IsLastLine { get; private set; }

		public PhysicalLine(ITextSnapshotLine snapshotLine, Collection<IFormattedLine> lines) {
			if (lines == null)
				throw new ArgumentNullException(nameof(lines));
			if (lines.Count == 0)
				throw new ArgumentException();
			if (snapshotLine.Snapshot != lines[0].Snapshot)
				throw new ArgumentException();
			Lines = lines;
			BufferSpan = snapshotLine.ExtentIncludingLineBreak;
			IsLastLine = snapshotLine.LineNumber + 1 == snapshotLine.Snapshot.LineCount;
		}

		public bool Contains(SnapshotPoint point) {
			if (disposed)
				throw new ObjectDisposedException(nameof(PhysicalLine));
			if (point.Snapshot == null)
				throw new ArgumentException();
			if (point.Snapshot != BufferSpan.Snapshot)
				return false;
			if (point < BufferSpan.Start)
				return false;
			if (IsLastLine)
				return point <= BufferSpan.End;
			return point < BufferSpan.End;
		}

		public IFormattedLine FindFormattedLineByBufferPosition(SnapshotPoint point) {
			if (disposed)
				throw new ObjectDisposedException(nameof(PhysicalLine));
			if (point.Snapshot == null)
				throw new ArgumentException();
			if (point.Snapshot != BufferSpan.Snapshot)
				return null;
			if (!Contains(point))
				return null;
			foreach (var line in Lines) {
				if (point <= line.Start || line.ContainsBufferPosition(point))
					return line;
			}
			return Lines[Lines.Count - 1];
		}

		public bool OverlapsWith(NormalizedSnapshotSpanCollection regions) {
			if (disposed)
				throw new ObjectDisposedException(nameof(PhysicalLine));
			if (regions.Count == 0)
				return false;
			if (BufferSpan.Snapshot != regions[0].Snapshot)
				throw new ArgumentException();
			foreach (var r in regions) {
				if (r.OverlapsWith(BufferSpan))
					return true;
			}
			return false;
		}

		public void Dispose() {
			if (disposed)
				return;
			disposed = true;
			foreach (var l in Lines)
				l.Dispose();
		}
		bool disposed;

		public void UpdateIsLastLine() {
			var snapshotLine = BufferSpan.Start.GetContainingLine();
			Debug.Assert(snapshotLine.ExtentIncludingLineBreak == BufferSpan);
			IsLastLine = snapshotLine.LineNumber + 1 == snapshotLine.Snapshot.LineCount;
		}

		public bool TranslateTo(ITextSnapshot visualSnapshot, ITextSnapshot editSnapshot) {
			if (disposed)
				throw new ObjectDisposedException(nameof(PhysicalLine));

			var newSpan = BufferSpan.TranslateTo(editSnapshot, SpanTrackingMode.EdgeExclusive);
			bool hasChanges = HasChanges(BufferSpan.Snapshot, newSpan.Snapshot);
			BufferSpan = newSpan;
			return hasChanges;
		}

		bool HasChanges(ITextSnapshot oldSnapshot, ITextSnapshot newSnapshot) {
			if (oldSnapshot == newSnapshot)
				return false;
			var span = BufferSpan.Span;
			var oldVer = oldSnapshot.Version;
			var newVer = newSnapshot.Version;
			while (oldVer != newVer) {
				foreach (var c in oldVer.Changes) {
					bool change = span.IntersectsWith(c.OldSpan);
					if (change)
						return true;
				}
				span = Tracking.TrackSpanForwardInTime(SpanTrackingMode.EdgePositive, span, oldVer, oldVer.Next);
				oldVer = oldVer.Next;
			}
			return false;
		}

		public void TranslateLinesTo(ITextSnapshot visualSnapshot, ITextSnapshot editSnapshot) {
			if (disposed)
				throw new ObjectDisposedException(nameof(PhysicalLine));
			if (editSnapshot != BufferSpan.Snapshot)
				throw new ArgumentException();
			foreach (var line in Lines) {
#if DEBUG
				var oldText = line.ExtentIncludingLineBreak.GetText();
#endif
				line.SetSnapshot(visualSnapshot, editSnapshot);
#if DEBUG
				// If it doesn't have the same contents, it should've been invalidated
				Debug.Assert(oldText == line.ExtentIncludingLineBreak.GetText());
#endif
			}
		}
	}
}
