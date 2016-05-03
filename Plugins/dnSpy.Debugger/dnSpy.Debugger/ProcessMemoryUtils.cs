/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dndbg.Engine;
using dnSpy.Debugger.Memory;
using dnSpy.Shared.HexEditor;

namespace dnSpy.Debugger {
	static class ProcessMemoryUtils {
		public static void ReadMemory(DnProcess process, ulong address, byte[] data, long index, int count) =>
			ReadMemory(process.CorProcess.Handle, address, data, index, count);

		public static void ReadMemory(IntPtr hProcess, ulong address, byte[] data, long index, int count) {
			var reader = new CachedHexStream(new ProcessHexStream(hProcess));
			reader.Read(address, data, index, count);
		}
	}
}
