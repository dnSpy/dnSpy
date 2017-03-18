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

namespace dnSpy.Contracts.Debugger.Breakpoints.Modules {
	/// <summary>
	/// Module breakpoint settings
	/// </summary>
	public struct DbgModuleBreakpointSettings {
		/// <summary>
		/// true if the breakpoint is enabled
		/// </summary>
		public bool IsEnabled { get; set; }

		/// <summary>
		/// Name of module (case insensitive) or null/empty string if any name. Wildcards can be used
		/// </summary>
		public string ModuleName { get; set; }

		/// <summary>
		/// true if dynamic, false if not dynamic, and null if any value
		/// </summary>
		public bool? IsDynamic { get; set; }

		/// <summary>
		/// true if in-memory, false if not in-memory, and null if any value
		/// </summary>
		public bool? IsInMemory { get; set; }

		/// <summary>
		/// Module load order or null if any value
		/// </summary>
		public int? Order { get; set; }

		/// <summary>
		/// App domain name (case insensitive) or null/empty string if any name. Wildcards can be used
		/// </summary>
		public string AppDomainName { get; set; }

		/// <summary>
		/// Process name (case insensitive) or null/empty string if any name. Wildcards can be used
		/// </summary>
		public string ProcessName { get; set; }
	}
}
