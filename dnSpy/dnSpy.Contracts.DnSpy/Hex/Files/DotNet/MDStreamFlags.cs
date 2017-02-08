/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
	/// MDStream flags
	/// </summary>
	[Flags]
	public enum MDStreamFlags : byte {
		/// <summary>#Strings stream is big and requires 4 byte offsets</summary>
		BigStrings = 1,
		/// <summary>#GUID stream is big and requires 4 byte offsets</summary>
		BigGUID = 2,
		/// <summary>#Blob stream is big and requires 4 byte offsets</summary>
		BigBlob = 4,
		/// <summary/>
		Padding = 8,
		/// <summary/>
		DeltaOnly = 0x20,
		/// <summary>Extra data follows the row counts</summary>
		ExtraData = 0x40,
		/// <summary>Set if certain tables can contain deleted rows. The name column (if present) is set to "_Deleted"</summary>
		HasDelete = 0x80,
	}
}
