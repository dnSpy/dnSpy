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
	/// Module breakpoint settings
	/// </summary>
	public struct DbgModuleBreakpointSettings : IEquatable<DbgModuleBreakpointSettings> {
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DbgModuleBreakpointSettings left, DbgModuleBreakpointSettings right) => left.Equals(right);
		public static bool operator !=(DbgModuleBreakpointSettings left, DbgModuleBreakpointSettings right) => !left.Equals(right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Compares this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(DbgModuleBreakpointSettings other) =>
			IsEnabled == other.IsEnabled &&
			ModuleName == other.ModuleName &&
			IsDynamic == other.IsDynamic &&
			IsInMemory == other.IsInMemory &&
			Order == other.Order &&
			AppDomainName == other.AppDomainName &&
			ProcessName == other.ProcessName;

		/// <summary>
		/// Compares this instance to <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is DbgModuleBreakpointSettings other && Equals(other);

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() =>
			(IsEnabled ? 1 : 0) ^
			(ModuleName ?? string.Empty).GetHashCode() ^
			((IsDynamic ?? false) ? 2 : 0) ^
			((IsInMemory ?? false) ? 4 : 0) ^
			(Order ?? 0) ^
			(AppDomainName ?? string.Empty).GetHashCode() ^
			(ProcessName ?? string.Empty).GetHashCode();
	}
}
