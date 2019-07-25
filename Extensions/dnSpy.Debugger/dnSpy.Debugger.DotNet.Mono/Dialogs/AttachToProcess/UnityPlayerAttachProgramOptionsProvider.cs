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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using dnSpy.Contracts.Debugger.Attach;
using dnSpy.Debugger.DotNet.Mono.Impl;

namespace dnSpy.Debugger.DotNet.Mono.Dialogs.AttachToProcess {
	[ExportAttachProgramOptionsProviderFactory(PredefinedAttachProgramOptionsProviderNames.UnityPlayer)]
	sealed class UnityPlayerAttachProgramOptionsProviderFactory : AttachProgramOptionsProviderFactory {
		public override AttachProgramOptionsProvider? Create(bool allFactories) => allFactories ? null : new UnityPlayerAttachProgramOptionsProvider();
	}

	sealed class UnityPlayerAttachProgramOptionsProvider : AttachProgramOptionsProvider {
		static readonly ushort[] multicastPorts = new ushort[] { 34997, 54997, 57997, 58997 };
		static readonly TimeSpan maxWaitTime = TimeSpan.FromMilliseconds(2000);

		struct PlayerId : IEquatable<PlayerId> {
			readonly string ip;
			readonly ushort port;

			public PlayerId(string ip, ushort port) {
				this.ip = ip;
				this.port = port;
			}

			public bool Equals(PlayerId other) =>
				StringComparer.OrdinalIgnoreCase.Equals(ip ?? string.Empty, other.ip ?? string.Empty) &&
				port == other.port;
			public override bool Equals(object? obj) =>
				obj is PlayerId other && Equals(other);
			public override int GetHashCode() =>
				StringComparer.OrdinalIgnoreCase.GetHashCode(ip ?? string.Empty) ^ port;
		}

		public override IEnumerable<AttachProgramOptions> Create(AttachProgramOptionsProviderContext context) {
			var groupAddr = IPAddress.Parse("225.0.0.222");
			var foundIds = new HashSet<PlayerId>();
			using (var receiver = new UnityDataReceiver(maxWaitTime, context.CancellationToken)) {
				foreach (var addr in GetAddresses(context.CancellationToken)) {
					context.CancellationToken.ThrowIfCancellationRequested();
					foreach (var port in multicastPorts)
						receiver.Add(groupAddr, port, addr);
				}
				receiver.Start();
				for (;;) {
					context.CancellationToken.ThrowIfCancellationRequested();
					var data = receiver.GetNextData();
					if (data is null)
						break;
					var s = Encoding.UTF8.GetString(data);
					if (!TryParseUnityPlayerData(s, out var ipAddress, out var port, out var id))
						continue;

					var playerId = new PlayerId(ipAddress, port);
					if (!foundIds.Add(playerId))
						continue;
					var pid = NetUtils.GetProcessIdOfListenerLocalAddress(IPAddress.Any.MapToIPv4().GetAddressBytes(), port);
					if (pid is null) {
						foundIds.Remove(playerId);
						continue;
					}
					if (!ProcessUtils.IsValidProcess(context, pid.Value, null))
						continue;
					ipAddress = "127.0.0.1";
					yield return new UnityAttachProgramOptionsImpl(pid.Value, ipAddress, port, "Unity (" + id + ")");
				}
			}
		}

		static readonly Regex playerAnnounceStringRegex = new Regex(@"^\[IP\] (\S+) \[Port\] (\d+) \[Flags\] (-?\d+) \[Guid\] (\d+) \[EditorId\] (\d+) \[Version\] (\d+) \[Id\] ([^\(]+)\(([^\)]+)\)(:(\d+))? \[Debug\] (\d+)");
		bool TryParseUnityPlayerData(string s, [NotNullWhen(true)] out string? ipAddress, out ushort port, [NotNullWhen(true)] out string? playerId) {
			ipAddress = null;
			port = 0;
			playerId = null;

			var m = playerAnnounceStringRegex.Match(s);
			if (!m.Success)
				return false;
			if (m.Groups.Count != 12)
				return false;
			var ip = m.Groups[1].Value;
			//var editorPort = m.Groups[2].Value;
			//var flags = m.Groups[3].Value;
			var guid = m.Groups[4].Value;
			//var editorId = m.Groups[5].Value;
			//var version = m.Groups[6].Value;
			var id = m.Groups[7].Value;
			var machine = m.Groups[8].Value;
			var debuggerPort = m.Groups[10].Value;
			var debug = m.Groups[11].Value;

			if (machine != Dns.GetHostName())
				return false;
			if (ip == string.Empty)
				return false;
			if (id == string.Empty)
				return false;

			if (!uint.TryParse(debug, out var utmp) || utmp != 1)
				return false;
			if (debuggerPort == string.Empty) {
				if (!uint.TryParse(guid, out utmp))
					return false;
				port = (ushort)(56000 + utmp % 1000);
			}
			else {
				if (!uint.TryParse(debuggerPort, out utmp) || utmp > ushort.MaxValue)
					return false;
				port = (ushort)utmp;
			}

			ipAddress = ip;
			playerId = id;
			return true;
		}

		static IEnumerable<IPAddress> GetAddresses(CancellationToken cancellationToken) {
			foreach (var niface in NetworkInterface.GetAllNetworkInterfaces()) {
				cancellationToken.ThrowIfCancellationRequested();
				if (!niface.SupportsMulticast)
					continue;
				if (niface.OperationalStatus != OperationalStatus.Up)
					continue;
				foreach (var addr in niface.GetIPProperties().UnicastAddresses) {
					cancellationToken.ThrowIfCancellationRequested();
					if (addr.Address.AddressFamily != AddressFamily.InterNetwork)
						continue;
					if (IPAddress.IsLoopback(addr.Address))
						continue;
					yield return addr.Address;
				}
			}
		}
	}
}
