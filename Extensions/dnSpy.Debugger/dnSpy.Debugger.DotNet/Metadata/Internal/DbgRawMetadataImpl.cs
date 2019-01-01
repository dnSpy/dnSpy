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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.PE;
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

		public override IntPtr MetadataAddress {
			get {
				if (disposed)
					throw new ObjectDisposedException(nameof(DbgRawMetadataImpl));
				return metadataAddress;
			}
		}

		public override int MetadataSize {
			get {
				if (disposed)
					throw new ObjectDisposedException(nameof(DbgRawMetadataImpl));
				return metadataSize;
			}
		}

		readonly bool isFileLayout;
		readonly IntPtr address;
		readonly int size;
		readonly IntPtr metadataAddress;
		readonly int metadataSize;
		readonly object lockObj;
		GCHandle moduleBytesHandle;
		readonly DbgProcess process;
		readonly ulong moduleAddress;
		volatile int referenceCounter;
		volatile bool disposed;
		volatile int freedAddress;

		public DbgRawMetadataImpl(byte[] moduleBytes, bool isFileLayout) {
			lockObj = new object();
			referenceCounter = 1;
			this.isFileLayout = isFileLayout;
			size = moduleBytes.Length;
			moduleBytesHandle = GCHandle.Alloc(moduleBytes, GCHandleType.Pinned);
			address = moduleBytesHandle.AddrOfPinnedObject();
			(metadataAddress, metadataSize) = GetMetadataInfo();
		}

		public unsafe DbgRawMetadataImpl(DbgProcess process, bool isFileLayout, ulong moduleAddress, int moduleSize) {
			lockObj = new object();
			referenceCounter = 1;
			this.isFileLayout = isFileLayout;
			size = moduleSize;
			this.process = process;
			this.moduleAddress = moduleAddress;

			try {
				// Prevent allocation on the LOH. We'll also be able to free the memory as soon as it's not needed.
				address = NativeMethods.VirtualAlloc(IntPtr.Zero, new IntPtr(moduleSize), NativeMethods.MEM_COMMIT, NativeMethods.PAGE_READWRITE);
				if (address == IntPtr.Zero)
					throw new OutOfMemoryException();
				process.ReadMemory(moduleAddress, address.ToPointer(), size);
				(metadataAddress, metadataSize) = GetMetadataInfo();
			}
			catch {
				Dispose();
				throw;
			}
		}

		unsafe (IntPtr metadataAddress, int metadataSize) GetMetadataInfo() {
			try {
				var peImage = new PEImage(address, (uint)size, isFileLayout ? ImageLayout.File : ImageLayout.Memory, true);
				var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
				if (dotNetDir.VirtualAddress != 0 && dotNetDir.Size >= 0x48) {
					var cor20Reader = peImage.CreateReader(dotNetDir.VirtualAddress, 0x48);
					var cor20 = new ImageCor20Header(ref cor20Reader, true);
					var mdStart = (long)peImage.ToFileOffset(cor20.Metadata.VirtualAddress);
					var mdAddr = new IntPtr((byte*)address + mdStart);
					var mdSize = (int)cor20.Metadata.Size;
					return (mdAddr, mdSize);
				}
			}
			catch (Exception ex) when (ex is IOException || ex is BadImageFormatException) {
				Debug.Fail("Couldn't read .NET metadata");
			}
			return (IntPtr.Zero, 0);
		}

		~DbgRawMetadataImpl() {
			Debug.Assert(Environment.HasShutdownStarted, nameof(DbgRawMetadataImpl) + " dtor called!");
			Dispose();
		}

		public unsafe override void UpdateMemory() {
			if (disposed)
				throw new ObjectDisposedException(nameof(DbgRawMetadataImpl));
			process?.ReadMemory(moduleAddress, address.ToPointer(), size);
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
			ForceDispose();
		}

		internal void ForceDispose() {
			GC.SuppressFinalize(this);
			if (process != null && address != IntPtr.Zero && Interlocked.Exchange(ref freedAddress, 1) == 0) {
				bool b = NativeMethods.VirtualFree(address, IntPtr.Zero, NativeMethods.MEM_RELEASE);
				Debug.Assert(b);
			}
			if (process == null) {
				try {
					if (moduleBytesHandle.IsAllocated)
						moduleBytesHandle.Free();
				}
				catch (InvalidOperationException) {
				}
			}
		}
	}
}
