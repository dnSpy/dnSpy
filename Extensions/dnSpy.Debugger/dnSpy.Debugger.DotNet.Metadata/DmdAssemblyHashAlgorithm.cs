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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Assembly hash algorithm
	/// </summary>
	public enum DmdAssemblyHashAlgorithm : uint {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		None				= 0,
		MD5					= 0x8003,
		SHA1				= 0x8004,
		SHA256				= 0x800C,
		SHA384				= 0x800D,
		SHA512				= 0x800E,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
