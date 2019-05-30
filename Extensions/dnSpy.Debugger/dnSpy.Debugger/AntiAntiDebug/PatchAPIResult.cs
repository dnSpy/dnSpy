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

namespace dnSpy.Debugger.AntiAntiDebug {
	readonly struct PatchAPIResult {
		public readonly string? ErrorMessage;
		public readonly ProcessMemoryBlock? Block;
		public readonly ulong NewFunctionAddress;
		public readonly SimpleAPIPatch SimplePatch;

		public PatchAPIResult(string errorMessage) {
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
			Block = null;
			NewFunctionAddress = 0;
			SimplePatch = default;
		}

		public PatchAPIResult(ProcessMemoryBlock block, ulong newFunctionAddress, SimpleAPIPatch simplePatch) {
			if (simplePatch.Data is null)
				throw new ArgumentException();
			ErrorMessage = null;
			Block = block ?? throw new ArgumentNullException(nameof(block));
			NewFunctionAddress = newFunctionAddress;
			SimplePatch = simplePatch;
		}
	}

	readonly struct SimpleAPIPatch {
		public readonly ulong Address;
		public readonly byte[] Data;

		public SimpleAPIPatch(ulong address, byte[] data) {
			Address = address;
			Data = data ?? throw new ArgumentNullException(nameof(data));
		}
	}
}
