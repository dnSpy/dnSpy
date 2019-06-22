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

namespace dnSpy.Contracts.Disassembly {
	/// <summary>
	/// Extra x86 (16/32/64-bit) info that can be used by a disassembler to show more info to the user
	/// </summary>
	public sealed class X86NativeCodeInfo : NativeCodeInfo {
		/// <summary>
		/// All known variables. Can be empty.
		/// </summary>
		public X86Variable[] Variables { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="variables">Variables or null</param>
		public X86NativeCodeInfo(X86Variable[]? variables) =>
			Variables = variables ?? Array.Empty<X86Variable>();
	}
}
