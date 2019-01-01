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

using dnSpy.Contracts.Debugger;

namespace dnSpy.Debugger.AntiAntiDebug {
	abstract class ProcessMemoryBlock {
		public abstract ulong CurrentAddress { get; }
		public abstract void WriteByte(byte value);
	}

	sealed class ProcessMemoryBlockImpl : ProcessMemoryBlock {
		public override ulong CurrentAddress => address + (uint)currentIndex;
		internal ulong EndAddress => address + (uint)size;

		readonly ulong address;
		readonly int size;
		readonly byte[] data;
		int currentIndex;

		public ProcessMemoryBlockImpl(ulong address, int size) {
			this.address = address;
			this.size = size;
			data = new byte[size];
		}

		public override void WriteByte(byte value) {
			if (currentIndex >= data.Length)
				throw new DbgHookException("Can't write more data");
			data[currentIndex] = value;
			currentIndex++;
		}

		internal void WriteTo(DbgProcess process) => process.WriteMemory(address, data, 0, currentIndex);
	}
}
