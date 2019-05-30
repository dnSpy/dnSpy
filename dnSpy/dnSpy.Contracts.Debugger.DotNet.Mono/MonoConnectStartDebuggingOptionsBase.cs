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

namespace dnSpy.Contracts.Debugger.DotNet.Mono {
	/// <summary>
	/// Debugging options used when connecting to a Mono / Unity program
	/// </summary>
	public abstract class MonoConnectStartDebuggingOptionsBase : StartDebuggingOptions {
		/// <summary>
		/// The IP address <c>mono.exe</c> is listening on or null / empty string to use <c>127.0.0.1</c>
		/// </summary>
		public string? Address { get; set; }

		/// <summary>
		/// The port <c>mono.exe</c> is listening on
		/// </summary>
		public ushort Port { get; set; }

		/// <summary>
		/// Gets the connection timeout. If it's <see cref="TimeSpan.Zero"/>, the default timeout is used.
		/// </summary>
		public TimeSpan ConnectionTimeout { get; set; }

		/// <summary>
		/// true if the process is suspended and waiting for the debugger to connect
		/// </summary>
		public bool ProcessIsSuspended { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		protected MonoConnectStartDebuggingOptionsBase() => ConnectionTimeout = TimeSpan.Zero;

		/// <summary>
		/// Copies this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Destination</param>
		protected void CopyTo(MonoConnectStartDebuggingOptionsBase other) {
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			base.CopyTo(other);
			other.Address = Address;
			other.Port = Port;
			other.ConnectionTimeout = ConnectionTimeout;
			other.ProcessIsSuspended = ProcessIsSuspended;
		}
	}
}
