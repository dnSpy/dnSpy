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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// An address reference
	/// </summary>
	public sealed class AddressReference : IEquatable<AddressReference> {
		/// <summary>
		/// Filename
		/// </summary>
		public string Filename { get; }

		/// <summary>
		/// true if <see cref="Address"/> is an RVA, false if it's a file offset
		/// </summary>
		public bool IsRVA { get; }

		/// <summary>
		/// Address, either an RVA or a file offset (<see cref="IsRVA"/>)
		/// </summary>
		public ulong Address { get; }

		/// <summary>
		/// Length of range
		/// </summary>
		public ulong Length { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="isRva">true if <paramref name="address"/> is an RVA, false if it's a file offset</param>
		/// <param name="address">Address</param>
		/// <param name="length">Length</param>
		public AddressReference(string filename, bool isRva, ulong address, ulong length) {
			Filename = filename ?? string.Empty;
			IsRVA = isRva;
			Address = address;
			Length = length;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(AddressReference other) {
			return other != null &&
				IsRVA == other.IsRVA &&
				Address == other.Address &&
				Length == other.Length &&
				StringComparer.OrdinalIgnoreCase.Equals(Filename, other.Filename);
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => Equals(obj as AddressReference);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			return StringComparer.OrdinalIgnoreCase.GetHashCode(Filename) ^
				(IsRVA ? 0 : int.MinValue) ^
				(int)Address ^ (int)(Address >> 32) ^
				(int)Length ^ (int)(Length >> 32);
		}
	}
}
