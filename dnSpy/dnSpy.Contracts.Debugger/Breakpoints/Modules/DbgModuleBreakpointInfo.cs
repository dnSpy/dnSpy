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

namespace dnSpy.Contracts.Debugger.Breakpoints.Modules {
	/// <summary>
	/// Module info
	/// </summary>
	public readonly struct DbgModuleBreakpointInfo {
		/// <summary>
		/// Name of module
		/// </summary>
		public string ModuleName { get; }

		/// <summary>
		/// true if dynamic
		/// </summary>
		public bool IsDynamic { get; }

		/// <summary>
		/// true if in-memory
		/// </summary>
		public bool IsInMemory { get; }

		/// <summary>
		/// Order
		/// </summary>
		public int Order { get; }

		/// <summary>
		/// App domain name
		/// </summary>
		public string AppDomainName { get; }

		/// <summary>
		/// Process name
		/// </summary>
		public string ProcessName { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		public DbgModuleBreakpointInfo(DbgModule module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			ModuleName = module.Name ?? string.Empty;
			IsDynamic = module.IsDynamic;
			IsInMemory = module.IsInMemory;
			Order = module.Order;
			AppDomainName = module.AppDomain?.Name ?? string.Empty;
			ProcessName = module.Process.Name;
		}
	}
}
