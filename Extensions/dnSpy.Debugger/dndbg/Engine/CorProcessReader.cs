/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dndbg.DotNet;

namespace dndbg.Engine {
	public sealed class CorProcessReader : IProcessReader {
		readonly CorProcess process;

		public CorProcessReader(DnProcess process)
			: this(process.CorProcess) {
		}

		public CorProcessReader(CorProcess process) {
			this.process = process ?? throw new ArgumentNullException(nameof(process));
		}

		public int ReadBytes(ulong address, byte[] data, int index, int count) {
			int hr = process.ReadMemory(address, data, index, count, out int sizeRead);
			return sizeRead;
		}
	}
}
