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

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Debug mapping result
	/// </summary>
	[Flags]
	public enum MappingResult {
		// IMPORTANT: Must be identical to dndbg.COM.CorDebug.CorDebugMappingResult (enum field names may be different)

		/// <summary>
		/// The native code is in the prolog, so the value of the IP is 0.
		/// </summary>
		Prolog = 1,
		/// <summary>
		/// The native code is in an epilog, so the value of the IP is the address of the last instruction of the method.
		/// </summary>
		Epilog,
		/// <summary>
		/// No mapping information is available for the method, so the value of the IP is 0.
		/// </summary>
		NoInfo = 4,
		/// <summary>
		/// Although there is mapping information for the method, the current address cannot be mapped to Microsoft intermediate language (MSIL) code. The value of the IP is 0.
		/// </summary>
		UnmappedAddress = 8,
		/// <summary>
		/// Either the method maps exactly to MSIL code or the frame has been interpreted, so the value of the IP is accurate.
		/// </summary>
		Exact = 16,
		/// <summary>
		/// The method was successfully mapped, but the value of the IP may be approximate.
		/// </summary>
		Approximate = 32
	}
}
