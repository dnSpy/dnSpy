/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
	public sealed class CoreCLRDebugOptions {
		/// <summary>
		/// Debug options
		/// </summary>
		public DebugOptions Options { get; }

		/// <summary>
		/// dbgshim.dll filename or null
		/// </summary>
		public string DbgShimFilename { get; set; }

		/// <summary>
		/// Host filename, eg. path to <c>CoreRun.exe</c>
		/// </summary>
		public string HostFilename { get; set; }

		/// <summary>
		/// Host command line
		/// </summary>
		public string HostCommandLine { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public CoreCLRDebugOptions() {
			this.Options = new DebugOptions();
		}
	}
}
