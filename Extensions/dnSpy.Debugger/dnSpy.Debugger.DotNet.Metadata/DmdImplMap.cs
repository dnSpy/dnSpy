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
	[Flags]
	enum DmdPInvokeAttributes : ushort {
		NoMangle			= 0x0001,
		CharSetMask			= 0x0006,
		CharSetNotSpec		= 0x0000,
		CharSetAnsi			= 0x0002,
		CharSetUnicode		= 0x0004,
		CharSetAuto			= 0x0006,
		BestFitUseAssem		= 0x0000,
		BestFitEnabled		= 0x0010,
		BestFitDisabled		= 0x0020,
		BestFitMask			= 0x0030,
		ThrowOnUnmappableCharUseAssem	= 0x0000,
		ThrowOnUnmappableCharEnabled	= 0x1000,
		ThrowOnUnmappableCharDisabled	= 0x2000,
		ThrowOnUnmappableCharMask		= 0x3000,
		SupportsLastError	= 0x0040,
		CallConvMask		= 0x0700,
		CallConvWinapi		= 0x0100,
		CallConvCdecl		= 0x0200,
		CallConvStdcall		= 0x0300,
		CallConvStdCall		= CallConvStdcall,
		CallConvThiscall	= 0x0400,
		CallConvFastcall	= 0x0500,
	}

	readonly struct DmdImplMap {
		public DmdPInvokeAttributes Attributes { get; }
		public string Name { get; }
		public string Module { get; }
		public DmdImplMap(DmdPInvokeAttributes attributes, string name, string module) {
			Attributes = attributes;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Module = module ?? throw new ArgumentNullException(nameof(module));
		}
	}
}
