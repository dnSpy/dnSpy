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

namespace dnSpy.Contracts.Debugger.CorDebug {
	/// <summary>
	/// Debugging options that will start and debug an application when passed to <see cref="DbgManager.Start(StartDebuggingOptions)"/>.
	/// This is used to debug .NET Core assemblies.
	/// </summary>
	public sealed class DotNetCoreStartDebuggingOptions : StartDebuggingOptions {
		/// <summary>
		/// Path to host or null if dnSpy should try to find dotnet.exe
		/// </summary>
		public string Host { get; set; }

		/// <summary>
		/// Host arguments. Can be null if <see cref="Host"/> is null in which case "exec" is used.
		/// </summary>
		public string HostArguments { get; set; }

		/// <summary>
		/// Path to application to debug
		/// </summary>
		public string Filename { get; set; }

		/// <summary>
		/// Command line
		/// </summary>
		public string CommandLine { get; set; }

		/// <summary>
		/// Working directory
		/// </summary>
		public string WorkingDirectory { get; set; }

		/// <summary>
		/// Break kind, defaults to <see cref="BreakProcessKind.None"/>
		/// </summary>
		public BreakProcessKind BreakProcessKind { get; set; } = BreakProcessKind.None;

		/// <summary>
		/// true to ignore the break instruction and <see cref="System.Diagnostics.Debugger.Break"/> method calls
		/// </summary>
		public bool IgnoreBreakInstructions { get; set; }

		/// <summary>
		/// true to disable detection of managed debuggers
		/// </summary>
		public bool DisableManagedDebuggerDetection { get; set; }
	}
}
