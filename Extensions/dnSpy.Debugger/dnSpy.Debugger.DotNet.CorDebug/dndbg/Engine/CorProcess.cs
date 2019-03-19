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
using System.Collections.Generic;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class CorProcess : COMObject<ICorDebugProcess>, IEquatable<CorProcess> {
		public uint HelperThreadId {
			get {
				int hr = obj.GetHelperThreadID(out uint threadId);
				return hr < 0 ? 0 : threadId;
			}
		}

		public int ProcessId => pid;
		readonly int pid;

		public bool IsRunning {
			get {
				int hr = obj.IsRunning(out int running);
				return hr >= 0 && running != 0;
			}
		}

		public IEnumerable<CorThread> Threads {
			get {
				int hr = obj.EnumerateThreads(out var threadEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					hr = threadEnum.Next(1, out var thread, out uint count);
					if (hr != 0 || thread == null)
						break;
					yield return new CorThread(thread);
				}
			}
		}

		public IEnumerable<CorAppDomain> AppDomains {
			get {
				int hr = obj.EnumerateAppDomains(out var appDomainEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					hr = appDomainEnum.Next(1, out var appDomain, out uint count);
					if (hr != 0 || appDomain == null)
						break;
					yield return new CorAppDomain(appDomain);
				}
			}
		}

		public IntPtr Handle {
			get {
				int hr = obj.GetHandle(out var handle);
				return hr < 0 ? IntPtr.Zero : handle;
			}
		}

		public Version CLRVersion {
			get {
				if (obj is ICorDebugProcess2 p2) {
					int hr = p2.GetVersion(out var ver);
					if (hr >= 0)
						return new Version((int)ver.dwMajor, (int)ver.dwMinor, (int)ver.dwBuild, (int)ver.dwSubBuild);
				}
				return new Version(0, 0, 0, 0);
			}
		}

		public CorDebugJITCompilerFlags DesiredNGENCompilerFlags {
			get {
				var p2 = obj as ICorDebugProcess2;
				if (p2 == null)
					return 0;
				int hr = p2.GetDesiredNGENCompilerFlags(out var flags);
				return hr < 0 ? 0 : flags;
			}
			set {
				var p2 = obj as ICorDebugProcess2;
				if (p2 == null)
					return;
				int hr = p2.SetDesiredNGENCompilerFlags(value);
			}
		}

		public CorProcess(ICorDebugProcess process)
			: base(process) {
			int hr = process.GetID(out pid);
			if (hr < 0)
				pid = 0;
		}

		public unsafe int ReadMemory(ulong address, byte[] buffer, long index, int size, out int sizeRead) {
			IntPtr sizeRead2 = IntPtr.Zero;
			int hr;
			fixed (byte* p = &buffer[index])
				hr = obj.ReadMemory(address, (uint)size, new IntPtr(p), out sizeRead2);
			const int ERROR_PARTIAL_COPY = unchecked((int)0x8007012B);
			if (hr < 0 && hr != ERROR_PARTIAL_COPY) {
				sizeRead = 0;
				return hr;
			}

			sizeRead = (int)sizeRead2.ToInt64();
			return 0;
		}

		public unsafe int WriteMemory(ulong address, byte[] buffer, long index, int size, out int sizeWritten) {
			if (size == 0) {
				sizeWritten = 0;
				return 0;
			}
			fixed (byte* p = &buffer[index])
				return WriteMemory(address, p, size, out sizeWritten);
		}

		public unsafe int WriteMemory(ulong address, void* buffer, int size, out int sizeWritten) {
			var sizeWritten2 = IntPtr.Zero;
			int hr = obj.WriteMemory(address, (uint)size, new IntPtr(buffer), out sizeWritten2);
			const int ERROR_PARTIAL_COPY = unchecked((int)0x8007012B);
			if (hr < 0 && hr != ERROR_PARTIAL_COPY) {
				sizeWritten = 0;
				return hr;
			}

			sizeWritten = (int)sizeWritten2.ToInt64();
			return 0;
		}

		public byte[] ReadMemory(ulong addr, int size) {
			if (addr == 0 || size < 0)
				return null;
			var buf = new byte[size];
			for (int index = 0; index < size;) {
				int sizeLeft = size - index;
				int hr = ReadMemory(addr, buf, index, sizeLeft, out int sizeRead);
				if (hr < 0 || sizeRead <= 0)
					return null;
				index += sizeRead;
				addr += (ulong)sizeRead;
			}
			return buf;
		}

		public void SetEnableCustomNotification(CorClass cls, bool enable) {
			if (obj is ICorDebugProcess3 p3) {
				int hr = p3.SetEnableCustomNotification(cls.RawObject, enable ? 1 : 0);
			}
		}

		public void SetWriteableMetadataUpdateMode(WriteableMetadataUpdateMode mode) {
			if (obj is ICorDebugProcess7 p7) {
				int hr = p7.SetWriteableMetadataUpdateMode(mode);
				// 0x80131c4e: CORDBG_E_UNSUPPORTED
				// Not supported in V2 debuggers (when shim is used). Supported in V3, which we're
				// not using.
			}
		}

		public void EnableLogMessages(bool enable) => obj.EnableLogMessages(enable ? 1 : 0);

		public void EnableExceptionCallbacksOutsideOfMyCode(bool value) {
			if (obj is ICorDebugProcess8 p8) {
				int hr = p8.EnableExceptionCallbacksOutsideOfMyCode(value ? 1 : 0);
			}
		}

		public void EnableNGENPolicy(CorDebugNGENPolicy policy) {
			if (obj is ICorDebugProcess5 p5) {
				int hr = p5.EnableNGENPolicy(policy);
			}
		}

		public bool Terminate(int exitCode) => obj.Terminate((uint)exitCode) >= 0;

		public bool Detach() {
			int hr = obj.Detach();
			return hr >= 0;
		}

		public static bool operator ==(CorProcess a, CorProcess b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorProcess a, CorProcess b) => !(a == b);

		public bool Equals(CorProcess other) => !ReferenceEquals(other, null) && RawObject == other.RawObject;
		public override bool Equals(object obj) => Equals(obj as CorProcess);
		public override int GetHashCode() => RawObject.GetHashCode();
		public override string ToString() => $"[Process] {ProcessId} CLR v{CLRVersion} Flags={DesiredNGENCompilerFlags}";
	}
}
