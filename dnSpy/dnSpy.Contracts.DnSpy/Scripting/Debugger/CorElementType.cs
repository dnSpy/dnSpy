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

using dnlib.DotNet;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Element type, identical to <see cref="ElementType"/>
	/// </summary>
	public enum CorElementType : byte {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		End			= 0x00,
		Void		= 0x01,
		Boolean		= 0x02,
		Char		= 0x03,
		I1			= 0x04,
		U1 			= 0x05,
		I2 			= 0x06,
		U2 			= 0x07,
		I4 			= 0x08,
		U4			= 0x09,
		I8			= 0x0A,
		U8			= 0x0B,
		R4			= 0x0C,
		R8			= 0x0D,
		String		= 0x0E,
		Ptr			= 0x0F,
		ByRef		= 0x10,
		ValueType	= 0x11,
		Class		= 0x12,
		Var			= 0x13,
		Array		= 0x14,
		GenericInst	= 0x15,
		TypedByRef	= 0x16,
		ValueArray	= 0x17,
		I			= 0x18,
		U			= 0x19,
		R			= 0x1A,
		FnPtr		= 0x1B,
		Object		= 0x1C,
		SZArray		= 0x1D,
		MVar		= 0x1E,
		CModReqd	= 0x1F,
		CModOpt		= 0x20,
		Internal	= 0x21,
		Module		= 0x3F,
		Sentinel	= 0x41,
		Pinned		= 0x45,
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
