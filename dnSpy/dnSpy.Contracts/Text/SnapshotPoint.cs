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
	/// <see cref="ITextSnapshot"/> point
	/// </summary>
	public struct SnapshotPoint : IEquatable<SnapshotPoint>, IComparable<SnapshotPoint> {
		/// <summary>
		/// Gets the snapshot
		/// </summary>
		public ITextSnapshot Snapshot { get; }

		/// <summary>
		/// Gets the position
		/// </summary>
		public int Position { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <param name="position">Position</param>
		public SnapshotPoint(ITextSnapshot snapshot, int position) {
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			if ((uint)position > snapshot.Length)
				throw new ArgumentOutOfRangeException(nameof(position));
			Snapshot = snapshot;
			Position = position;
		}

		/// <summary>
		/// Gets the character at <see cref="Position"/>
		/// </summary>
		/// <returns></returns>
		public char GetChar() => Snapshot[Position];

		/// <summary>
		/// Gets the line containing this instance
		/// </summary>
		/// <returns></returns>
		public ITextSnapshotLine GetContainingLine() => Snapshot.GetLineFromPosition(Position);

		/// <summary>
		/// Creates a new instance with the added offset
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public SnapshotPoint Add(int offset) => new SnapshotPoint(this.Snapshot, this.Position + offset);

		/// <summary>
		/// Creates a new instance with the subtracted offset
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public SnapshotPoint Subtract(int offset) => new SnapshotPoint(this.Snapshot, this.Position - offset);

		/// <summary>
		/// Gets the difference of <paramref name="other"/> with this
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public int Difference(SnapshotPoint other) {
			if (other.Snapshot != Snapshot)
				throw new ArgumentException();
			return other.Position - Position;
		}

		/// <summary>
		/// operator +()
		/// </summary>
		/// <param name="point"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		public static SnapshotPoint operator +(SnapshotPoint point, int offset) => new SnapshotPoint(point.Snapshot, point.Position + offset);

		/// <summary>
		/// operator -()
		/// </summary>
		/// <param name="point"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		public static SnapshotPoint operator -(SnapshotPoint point, int offset) => new SnapshotPoint(point.Snapshot, point.Position - offset);

		/// <summary>
		/// operator -()
		/// </summary>
		/// <param name="start"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static int operator -(SnapshotPoint start, SnapshotPoint other) {
			if (start.Snapshot != other.Snapshot)
				throw new ArgumentException();
			return start.Position - other.Position;
		}

		/// <summary>
		/// Compares this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public int CompareTo(SnapshotPoint other) {
			if (other.Snapshot != Snapshot)
				throw new ArgumentException();
			return Position - other.Position;
		}

		/// <summary>
		/// operator >()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator >(SnapshotPoint left, SnapshotPoint right) => left.CompareTo(right) > 0;

		/// <summary>
		/// operator >=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator >=(SnapshotPoint left, SnapshotPoint right) => left.CompareTo(right) >= 0;

		/// <summary>
		/// operator &lt;()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator <(SnapshotPoint left, SnapshotPoint right) => left.CompareTo(right) < 0;

		/// <summary>
		/// operator &lt;=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator <=(SnapshotPoint left, SnapshotPoint right) => left.CompareTo(right) <= 0;

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(SnapshotPoint left, SnapshotPoint right) => left.Snapshot == right.Snapshot && left.Position == right.Position;

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(SnapshotPoint left, SnapshotPoint right) => left.Snapshot != right.Snapshot || left.Position != right.Position;

		/// <summary>
		/// implicit operator <see cref="int"/>
		/// </summary>
		/// <param name="snapshotPoint"></param>
		public static implicit operator int(SnapshotPoint snapshotPoint) => snapshotPoint.Position;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(SnapshotPoint other) => Snapshot == other.Snapshot && Position == other.Position;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is SnapshotPoint && Equals((SnapshotPoint)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (Snapshot == null ? 0 : Snapshot.GetHashCode()) ^ Position;

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (Snapshot == null)
				return "<default>";
			return $"{Position}_'{(Position == Snapshot.Length ? "<end>" : Snapshot.GetText(Position, 1))}'";
		}
	}
}
