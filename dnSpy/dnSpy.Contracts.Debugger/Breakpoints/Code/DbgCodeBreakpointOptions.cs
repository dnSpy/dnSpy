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

namespace dnSpy.Contracts.Debugger.Breakpoints.Code {
	/// <summary>
	/// <see cref="DbgCodeBreakpoint"/> options
	/// </summary>
	[Flags]
	public enum DbgCodeBreakpointOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// It's a temporary breakpoint that gets removed when all debugged processes have exited
		/// </summary>
		Temporary				= 0x00000001,

		/// <summary>
		/// It's a hidden breakpoint. It's not shown in the UI (eg. breakpoints window, call stack window, glyph margin, text view)
		/// </summary>
		Hidden					= 0x00000002,

		/// <summary>
		/// It's a one-shot breakpoint. When the breakpoint is hit, the process is paused and the breakpoint is removed
		/// </summary>
		OneShot					= 0x00000004,
	}
}
