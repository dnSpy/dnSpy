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
	/// Type attributes
	/// </summary>
	[Flags]
	public enum DmdTypeAttributes : uint {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		VisibilityMask			= 0x00000007,
		NotPublic				= 0x00000000,
		Public					= 0x00000001,
		NestedPublic			= 0x00000002,
		NestedPrivate			= 0x00000003,
		NestedFamily			= 0x00000004,
		NestedAssembly			= 0x00000005,
		NestedFamANDAssem		= 0x00000006,
		NestedFamORAssem		= 0x00000007,
		LayoutMask				= 0x00000018,
		AutoLayout				= 0x00000000,
		SequentialLayout		= 0x00000008,
		ExplicitLayout			= 0x00000010,
		ClassSemanticsMask		= 0x00000020,
		Class					= 0x00000000,
		Interface				= 0x00000020,
		Abstract				= 0x00000080,
		Sealed					= 0x00000100,
		SpecialName				= 0x00000400,
		Import					= 0x00001000,
		Serializable			= 0x00002000,
		WindowsRuntime			= 0x00004000,
		StringFormatMask		= 0x00030000,
		AnsiClass				= 0x00000000,
		UnicodeClass			= 0x00010000,
		AutoClass				= 0x00020000,
		CustomFormatClass		= 0x00030000,
		CustomFormatMask		= 0x00C00000,
		BeforeFieldInit			= 0x00100000,
		Forwarder				= 0x00200000,
		ReservedMask			= 0x00040800,
		RTSpecialName			= 0x00000800,
		HasSecurity				= 0x00040000,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
