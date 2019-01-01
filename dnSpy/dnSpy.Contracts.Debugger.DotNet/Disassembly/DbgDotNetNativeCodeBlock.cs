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
using dnSpy.Contracts.Disassembly;

namespace dnSpy.Contracts.Debugger.DotNet.Disassembly {
	/// <summary>
	/// A block of native code
	/// </summary>
	public readonly struct DbgDotNetNativeCodeBlock {
		/// <summary>
		/// Gets the kind
		/// </summary>
		public NativeCodeBlockKind Kind { get; }

		/// <summary>
		/// Gets the address of the code
		/// </summary>
		public ulong Address { get; }

		/// <summary>
		/// Gets the raw code
		/// </summary>
		public ArraySegment<byte> Code { get; }

		/// <summary>
		/// IL offset or -1 if unknown
		/// </summary>
		public int ILOffset { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="kind">Code kind</param>
		/// <param name="address">Address of block</param>
		/// <param name="code">Raw code</param>
		/// <param name="ilOffset">IL offset or -1 if unknown</param>
		public DbgDotNetNativeCodeBlock(NativeCodeBlockKind kind, ulong address, ArraySegment<byte> code, int ilOffset) {
			Kind = kind;
			Address = address;
			Code = code;
			ILOffset = ilOffset;
		}
	}
}
