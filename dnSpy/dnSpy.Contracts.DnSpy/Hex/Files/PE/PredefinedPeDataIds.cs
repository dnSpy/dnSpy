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

namespace dnSpy.Contracts.Hex.Files.PE {
	/// <summary>
	/// Predefined PE <see cref="BufferData"/> ids
	/// </summary>
	public static class PredefinedPeDataIds {
		/// <summary><see cref="PeDosHeaderData"/></summary>
		public const string PeDosHeader = nameof(PeDosHeader);

		/// <summary><see cref="PeFileHeaderData"/></summary>
		public const string PeFileHeader = nameof(PeFileHeader);

		/// <summary><see cref="PeOptionalHeaderData"/></summary>
		public const string PeOptionalHeader = nameof(PeOptionalHeader);

		/// <summary><see cref="PeSectionsData"/></summary>
		public const string PeSections = nameof(PeSections);
	}
}
