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
using dnSpy.Contracts.Metadata;

namespace dnSpy.Contracts.Debugger.DotNet.Breakpoints.Code {
	/// <summary>
	/// Creates breakpoints and tracepoints
	/// </summary>
	public abstract class DbgDotNetBreakpointLocationFactory {
		/// <summary>
		/// Creates an enabled breakpoint. If there's already a breakpoint at the location, null is returned.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of a method within the module</param>
		/// <param name="offset">IL offset of the breakpoint within the method body</param>
		/// <returns></returns>
		public DbgCodeBreakpoint Create(ModuleId module, uint token, uint offset) =>
			Create(module, token, offset, new DbgCodeBreakpointSettings { IsEnabled = true });

		/// <summary>
		/// Creates an enabled tracepoint. If there's already a breakpoint at the location, null is returned.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of a method within the module</param>
		/// <param name="offset">IL offset of the tracepoint within the method body</param>
		/// <param name="message">Message</param>
		/// <returns></returns>
		public DbgCodeBreakpoint CreateTracepoint(ModuleId module, uint token, uint offset, string message) =>
			Create(module, token, offset, new DbgCodeBreakpointSettings { IsEnabled = true, Trace = new DbgCodeBreakpointTrace(message, @continue: true) });

		/// <summary>
		/// Creates a breakpoint or a tracepoint
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of a method within the module</param>
		/// <param name="offset">IL offset of the breakpoint within the method body</param>
		/// <param name="bpSettings">Breakpoint settings</param>
		/// <returns></returns>
		public abstract DbgCodeBreakpoint Create(ModuleId module, uint token, uint offset, DbgCodeBreakpointSettings bpSettings);
	}
}
