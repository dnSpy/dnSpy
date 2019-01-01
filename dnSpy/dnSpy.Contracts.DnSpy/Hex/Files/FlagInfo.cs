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

using System;

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Flag info
	/// </summary>
	public readonly struct FlagInfo {
		const ulong EnumNameValue = ulong.MaxValue;

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
		/// true if it only stores the name of the enum and the enum mask. <see cref="Value"/> is not used
		/// and should be ignored.
		/// </summary>
		public bool IsEnumName => (Value & ~Mask) != 0 && Value == EnumNameValue;

		/// <summary>
		/// Creates an instance that only holds the name of the embedded enum value
		/// </summary>
		/// <param name="mask">Enum mask</param>
		/// <param name="name">Name of enum</param>
		/// <returns></returns>
		public static FlagInfo CreateEnumName(ulong mask, string name) => new FlagInfo(mask, name, false);

		FlagInfo(ulong mask, string name, bool dummy) {
			if (mask == 0)
				throw new ArgumentOutOfRangeException(nameof(mask));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Mask = mask;
			Value = EnumNameValue;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bitMask">Bit mask</param>
		/// <param name="name">Name</param>
		public FlagInfo(ulong bitMask, string name)
			: this(bitMask, bitMask, name) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mask">Mask</param>
		/// <param name="value">Value</param>
		/// <param name="name">Name</param>
		public FlagInfo(ulong mask, ulong value, string name) {
			if (mask == 0)
				throw new ArgumentOutOfRangeException(nameof(mask));
			if ((value & ~mask) != 0)
				throw new ArgumentOutOfRangeException(nameof(value));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Mask = mask;
			Value = value;
		}
	}
}
