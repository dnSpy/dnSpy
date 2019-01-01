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
using dnSpy.Contracts.Debugger.AntiAntiDebug;

namespace dnSpy.Debugger.AntiAntiDebug {
	sealed class DbgHookedNativeFunctionImpl : DbgHookedNativeFunction {
		public override ulong CurrentAddress => memBlock.CurrentAddress;
		public override ulong NewCodeAddress { get; }
		public override ulong NewFunctionAddress { get; }
		public override ulong OriginalFunctionAddress { get; }

		readonly ProcessMemoryBlock memBlock;

		public DbgHookedNativeFunctionImpl(ProcessMemoryBlock memBlock, ulong newFunctionAddress, ulong originalFunctionAddress) {
			this.memBlock = memBlock ?? throw new ArgumentNullException(nameof(memBlock));
			NewCodeAddress = memBlock.CurrentAddress;
			NewFunctionAddress = newFunctionAddress;
			OriginalFunctionAddress = originalFunctionAddress;
		}

		public override void WriteByte(byte value) => memBlock.WriteByte(value);
	}
}
