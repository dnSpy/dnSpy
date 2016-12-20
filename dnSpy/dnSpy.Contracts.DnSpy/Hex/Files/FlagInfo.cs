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

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Flag info
	/// </summary>
	public struct FlagInfo {
		/// <summary>
		/// Name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Mask
		/// </summary>
		public ulong Mask { get; }

		/// <summary>
		/// Value
		/// </summary>
		public ulong Value { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="bitMask">Bit mask</param>
		public FlagInfo(string name, ulong bitMask)
			: this(name, bitMask, bitMask) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="mask">Mask</param>
		/// <param name="value">Value</param>
		public FlagInfo(string name, ulong mask, ulong value) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (mask == 0)
				throw new ArgumentOutOfRangeException(nameof(mask));
			if ((value & ~mask) != 0)
				throw new ArgumentOutOfRangeException(nameof(value));
			Name = name;
			Mask = mask;
			Value = value;
		}
	}
}
