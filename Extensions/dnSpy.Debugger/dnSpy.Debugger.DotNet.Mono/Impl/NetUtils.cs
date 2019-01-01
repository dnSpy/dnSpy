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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	static class NetUtils {
		static readonly Random random = new Random();

		[StructLayout(LayoutKind.Sequential)]
		struct MIB_TCPTABLE2 {
#pragma warning disable CS0649
			public uint dwNumEntries;
			public MIB_TCPROW2 table;
#pragma warning restore CS0649
		}

		[StructLayout(LayoutKind.Sequential)]
		struct MIB_TCPROW2 {
			public const uint SIZE = 0x1C;
#pragma warning disable CS0649
			public uint dwState;
			public uint dwLocalAddr;
			public uint dwLocalPort;
			public uint dwRemoteAddr;
			public uint dwRemotePort;
			public uint dwOwningPid;
			public TCP_CONNECTION_OFFLOAD_STATE dwOffloadState;
#pragma warning restore CS0649
		}

		enum TCP_CONNECTION_OFFLOAD_STATE {
			TcpConnectionOffloadStateInHost = 0,
			TcpConnectionOffloadStateOffloading = 1,
			TcpConnectionOffloadStateOffloaded = 2,
			TcpConnectionOffloadStateUploading = 3,
			TcpConnectionOffloadStateMax = 4,
		}

		static unsafe class NativeMethods {
			[DllImport("iphlpapi")]
			public static extern uint GetTcpTable2(MIB_TCPTABLE2* TcpTable, ref uint SizePointer, bool Order);
		}

		unsafe static MIB_TCPROW2[] GetTcpRows() {
			uint sizePointer = 0;
			const bool order = false;
			NativeMethods.GetTcpTable2(null, ref sizePointer, order);

			var rawTableBytes = new byte[sizePointer];
			fixed (byte* ptbl = rawTableBytes) {
				uint error = NativeMethods.GetTcpTable2((MIB_TCPTABLE2*)ptbl, ref sizePointer, order);
				Debug.Assert(error == 0);
				if (error != 0)
					return Array.Empty<MIB_TCPROW2>();
				int elems = (int)((MIB_TCPTABLE2*)ptbl)->dwNumEntries;
				var res = new MIB_TCPROW2[elems];
				var pelem = (MIB_TCPROW2*)(ptbl + 4);
				var p = ptbl + 4;
				for (int i = 0; i < res.Length; i++, p += MIB_TCPROW2.SIZE)
					res[i] = *(MIB_TCPROW2*)p;
				return res;
			}
		}

		public static int GetConnectionPort() => GetConnectionPort(1024, 65535);

		public static int GetConnectionPort(ushort min, ushort max) {
			int count = max - min + 1;
			var portInUse = new HashSet<uint>();
			foreach (var row in GetTcpRows())
				portInUse.Add((ushort)(((ushort)row.dwLocalPort << 8) | ((ushort)row.dwLocalPort >> 8)));
			int r = random.Next();
			for (int i = 0; i < count; i++) {
				ushort port = (ushort)(min + (r++ % count));
				if (!portInUse.Contains(port))
					return port;
			}
			return -1;
		}

		public static int? GetProcessIdOfListener(byte[] address, ushort port) {
			if (address.Length != 4)
				return null;
			uint addr = ((uint)address[3] << 24) | ((uint)address[2] << 16) | ((uint)address[1] << 8) | address[0];
			port = (ushort)((port >> 8) | (port << 8));
			var rows = GetTcpRows();
			foreach (var row in rows) {
				if (row.dwRemoteAddr == addr && (ushort)row.dwRemotePort == port) {
					foreach (var row2 in rows) {
						if (row2.dwOwningPid == 0)
							continue;
						if ((ushort)row.dwLocalPort == (ushort)row2.dwRemotePort && row.dwLocalAddr == row2.dwRemoteAddr)
							return (int)row2.dwOwningPid;
					}
					return null;
				}
			}
			return null;
		}

		public static int? GetProcessIdOfListenerLocalAddress(byte[] address, ushort port) {
			if (address.Length != 4)
				return null;
			uint addr = ((uint)address[3] << 24) | ((uint)address[2] << 16) | ((uint)address[1] << 8) | address[0];
			port = (ushort)((port >> 8) | (port << 8));
			var rows = GetTcpRows();
			foreach (var row in rows) {
				if (row.dwLocalAddr == addr && row.dwLocalPort == port)
					return (int)row.dwOwningPid;
			}
			return null;
		}
	}
}
