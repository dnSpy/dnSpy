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

namespace dnSpy.Contracts.Debugger.DotNet.CorDebug {
	/// <summary>
	/// Debugging options that will start and debug an application when passed to <see cref="DbgManager.Start(DebugProgramOptions)"/>.
	/// This is used to debug .NET Core assemblies.
	/// </summary>
	public sealed class DotNetCoreStartDebuggingOptions : CorDebugStartDebuggingOptions {
		/// <summary>
		/// If true, use <see cref="Host"/> (eg. dotnet.exe). If false, <see cref="Host"/>
		/// isn't used and <see cref="CorDebugStartDebuggingOptions.Filename"/> should be
		/// a native executable (eg. a renamed apphost.exe) that knows how to start the runtime.
		/// </summary>
		public bool UseHost { get; set; } = true;

		/// <summary>
		/// Path to host (eg. dotnet.exe) or null if dnSpy should try to find dotnet.exe
		/// </summary>
		public string Host { get; set; }

		/// <summary>
		/// Host arguments (eg. "exec" if .NET Core's dotnet.exe is used)
		/// </summary>
		public string HostArguments { get; set; }

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns></returns>
		public override DebugProgramOptions Clone() => CopyTo(new DotNetCoreStartDebuggingOptions());

		/// <summary>
		/// Copies this instance to <paramref name="other"/> and returns it
		/// </summary>
		/// <param name="other">Destination</param>
		/// <returns></returns>
		public DotNetCoreStartDebuggingOptions CopyTo(DotNetCoreStartDebuggingOptions other) {
			if (other == null)
				throw new ArgumentNullException(nameof(other));
			base.CopyTo(other);
			other.UseHost = UseHost;
			other.Host = Host;
			other.HostArguments = HostArguments;
			return other;
		}
	}
}
