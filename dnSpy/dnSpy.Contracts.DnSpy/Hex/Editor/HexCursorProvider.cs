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
using System.Windows.Input;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Creates <see cref="HexCursorProvider"/>s
	/// </summary>
	public abstract class HexCursorProviderFactory {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexCursorProviderFactory() { }

		/// <summary>
		/// Creates a <see cref="HexCursorProvider"/> instance or returns null
		/// </summary>
		/// <param name="wpfHexView">Hex view</param>
		/// <returns></returns>
		public abstract HexCursorProvider Create(WpfHexView wpfHexView);
	}

	/// <summary>
	/// Cursor priorities
	/// </summary>
	public static class PredefinedHexCursorPriorities {
		/// <summary>
		/// Low priority
		/// </summary>
		public static readonly double Low = -100000;

		/// <summary>
		/// Normal priority
		/// </summary>
		public static readonly double Normal = 0;

		/// <summary>
		/// High priority
		/// </summary>
		public static readonly double High = 100000;

		/// <summary>
		/// Priority of the offset cursor (hand)
		/// </summary>
		public static readonly double Offset = High;
	}

	/// <summary>
	/// Cursor info
	/// </summary>
	public struct HexCursorInfo : IEquatable<HexCursorInfo> {
		/// <summary>
		/// Gets the cursor or null
		/// </summary>
		public Cursor Cursor { get; }

		/// <summary>
		/// Gets the priority, eg. <see cref="PredefinedHexCursorPriorities.High"/>. The highest priority cursor is used.
		/// </summary>
		public double Priority { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cursor">Cursor or null</param>
		/// <param name="priority">Priority, eg. <see cref="PredefinedHexCursorPriorities.High"/>. The highest priority cursor is used</param>
		public HexCursorInfo(Cursor cursor, double priority) {
			Cursor = cursor;
			Priority = priority;
		}

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(HexCursorInfo left, HexCursorInfo right) => left.Equals(right);
		public static bool operator !=(HexCursorInfo left, HexCursorInfo right) => !left.Equals(right);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(HexCursorInfo other) => Cursor == other.Cursor && Priority == other.Priority;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is HexCursorInfo && Equals((HexCursorInfo)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (Cursor?.GetHashCode() ?? 0) ^ Priority.GetHashCode();
	}

	/// <summary>
	/// Hex editor <see cref="Cursor"/> provider
	/// </summary>
	public abstract class HexCursorProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexCursorProvider() { }

		/// <summary>
		/// Raised after <see cref="CursorInfo"/> is changed
		/// </summary>
		public abstract event EventHandler CursorInfoChanged;

		/// <summary>
		/// Gets the cursor and priority
		/// </summary>
		public abstract HexCursorInfo CursorInfo { get; }
	}
}
