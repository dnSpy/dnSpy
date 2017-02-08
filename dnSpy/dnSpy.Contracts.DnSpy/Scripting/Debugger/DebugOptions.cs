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

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Debug options
	/// </summary>
	public sealed class DebugOptions {
		/// <summary>
		/// File to debug
		/// </summary>
		public string Filename { get; set; }

		/// <summary>
		/// Command line to pass to debugged program
		/// </summary>
		public string CommandLine { get; set; }

		/// <summary>
		/// Current directory of debugged program or null to use the debugger's cwd
		/// </summary>
		public string CurrentDirectory { get; set; }

		/// <summary>
		/// Break process kind
		/// </summary>
		public BreakProcessKind BreakProcessKind { get; set; }
	}
}
