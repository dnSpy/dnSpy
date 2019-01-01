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
	/// Debugging options base class shared by .NET Framework code and .NET Core code
	/// </summary>
	public abstract class CorDebugStartDebuggingOptions : StartDebuggingOptions {
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
		/// Environment variables
		/// </summary>
		public DbgEnvironment Environment { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		protected CorDebugStartDebuggingOptions() => Environment = new DbgEnvironment();

		/// <summary>
		/// Copies this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Destination</param>
		protected void CopyTo(CorDebugStartDebuggingOptions other) {
			if (other == null)
				throw new ArgumentNullException(nameof(other));
			base.CopyTo(other);
			other.Filename = Filename;
			other.CommandLine = CommandLine;
			other.WorkingDirectory = WorkingDirectory;
			other.Environment.Clear();
			other.Environment.AddRange(Environment.Environment);
		}
	}
}
