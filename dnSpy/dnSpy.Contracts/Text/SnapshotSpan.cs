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

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// <see cref="ITextSnapshot"/> span
	/// </summary>
	public struct SnapshotSpan : IEquatable<SnapshotSpan> {
		readonly int position;

		/// <summary>
		/// Gets the snapshot
		/// </summary>
		public ITextSnapshot Snapshot { get; }

		/// <summary>
		/// Gets the span
		/// </summary>
		public Span Span => new Span(position, Length);

		/// <summary>
		/// Gets the length
		/// </summary>
		public int Length { get; }

		/// <summary>
		/// Gets the start
		/// </summary>
		public SnapshotPoint Start => new SnapshotPoint(Snapshot, position);

		/// <summary>
		/// Gets the end
		/// </summary>
		public SnapshotPoint End => new SnapshotPoint(Snapshot, position + Length);

		/// <summary>
		/// true if it's empty
		/// </summary>
		public bool IsEmpty => Length == 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <param name="span">Span</param>
		public SnapshotSpan(ITextSnapshot snapshot, Span span) {
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			if (span.End > snapshot.Length)
				throw new ArgumentOutOfRangeException(nameof(span));
			Snapshot = snapshot;
			this.position = span.Start;
			Length = span.Length;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Start</param>
		/// <param name="end">End</param>
		public SnapshotSpan(SnapshotPoint start, SnapshotPoint end) {
			if (start.Snapshot == null || end.Snapshot == null || start.Snapshot != end.Snapshot)
				throw new ArgumentException();
			if (start.Position > end.Position)
				throw new ArgumentOutOfRangeException(nameof(start));
			Snapshot = start.Snapshot;
			this.position = start.Position;
			Length = end.Position - start.Position;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Start</param>
		/// <param name="length">Length</param>
		public SnapshotSpan(SnapshotPoint start, int length) {
			if (start.Snapshot == null)
				throw new ArgumentException();
			if ((uint)(start.Position + length) > (uint)start.Snapshot.Length)
				throw new ArgumentOutOfRangeException(nameof(start));
			Snapshot = start.Snapshot;
			this.position = start.Position;
			Length = length;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <param name="start">Start</param>
		/// <param name="length">Length</param>
		public SnapshotSpan(ITextSnapshot snapshot, int start, int length) {
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			if (start < 0)
				throw new ArgumentOutOfRangeException(nameof(start));
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(start));
			if ((uint)(start + length) > (uint)snapshot.Length)
				throw new ArgumentOutOfRangeException(nameof(start));
			Snapshot = snapshot;
			this.position = start;
			Length = length;
		}

		/// <summary>
		/// Returns true if <paramref name="position"/> lies within this span
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public bool Contains(int position) => Span.Contains(position);

		/// <summary>
		/// Returns true if <paramref name="point"/> lies within this span
		/// </summary>
		/// <param name="point">Point</param>
		/// <returns></returns>
		public bool Contains(SnapshotPoint point) {
			if (point.Snapshot != Snapshot)
				throw new ArgumentException();
			return Span.Contains(point.Position);
		}

		/// <summary>
		/// Returns true if <paramref name="simpleSpan"/> lies within this span
		/// </summary>
		/// <param name="simpleSpan">Span</param>
		/// <returns></returns>
		public bool Contains(Span simpleSpan) => Span.Contains(simpleSpan);

		/// <summary>
		/// Returns true if <paramref name="snapshotSpan"/> lies within this span
		/// </summary>
		/// <param name="snapshotSpan">Span</param>
		/// <returns></returns>
		public bool Contains(SnapshotSpan snapshotSpan) {
			if (snapshotSpan.Snapshot != Snapshot)
				throw new ArgumentException();
			return Span.Contains(snapshotSpan.Span);
		}

		/// <summary>
		/// Gets the text
		/// </summary>
		/// <returns></returns>
		public string GetText() => Snapshot.GetText(position, Length);

		/// <summary>
		/// Gets the intersection of this with <paramref name="simpleSpan"/>
		/// </summary>
		/// <param name="simpleSpan">Span</param>
		/// <returns></returns>
		public SnapshotSpan? Intersection(Span simpleSpan) {
			var span = Span.Intersection(simpleSpan);
			if (span != null)
				return new SnapshotSpan(Snapshot, span.Value);
			return null;
		}

		/// <summary>
		/// Returns the intersection of this with <paramref name="snapshotSpan"/>
		/// </summary>
		/// <param name="snapshotSpan">Span</param>
		/// <returns></returns>
		public SnapshotSpan? Intersection(SnapshotSpan snapshotSpan) {
			if (snapshotSpan.Snapshot != Snapshot)
				throw new ArgumentException();
			return Intersection(snapshotSpan.Span);
		}

		/// <summary>
		/// Returns true if this intersects with <paramref name="simpleSpan"/>
		/// </summary>
		/// <param name="simpleSpan">Span</param>
		/// <returns></returns>
		public bool IntersectsWith(Span simpleSpan) => Span.IntersectsWith(simpleSpan);

		/// <summary>
		/// Returns true if this intersects with <paramref name="snapshotSpan"/>
		/// </summary>
		/// <param name="snapshotSpan">Span</param>
		/// <returns></returns>
		public bool IntersectsWith(SnapshotSpan snapshotSpan) {
			if (snapshotSpan.Snapshot != Snapshot)
				throw new ArgumentException();
			return Span.IntersectsWith(snapshotSpan.Span);
		}

		/// <summary>
		/// Gets the overlap with <paramref name="simpleSpan"/>
		/// </summary>
		/// <param name="simpleSpan">Span</param>
		/// <returns></returns>
		public SnapshotSpan? Overlap(Span simpleSpan) {
			var span = Span.Overlap(simpleSpan);
			if (span != null)
				return new SnapshotSpan(Snapshot, span.Value);
			return null;
		}

		/// <summary>
		/// Gets the overlap with <paramref name="snapshotSpan"/>
		/// </summary>
		/// <param name="snapshotSpan">Span</param>
		/// <returns></returns>
		public SnapshotSpan? Overlap(SnapshotSpan snapshotSpan) {
			if (snapshotSpan.Snapshot != Snapshot)
				throw new ArgumentException();
			var span = Span.Overlap(snapshotSpan.Span);
			if (span != null)
				return new SnapshotSpan(Snapshot, span.Value);
			return null;
		}

		/// <summary>
		/// Returns true if this overlaps with <paramref name="simpleSpan"/>
		/// </summary>
		/// <param name="simpleSpan">Span</param>
		/// <returns></returns>
		public bool OverlapsWith(Span simpleSpan) => Span.OverlapsWith(simpleSpan);

		/// <summary>
		/// Returns true if this overlaps with <paramref name="snapshotSpan"/>
		/// </summary>
		/// <param name="snapshotSpan">Span</param>
		/// <returns></returns>
		public bool OverlapsWith(SnapshotSpan snapshotSpan) {
			if (snapshotSpan.Snapshot != Snapshot)
				throw new ArgumentException();
			return Span.OverlapsWith(snapshotSpan.Span);
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(SnapshotSpan left, SnapshotSpan right) => left.Snapshot == right.Snapshot && left.position == right.position && left.Length == right.Length;

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(SnapshotSpan left, SnapshotSpan right) => left.Snapshot != right.Snapshot || left.position != right.position || left.Length != right.Length;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(SnapshotSpan other) => Snapshot == other.Snapshot && position == other.position && Length == other.Length;

		/// <summary>
		/// implicit operator <see cref="Span"/>
		/// </summary>
		/// <param name="snapshotSpan"></param>
		public static implicit operator Span(SnapshotSpan snapshotSpan) => snapshotSpan.Span;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is SnapshotSpan && Equals((SnapshotSpan)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (Snapshot == null ? 0 : Snapshot.GetHashCode()) ^ position ^ Length;

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (Snapshot == null)
				return "<default>";
			const int MAXLEN = 40;
			return $"{Span.ToString()}_'{(Length <= MAXLEN ? GetText() : Snapshot.GetText(position, MAXLEN))}'";
		}
	}
}
