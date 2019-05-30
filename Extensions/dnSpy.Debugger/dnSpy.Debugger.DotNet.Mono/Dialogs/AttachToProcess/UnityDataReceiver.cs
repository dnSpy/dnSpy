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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace dnSpy.Debugger.DotNet.Mono.Dialogs.AttachToProcess {
	sealed class UnityDataReceiver : IDisposable {
		readonly CancellationToken cancellationToken;
		readonly List<Connection> connections;
		readonly DateTime endTime;

		sealed class Connection {
			readonly Socket socket;
			readonly byte[] buffer;
			public IAsyncResult? AsyncResult;

			public Connection(IPAddress groupAddr, ushort port, IPAddress addr) {
				try {
					buffer = new byte[1024 * 4];
					socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(groupAddr, addr));
					socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);
					socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, addr.GetAddressBytes());
					socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
					socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
					socket.Bind(new IPEndPoint(IPAddress.Any, port));
				}
				catch {
					socket?.Close();
					throw;
				}
			}

			public IAsyncResult Start() {
				EndPoint anyEndpoint = new IPEndPoint(IPAddress.Any, 0);
				return socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref anyEndpoint, null, null);
			}

			public byte[]? GetData() {
				EndPoint anyEndpoint = new IPEndPoint(IPAddress.Any, 0);
				var asyncRes = AsyncResult;
				AsyncResult = null;
				int count = socket.EndReceiveFrom(asyncRes, ref anyEndpoint);
				if (count == 0)
					return null;
				var data = new byte[count];
				Array.Copy(buffer, data, count);
				return data;
			}

			public void Dispose() => socket.Close();
		}

		public UnityDataReceiver(TimeSpan waitTime, CancellationToken cancellationToken) {
			this.cancellationToken = cancellationToken;
			connections = new List<Connection>();
			endTime = DateTime.UtcNow + waitTime;
		}

		public void Add(IPAddress groupAddr, ushort port, IPAddress addr) {
			try {
				connections.Add(new Connection(groupAddr, port, addr));
			}
			catch {
			}
		}

		public void Start() {
		}

		public byte[]? GetNextData() {
			for (;;) {
				if (cancellationToken.IsCancellationRequested)
					return null;
				if (connections.Count == 0)
					return null;

				var currentTime = DateTime.UtcNow;
				if (currentTime >= endTime)
					return null;

				foreach (var s in connections) {
					if (s.AsyncResult is null)
						s.AsyncResult = s.Start();
				}

				var handles = connections.Select(a => a.AsyncResult!.AsyncWaitHandle).ToArray();
				//TODO: Throws if there are too many handles
				int index = WaitHandle.WaitAny(handles, endTime - currentTime);
				if (index == WaitHandle.WaitTimeout || (uint)index >= (uint)handles.Length)
					return null;
				if (cancellationToken.IsCancellationRequested)
					return null;

				var conn = connections[index];
				byte[]? data = null;
				try {
					data = conn.GetData();
				}
				catch (SocketException) {
				}
				catch (ObjectDisposedException) {
				}

				if (!(data is null))
					return data;

				conn.Dispose();
				connections.RemoveAt(index);
			}
		}

		public void Dispose() {
			foreach (var s in connections)
				s.Dispose();
		}
	}
}
