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

// from dnlib

using System;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// MetaData token
	/// </summary>
	public readonly struct MDToken : IEquatable<MDToken>, IComparable<MDToken> {
		/// <summary>
		/// Mask to get the rid from a raw metadata token
		/// </summary>
		public const uint RID_MASK = 0x00FFFFFF;

		/// <summary>
		/// Max rid value
		/// </summary>
		public const uint RID_MAX = RID_MASK;

		/// <summary>
		/// Number of bits to right shift a raw metadata token to get the table index
		/// </summary>
		public const int TABLE_SHIFT = 24;

		readonly uint token;

		/// <summary>
		/// Returns the table type
		/// </summary>
		public Table Table => ToTable(token);

		/// <summary>
		/// Returns the row id
		/// </summary>
		public uint Rid => ToRID(token);

		/// <summary>
		/// Returns the raw token
		/// </summary>
		public uint Raw => token;

		/// <summary>
		/// Returns <c>true</c> if it's a <c>null</c> token
		/// </summary>
		public bool IsNull => Rid == 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="token">Raw token</param>
		public MDToken(uint token) => this.token = token;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="token">Raw token</param>
		public MDToken(int token)
			: this((uint)token) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="table">The table type</param>
		/// <param name="rid">Row id</param>
		public MDToken(Table table, uint rid)
			: this(((uint)table << TABLE_SHIFT) | rid) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="table">The table type</param>
		/// <param name="rid">Row id</param>
		public MDToken(Table table, int rid)
			: this(((uint)table << TABLE_SHIFT) | (uint)rid) {
		}

		/// <summary>
		/// Returns the <c>rid</c> (row ID)
		/// </summary>
		/// <param name="token">A raw metadata token</param>
		/// <returns>A <c>rid</c></returns>
		public static uint ToRID(uint token) => token & RID_MASK;

		/// <summary>
		/// Returns the <c>rid</c> (row ID)
		/// </summary>
		/// <param name="token">A raw metadata token</param>
		/// <returns>A <c>rid</c></returns>
		public static uint ToRID(int token) => ToRID((uint)token);

		/// <summary>
		/// Returns the <c>table</c>
		/// </summary>
		/// <param name="token">A raw metadata token</param>
		/// <returns>A metadata table index</returns>
		public static Table ToTable(uint token) => (Table)(token >> TABLE_SHIFT);

		/// <summary>
		/// Returns the <c>table</c>
		/// </summary>
		/// <param name="token">A raw metadata token</param>
		/// <returns>A metadata table index</returns>
		public static Table ToTable(int token) => ToTable((uint)token);

		/// <summary>
		/// Gets the token as a raw 32-bit signed integer
		/// </summary>
		public int ToInt32() => (int)token;

		/// <summary>
		/// Gets the token as a raw 32-bit unsigned integer
		/// </summary>
		public uint ToUInt32() => token;

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(MDToken left, MDToken right) {
			return left.CompareTo(right) == 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(MDToken left, MDToken right) {
			return left.CompareTo(right) != 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <(MDToken left, MDToken right) {
			return left.CompareTo(right) < 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >(MDToken left, MDToken right) {
			return left.CompareTo(right) > 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <=(MDToken left, MDToken right) {
			return left.CompareTo(right) <= 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >=(MDToken left, MDToken right) {
			return left.CompareTo(right) >= 0;
		}

		/// <inheritdoc/>
		public int CompareTo(MDToken other) => token.CompareTo(other.token);

		/// <inheritdoc/>
		public bool Equals(MDToken other) => CompareTo(other) == 0;

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if (!(obj is MDToken))
				return false;
			return Equals((MDToken)obj);
		}

		/// <inheritdoc/>
		public override int GetHashCode() => (int)token;

		/// <inheritdoc/>
		public override string ToString() => token.ToString("X8");
	}
}
