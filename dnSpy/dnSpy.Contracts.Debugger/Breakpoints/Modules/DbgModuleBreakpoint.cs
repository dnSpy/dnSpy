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
	/// Module breakpoint
	/// </summary>
	public abstract class DbgModuleBreakpoint : DbgObject {
		/// <summary>
		/// Gets the unique module breakpoint id
		/// </summary>
		public abstract int Id { get; }

		/// <summary>
		/// Gets/sets the current settings
		/// </summary>
		public abstract DbgModuleBreakpointSettings Settings { get; set; }

		/// <summary>
		/// true if the breakpoint is enabled
		/// </summary>
		public bool IsEnabled {
			get => Settings.IsEnabled;
			set {
				var settings = Settings;
				settings.IsEnabled = value;
				Settings = settings;
			}
		}

		/// <summary>
		/// Name of module (case insensitive) or null/empty string if any name. Wildcards can be used
		/// </summary>
		public string ModuleName {
			get => Settings.ModuleName;
			set {
				var settings = Settings;
				settings.ModuleName = value;
				Settings = settings;
			}
		}

		/// <summary>
		/// true if dynamic, false if not dynamic, and null if any value
		/// </summary>
		public bool? IsDynamic {
			get => Settings.IsDynamic;
			set {
				var settings = Settings;
				settings.IsDynamic = value;
				Settings = settings;
			}
		}

		/// <summary>
		/// true if in-memory, false if not in-memory, and null if any value
		/// </summary>
		public bool? IsInMemory {
			get => Settings.IsInMemory;
			set {
				var settings = Settings;
				settings.IsInMemory = value;
				Settings = settings;
			}
		}

		/// <summary>
		/// Order or null if any value
		/// </summary>
		public int? Order {
			get => Settings.Order;
			set {
				var settings = Settings;
				settings.Order = value;
				Settings = settings;
			}
		}

		/// <summary>
		/// App domain name (case insensitive) or null/empty string if any name. Wildcards can be used
		/// </summary>
		public string AppDomainName {
			get => Settings.AppDomainName;
			set {
				var settings = Settings;
				settings.AppDomainName = value;
				Settings = settings;
			}
		}

		/// <summary>
		/// Process name (case insensitive) or null/empty string if any name. Wildcards can be used
		/// </summary>
		public string ProcessName {
			get => Settings.ProcessName;
			set {
				var settings = Settings;
				settings.ProcessName = value;
				Settings = settings;
			}
		}

		/// <summary>
		/// Removes this breakpoint from the module breakpoints list
		/// </summary>
		public abstract void Remove();
	}
}
