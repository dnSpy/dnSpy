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
	/// Member binding flags
	/// </summary>
	[Flags]
	public enum DmdBindingFlags {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		Default					= 0,
		IgnoreCase				= 0x00000001,
		DeclaredOnly			= 0x00000002,
		Instance				= 0x00000004,
		Static					= 0x00000008,
		Public					= 0x00000010,
		NonPublic				= 0x00000020,
		FlattenHierarchy		= 0x00000040,
		InvokeMethod			= 0x00000100,
		CreateInstance			= 0x00000200,
		GetField				= 0x00000400,
		SetField				= 0x00000800,
		GetProperty				= 0x00001000,
		SetProperty				= 0x00002000,
		PutDispProperty			= 0x00004000,
		PutRefDispProperty		= 0x00008000,
		ExactBinding			= 0x00010000,
		SuppressChangeType		= 0x00020000,
		OptionalParamBinding	= 0x00040000,
		IgnoreReturn			= 0x01000000,
		Inaccessible			= int.MinValue,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
