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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Get property/event accessor options
	/// </summary>
	public enum DmdGetAccessorOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Return non-public accessors (doesn't include private accessors in base classes)
		/// </summary>
		NonPublic				= 0x00000001,

		/// <summary>
		/// Return all accessors, even if they're in a base class and private
		/// </summary>
		All						= 0x00000002,
	}
}
