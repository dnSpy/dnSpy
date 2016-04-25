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
	/// Breakpoint
	/// </summary>
	public interface IBreakpoint {
		/// <summary>
		/// Gets the breakpoint kind
		/// </summary>
		BreakpointKind Kind { get; }

		/// <summary>
		/// true if the breakpoint is enabled
		/// </summary>
		bool IsEnabled { get; set; }

		/// <summary>
		/// The user can set this property to any value. It's not used by the debugger.
		/// </summary>
		object Tag { get; set; }

		/// <summary>
		/// Removes the breakpoint
		/// </summary>
		void Remove();
	}
}
