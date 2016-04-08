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

namespace dnSpy.AsmEditor.Module {
	enum ClrVersion {
		/// <summary>
		/// Unknown version
		/// </summary>
		Unknown,

		/// <summary>
		/// .NET Framework 1.0
		/// </summary>
		CLR10,

		/// <summary>
		/// .NET Framework 1.1
		/// </summary>
		CLR11,

		/// <summary>
		/// .NET Framework 2.0 - 3.5
		/// </summary>
		CLR20,

		/// <summary>
		/// .NET Framework 4 - 4.6
		/// </summary>
		CLR40,

		/// <summary>
		/// Default version we should use when initializing fields
		/// </summary>
		DefaultVersion = ClrVersion.CLR40,
	}
}
