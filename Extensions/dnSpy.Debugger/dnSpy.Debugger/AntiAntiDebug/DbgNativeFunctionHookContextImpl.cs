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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.AntiAntiDebug;

namespace dnSpy.Debugger.AntiAntiDebug {
	sealed class DbgNativeFunctionHookContextImpl : DbgNativeFunctionHookContext, IDisposable {
		public override DbgProcess Process { get; }

		public override DbgHookedNativeFunctionProvider FunctionProvider => functionProvider;
		readonly DbgHookedNativeFunctionProviderImpl functionProvider;

		internal ProcessMemoryBlockAllocator Allocator => processMemoryBlockAllocator;
		readonly ProcessMemoryBlockAllocator processMemoryBlockAllocator;

		public DbgNativeFunctionHookContextImpl(DbgProcess process) {
			Process = process ?? throw new ArgumentNullException(nameof(process));
			processMemoryBlockAllocator = new ProcessMemoryBlockAllocator(process);
			functionProvider = new DbgHookedNativeFunctionProviderImpl(process, processMemoryBlockAllocator);
		}

		public void Write() {
			processMemoryBlockAllocator.Write();
			functionProvider.Write();
		}

		public void Dispose() {
			processMemoryBlockAllocator.Dispose();
			functionProvider.Dispose();
		}
	}
}
