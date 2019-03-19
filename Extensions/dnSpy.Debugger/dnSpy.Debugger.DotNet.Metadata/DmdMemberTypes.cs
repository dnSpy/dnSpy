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
	/// Member types
	/// </summary>
	[Flags]
	public enum DmdMemberTypes {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		Constructor			= 0x00000001,
		Event				= 0x00000002,
		Field				= 0x00000004,
		Method				= 0x00000008,
		Property			= 0x00000010,
		TypeInfo			= 0x00000020,
		Custom				= 0x00000040,
		NestedType			= 0x00000080,
		All					= Constructor | Event | Field | Method | Property | TypeInfo | Custom | NestedType,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
