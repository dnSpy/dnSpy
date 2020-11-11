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
using System.Diagnostics;

namespace dnSpy.AsmEditor.Compiler {
	abstract unsafe class RawModuleBytes {
		public abstract void* Pointer { get; }
		public abstract int Size { get; }
		public abstract bool IsFileLayout { get; }

		~RawModuleBytes() {
			Debug.Assert(Environment.HasShutdownStarted);
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected abstract void Dispose(bool disposing);
	}

	sealed unsafe class NativeMemoryRawModuleBytes : RawModuleBytes {
		public override unsafe void* Pointer => pointer;
		public override int Size => size;
		public override bool IsFileLayout { get; }

		void* pointer;
		int size;

		public NativeMemoryRawModuleBytes(int size, bool isFileLayout) {
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			pointer = size == 0 ? null : NativeMemoryAllocator.Allocate(size);
			this.size = size;
			IsFileLayout = isFileLayout;
		}

		protected override void Dispose(bool disposing) {
			if (pointer is not null) {
				NativeMemoryAllocator.Free(pointer, size);
				pointer = null;
				size = 0;
			}
		}
	}
}
