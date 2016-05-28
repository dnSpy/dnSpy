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
	/// Virtual <see cref="SnapshotPoint"/>
	/// </summary>
	public struct VirtualSnapshotPoint : IComparable<VirtualSnapshotPoint>, IEquatable<VirtualSnapshotPoint> {
		/// <summary>
		/// Gets the number of virtual spaces after the end of the line
		/// </summary>
		public int VirtualSpaces { get; }

		/// <summary>
		/// true if the caret is in the virtual space (after the end of the line)
		/// </summary>
		public bool IsInVirtualSpace => VirtualSpaces > 0;

		/// <summary>
		/// Gets the position
		/// </summary>
		public SnapshotPoint Position { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="position">Position</param>
		public VirtualSnapshotPoint(SnapshotPoint position) {
			if (position.Snapshot == null)
				throw new ArgumentException();
			Position = position;
			VirtualSpaces = 0;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <param name="position">Position</param>
		public VirtualSnapshotPoint(ITextSnapshot snapshot, int position) {
			Position = new SnapshotPoint(snapshot, position);
			VirtualSpaces = 0;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="virtualSpaces">Virtual spaces after end of line</param>
		public VirtualSnapshotPoint(SnapshotPoint position, int virtualSpaces) {
			if (position.Snapshot == null)
				throw new ArgumentException();
			if (virtualSpaces < 0)
				throw new ArgumentOutOfRangeException(nameof(virtualSpaces));
			Position = position;
			VirtualSpaces = virtualSpaces;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="line">Line</param>
		/// <param name="offset">Offset</param>
		public VirtualSnapshotPoint(ITextSnapshotLine line, int offset) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (offset < line.Length) {
				Position = line.Start + offset;
				VirtualSpaces = 0;
			}
			else {
				Position = line.End;
				VirtualSpaces = offset - line.Length;
			}
		}

		/// <summary>
		/// Translates this to <paramref name="snapshot"/>
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <returns></returns>
		public VirtualSnapshotPoint TranslateTo(ITextSnapshot snapshot) => TranslateTo(snapshot, PointTrackingMode.Positive);

		/// <summary>
		/// Translates this to <paramref name="snapshot"/>
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <param name="trackingMode">Tracking mode</param>
		/// <returns></returns>
		public VirtualSnapshotPoint TranslateTo(ITextSnapshot snapshot, PointTrackingMode trackingMode) {
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			if (snapshot == Position.Snapshot)
				return this;
			if (!IsInVirtualSpace)
				return new VirtualSnapshotPoint(Position.TranslateTo(snapshot, trackingMode));
			//TODO: Preserve VirtualSpaces if possible. If it's in a deleted span, clear VirtualSpaces
			return new VirtualSnapshotPoint(Position.TranslateTo(snapshot, trackingMode));
		}

		/// <summary>
		/// Compares this instance with <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public int CompareTo(VirtualSnapshotPoint other) {
			int c = Position.CompareTo(other.Position);
			if (c != 0)
				return c;
			return VirtualSpaces.CompareTo(other.VirtualSpaces);
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(VirtualSnapshotPoint left, VirtualSnapshotPoint right) => left.Equals(right);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(VirtualSnapshotPoint left, VirtualSnapshotPoint right) => !left.Equals(right);

		/// <summary>
		/// operator >()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator >(VirtualSnapshotPoint left, VirtualSnapshotPoint right) => left.CompareTo(right) > 0;

		/// <summary>
		/// operator >=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator >=(VirtualSnapshotPoint left, VirtualSnapshotPoint right) => left.CompareTo(right) >= 0;

		/// <summary>
		/// operator &lt;()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator <(VirtualSnapshotPoint left, VirtualSnapshotPoint right) => left.CompareTo(right) < 0;

		/// <summary>
		/// operator &lt;=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator <=(VirtualSnapshotPoint left, VirtualSnapshotPoint right) => left.CompareTo(right) <= 0;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(VirtualSnapshotPoint other) => Position.Equals(other.Position) && VirtualSpaces == other.VirtualSpaces;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is VirtualSnapshotPoint && Equals((VirtualSnapshotPoint)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => Position.GetHashCode() ^ VirtualSpaces;

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{Position}+{VirtualSpaces}";
	}
}
