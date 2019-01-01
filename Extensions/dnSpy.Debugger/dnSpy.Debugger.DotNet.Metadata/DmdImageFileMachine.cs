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
	/// Machine
	/// </summary>
	public enum DmdImageFileMachine {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		Unknown		= 0,
		I386		= 0x014C,
		R3000		= 0x0162,
		R4000		= 0x0166,
		R10000		= 0x0168,
		WCEMIPSV2	= 0x0169,
		ALPHA		= 0x0184,
		SH3			= 0x01A2,
		SH3DSP		= 0x01A3,
		SH3E		= 0x01A4,
		SH4			= 0x01A6,
		SH5			= 0x01A8,
		ARM			= 0x01C0,
		THUMB		= 0x01C2,
		ARMNT		= 0x01C4,
		AM33		= 0x01D3,
		POWERPC		= 0x01F0,
		POWERPCFP	= 0x01F1,
		IA64		= 0x0200,
		MIPS16		= 0x0266,
		ALPHA64		= 0x0284,
		MIPSFPU		= 0x0366,
		MIPSFPU16	= 0x0466,
		TRICORE		= 0x0520,
		CEF			= 0x0CEF,
		EBC			= 0x0EBC,
		AMD64		= 0x8664,
		M32R		= 0x9041,
		ARM64		= 0xAA64,
		CEE			= 0xC0EE,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
