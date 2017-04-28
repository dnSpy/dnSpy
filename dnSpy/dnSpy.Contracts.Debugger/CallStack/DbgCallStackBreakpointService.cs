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

using dnSpy.Contracts.Debugger.Breakpoints.Code;

namespace dnSpy.Contracts.Debugger.CallStack {
	/// <summary>
	/// Creates breakpoints
	/// </summary>
	public abstract class DbgCallStackBreakpointService {
		/// <summary>
		/// Creates a breakpoint or returns null if none could be created or if there's already a breakpoint at <paramref name="location"/>
		/// </summary>
		/// <param name="location">Location, null is allowed</param>
		/// <param name="options">Breakpoint options</param>
		/// <returns></returns>
		public abstract DbgCodeBreakpoint Create(DbgStackFrameLocation location, DbgCodeBreakpointOptions options = DbgCodeBreakpointOptions.None);

		/// <summary>
		/// Returns an existing breakpoint or null if none exists
		/// </summary>
		/// <param name="location">Location, null is allowed</param>
		/// <returns></returns>
		public abstract DbgCodeBreakpoint TryGetBreakpoint(DbgStackFrameLocation location);
	}
}
