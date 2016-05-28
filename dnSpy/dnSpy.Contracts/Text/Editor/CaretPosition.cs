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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Caret position
	/// </summary>
	public struct CaretPosition : IEquatable<CaretPosition> {
		/// <summary>
		/// Caret affinity
		/// </summary>
		public PositionAffinity Affinity { get; }

		/// <summary>
		/// Virtual buffer position
		/// </summary>
		public VirtualSnapshotPoint VirtualBufferPosition { get; }

		/// <summary>
		/// Buffer position
		/// </summary>
		public SnapshotPoint BufferPosition => VirtualBufferPosition.Position;

		/// <summary>
		/// Virtual spaces
		/// </summary>
		public int VirtualSpaces => VirtualBufferPosition.VirtualSpaces;

		/// <summary>
		/// Mapping point
		/// </summary>
		public IMappingPoint Point { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <param name="mappingPoint">Mapping point</param>
		/// <param name="caretAffinity">Caret affinity</param>
		public CaretPosition(VirtualSnapshotPoint bufferPosition, IMappingPoint mappingPoint, PositionAffinity caretAffinity) {
			if (mappingPoint == null)
				throw new ArgumentNullException(nameof(mappingPoint));
			VirtualBufferPosition = bufferPosition;
			Affinity = caretAffinity;
			Point = mappingPoint;
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(CaretPosition left, CaretPosition right) => left.Equals(right);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(CaretPosition left, CaretPosition right) => !left.Equals(right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(CaretPosition other) => VirtualBufferPosition.Equals(other.VirtualBufferPosition) && Affinity == other.Affinity;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is CaretPosition && Equals((CaretPosition)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => VirtualBufferPosition.GetHashCode() ^ ((int)Affinity).GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => Affinity == PositionAffinity.Predecessor ? $"|{VirtualBufferPosition}" : $"{VirtualBufferPosition}|";
	}
}
