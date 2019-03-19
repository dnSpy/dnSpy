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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Signature calling convention flags
	/// </summary>
	[Flags]
	public enum DmdSignatureCallingConvention : byte {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		Default			= 0x00,
		C				= 0x01,
		StdCall			= 0x02,
		ThisCall		= 0x03,
		FastCall		= 0x04,
		VarArg			= 0x05,
		Field			= 0x06,
		LocalSig		= 0x07,
		Property		= 0x08,
		Unmanaged		= 0x09,
		GenericInst		= 0x0A,
		NativeVarArg	= 0x0B,
		Mask			= 0x0F,
		Generic			= 0x10,
		HasThis			= 0x20,
		ExplicitThis	= 0x40,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
