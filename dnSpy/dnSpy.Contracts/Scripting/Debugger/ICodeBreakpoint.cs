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
	/// A code breakpoint (IL or native)
	/// </summary>
	public interface ICodeBreakpoint : IBreakpoint {
		/// <summary>
		/// true if it's an IL breakpoint (<see cref="IILBreakpoint"/>
		/// </summary>
		bool IsIL { get; }

		/// <summary>
		/// true if it's a native breakpoint (<see cref="INativeBreakpoint"/>
		/// </summary>
		bool IsNative { get; }

		/// <summary>
		/// Gets the module name
		/// </summary>
		ModuleName Module { get; }

		/// <summary>
		/// Gets the method token
		/// </summary>
		uint Token { get; }

		/// <summary>
		/// Gets the offset of the breakpoint relative to the start of the method
		/// </summary>
		uint Offset { get; }
	}
}
