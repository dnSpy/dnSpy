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
	/// A block of native code
	/// </summary>
	public readonly struct NativeCodeBlock {
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
		/// Block comment or null. It can contain multiple lines
		/// </summary>
		public string? Comment { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="kind">Code kind</param>
		/// <param name="address">Address of block</param>
		/// <param name="code">Raw code</param>
		/// <param name="comment">Block comment or null. It can contain multiple lines</param>
		public NativeCodeBlock(NativeCodeBlockKind kind, ulong address, ArraySegment<byte> code, string? comment) {
			Kind = kind;
			Address = address;
			Code = code;
			Comment = comment;
		}
	}
}
