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

namespace dnSpy.Contracts.Debugger.DotNet.Code {
	/// <summary>
	/// IL offset mapping result. This enum is similar to <c>CorDebugMappingResult</c>
	/// </summary>
	public enum DbgILOffsetMapping {
		/// <summary>
		/// Unknown
		/// </summary>
		Unknown,

		/// <summary>
		/// The native code is in the prolog
		/// </summary>
		Prolog,

		/// <summary>
		/// The native code is in an epilog
		/// </summary>
		Epilog,

		/// <summary>
		/// Either the method maps exactly to MSIL code or the frame has been interpreted, so the value of the IP is accurate
		/// </summary>
		Exact,

		/// <summary>
		/// The method was successfully mapped, but the value of the IP may be approximate
		/// </summary>
		Approximate,

		/// <summary>
		/// No mapping information is available for the method
		/// </summary>
		NoInfo,

		/// <summary>
		/// Although there is mapping information for the method, the current address cannot be mapped to Microsoft intermediate language (MSIL) code
		/// </summary>
		UnmappedAddress,
	}
}
