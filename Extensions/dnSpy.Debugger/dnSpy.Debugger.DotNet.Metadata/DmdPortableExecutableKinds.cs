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
	/// PE flags
	/// </summary>
	[Flags]
	public enum DmdPortableExecutableKinds {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		NotAPortableExecutableImage = 0,
		ILOnly						= 0x00000001,
		Required32Bit				= 0x00000002,
		PE32Plus					= 0x00000004,
		Unmanaged32Bit				= 0x00000008,
		Preferred32Bit				= 0x00000010,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
