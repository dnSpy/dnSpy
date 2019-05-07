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
	/// Method implementation attributes
	/// </summary>
	[Flags]
	public enum DmdMethodImplAttributes : ushort {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		CodeTypeMask			= 0x0003,
		IL						= 0x0000,
		Native					= 0x0001,
		OPTIL					= 0x0002,
		Runtime					= 0x0003,
		ManagedMask				= 0x0004,
		Unmanaged				= 0x0004,
		Managed					= 0x0000,
		ForwardRef				= 0x0010,
		PreserveSig				= 0x0080,
		InternalCall			= 0x1000,
		Synchronized			= 0x0020,
		NoInlining				= 0x0008,
		AggressiveInlining		= 0x0100,
		NoOptimization			= 0x0040,
		AggressiveOptimization	= 0x0200,
		SecurityMitigations		= 0x0400,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
