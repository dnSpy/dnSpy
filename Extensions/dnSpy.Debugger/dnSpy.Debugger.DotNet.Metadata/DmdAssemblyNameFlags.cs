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
	/// Assembly name flags
	/// </summary>
	[Flags]
	public enum DmdAssemblyNameFlags {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		None						= 0,
		PublicKey					= 1,
		PA_None						= 0x0000,
		PA_MSIL						= 0x0010,
		PA_x86						= 0x0020,
		PA_IA64						= 0x0030,
		PA_AMD64					= 0x0040,
		PA_ARM						= 0x0050,
		PA_NoPlatform				= 0x0070,
		PA_Specified				= 0x0080,
		PA_Mask						= 0x0070,
		PA_FullMask					= 0x00F0,
		PA_Shift					= 0x0004,
		EnableJITcompileTracking	= 0x8000,
		DisableJITcompileOptimizer	= 0x4000,
		Retargetable				= 0x0100,
		ContentType_Default			= 0x0000,
		ContentType_WindowsRuntime	= 0x0200,
		ContentType_Mask			= 0x0E00,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
