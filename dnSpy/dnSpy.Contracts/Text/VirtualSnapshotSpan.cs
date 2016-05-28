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
	/// Virtual <see cref="SnapshotSpan"/>
	/// </summary>
	public struct VirtualSnapshotSpan : IEquatable<VirtualSnapshotSpan> {
		/// <summary>
		/// Gets the snapshot
		/// </summary>
		public ITextSnapshot Snapshot => Start.Position.Snapshot;

		/// <summary>
		/// Start position
		/// </summary>
		public VirtualSnapshotPoint Start { get; }

		/// <summary>
		/// End position
		/// </summary>
		public VirtualSnapshotPoint End { get; }

		/// <summary>
		/// Gets the <see cref="SnapshotSpan"/>
		/// </summary>
		public SnapshotSpan SnapshotSpan => new SnapshotSpan(Start.Position, End.Position);

		/// <summary>
		/// true if it's empty
		/// </summary>
		public bool IsEmpty => Start == End;

		/// <summary>
		/// true if it's <see cref="Start"/> or <see cref="End"/> is in virtual space
		/// </summary>
		public bool IsInVirtualSpace => Start.IsInVirtualSpace || End.IsInVirtualSpace;

		/// <summary>
		/// Length
		/// </summary>
		public int Length => End.Position.Position == Start.Position.Position ? End.VirtualSpaces - Start.VirtualSpaces : End.Position.Position - Start.Position.Position + End.VirtualSpaces;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="snapshotSpan">Span</param>
		public VirtualSnapshotSpan(SnapshotSpan snapshotSpan) {
			if (snapshotSpan.Snapshot == null)
				throw new ArgumentException();
			Start = new VirtualSnapshotPoint(snapshotSpan.Start);
			End = new VirtualSnapshotPoint(snapshotSpan.End);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Start</param>
		/// <param name="end">End</param>
		public VirtualSnapshotSpan(VirtualSnapshotPoint start, VirtualSnapshotPoint end) {
			if (start.Position.Snapshot == null || end.Position.Snapshot == null)
				throw new ArgumentException();
			if (start.Position.Snapshot != end.Position.Snapshot)
				throw new ArgumentException();
			if (start > end)
				throw new ArgumentOutOfRangeException(nameof(start));
			Start = start;
			End = end;
		}

		/// <summary>
		/// Gets the text
		/// </summary>
		/// <returns></returns>
		public string GetText() => SnapshotSpan.GetText();

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(VirtualSnapshotSpan left, VirtualSnapshotSpan right) => left.Equals(right);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(VirtualSnapshotSpan left, VirtualSnapshotSpan right) => !left.Equals(right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(VirtualSnapshotSpan other) => Start == other.Start && End == other.End;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is VirtualSnapshotSpan && Equals((VirtualSnapshotSpan)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => Start.GetHashCode() ^ End.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"({Start},{End})";
	}
}
