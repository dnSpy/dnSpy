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
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Metadata.Internal;

namespace dnSpy.Debugger.DotNet.Metadata.Internal {
	sealed class DbgRawMetadataImpl : DbgRawMetadata {
		public override bool IsFileLayout {
			get {
				if (disposed)
					throw new ObjectDisposedException(nameof(DbgRawMetadataImpl));
				return isFileLayout;
			}
		}

		public override IntPtr Address {
			get {
				if (disposed)
					throw new ObjectDisposedException(nameof(DbgRawMetadataImpl));
				return address;
			}
		}

		public override int Size {
			get {
				if (disposed)
					throw new ObjectDisposedException(nameof(DbgRawMetadataImpl));
				return size;
			}
		}

		readonly bool isFileLayout;
		readonly IntPtr address;
		readonly int size;
		readonly object lockObj;
		volatile int referenceCounter;
		volatile bool disposed;

		public unsafe DbgRawMetadataImpl(DbgProcess process, bool isFileLayout, ulong moduleAddress, int moduleSize) {
			lockObj = new object();
			referenceCounter = 1;
			this.isFileLayout = isFileLayout;
			size = moduleSize;

			try {
				// Prevent allocation on the LOH. We'll also be able to free the memory as soon as it's not needed.
				address = NativeMethods.VirtualAlloc(IntPtr.Zero, new IntPtr(moduleSize), NativeMethods.MEM_COMMIT, NativeMethods.PAGE_READWRITE);
				if (address == IntPtr.Zero)
					throw new OutOfMemoryException();
				process.ReadMemory(moduleAddress, (byte*)address.ToPointer(), size);
			}
			catch {
				Dispose();
				throw;
			}
		}

		~DbgRawMetadataImpl() {
			Debug.Assert(Environment.HasShutdownStarted, nameof(DbgRawMetadataImpl) + " dtor called!");
			Dispose();
		}

		internal DbgRawMetadata TryAddRef() {
			lock (lockObj) {
				if (disposed)
					return null;
				referenceCounter++;
				return this;
			}
		}

		public override DbgRawMetadata AddRef() {
			if (disposed)
				throw new ObjectDisposedException(nameof(DbgRawMetadataImpl));
			lock (lockObj)
				referenceCounter++;
			return this;
		}

		public override void Release() {
			if (disposed)
				throw new ObjectDisposedException(nameof(DbgRawMetadataImpl));
			bool dispose;
			lock (lockObj)
				dispose = --referenceCounter == 0;
			if (dispose)
				Dispose();
		}

		void Dispose() {
			lock (lockObj) {
				if (disposed)
					return;
				disposed = true;
			}
			GC.SuppressFinalize(this);
			if (address != IntPtr.Zero) {
				bool b = NativeMethods.VirtualFree(address, IntPtr.Zero, NativeMethods.MEM_RELEASE);
				Debug.Assert(b);
			}
		}
	}
}
