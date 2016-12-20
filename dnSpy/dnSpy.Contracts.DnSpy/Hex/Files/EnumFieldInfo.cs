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
	/// Enum field info
	/// </summary>
	public struct EnumFieldInfo {
		/// <summary>
		/// Gets the enum field value
		/// </summary>
		public ulong Value { get; }

		/// <summary>
		/// Gets the enum field name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Enum field info
		/// </summary>
		/// <param name="value">Enum field value</param>
		/// <param name="name">Enum field name</param>
		public EnumFieldInfo(ulong value, string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			Value = value;
			Name = name;
		}
	}
}
