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
	/// Method attributes
	/// </summary>
	[Flags]
	public enum DmdMethodAttributes : ushort {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		MemberAccessMask	= 0x0007,
		PrivateScope		= 0x0000,
		Private				= 0x0001,
		FamANDAssem			= 0x0002,
		Assembly			= 0x0003,
		Family				= 0x0004,
		FamORAssem			= 0x0005,
		Public				= 0x0006,
		Static				= 0x0010,
		Final				= 0x0020,
		Virtual				= 0x0040,
		HideBySig			= 0x0080,
		VtableLayoutMask	= 0x0100,
		ReuseSlot			= 0x0000,
		NewSlot				= 0x0100,
		CheckAccessOnOverride = 0x0200,
		Abstract			= 0x0400,
		SpecialName			= 0x0800,
		PinvokeImpl			= 0x2000,
		UnmanagedExport		= 0x0008,
		RTSpecialName		= 0x1000,
		HasSecurity			= 0x4000,
		RequireSecObject	= 0x8000,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
