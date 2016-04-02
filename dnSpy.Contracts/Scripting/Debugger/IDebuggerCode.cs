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

using System;
using dnSpy.Contracts.Highlighting;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Code in the debugged process (<c>ICorDebugCode</c>)
	/// </summary>
	public interface IDebuggerCode {
		/// <summary>
		/// true if it's IL code, false if native
		/// </summary>
		bool IsIL { get; }

		/// <summary>
		/// Gets the size of the code
		/// </summary>
		uint Size { get; }

		/// <summary>
		/// Gets the address of code (eg. IL instructions). If it's IL, it doesn't include the
		/// method header.
		/// </summary>
		ulong Address { get; }

		/// <summary>
		/// Gets the EnC (edit and continue) version number of this method
		/// </summary>
		uint VersionNumber { get; }

		/// <summary>
		/// Gets the method
		/// </summary>
		IDebuggerMethod Method { get; }

		/// <summary>
		/// Gets all code chunks if <see cref="IsIL"/> is <c>false</c>
		/// </summary>
		/// <returns></returns>
		CodeChunkInfo[] GetCodeChunks();

		/// <summary>
		/// Reads the (IL or native) code at <see cref="Address"/>
		/// </summary>
		/// <returns></returns>
		byte[] ReadCode();

		/// <summary>
		/// Creates an IL code breakpoint that's only valid for the current debugging session. The
		/// breakpoint is not added to the breakpoints shown in the UI.
		/// </summary>
		/// <param name="offset">IL code offset in method</param>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		IILBreakpoint CreateBreakpoint(uint offset = 0, Func<IILBreakpoint, bool> cond = null);

		/// <summary>
		/// Creates an IL code breakpoint that's only valid for the current debugging session. The
		/// breakpoint is not added to the breakpoints shown in the UI.
		/// </summary>
		/// <param name="offset">IL code offset in method</param>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		IILBreakpoint CreateBreakpoint(int offset, Func<IILBreakpoint, bool> cond = null);

		/// <summary>
		/// Creates a native code breakpoint that's only valid for the current debugging session.
		/// The breakpoint is not added to the breakpoints shown in the UI. The method must have been
		/// jitted or setting the breakpoint will fail.
		/// </summary>
		/// <param name="offset">Native code offset in method</param>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		INativeBreakpoint CreateNativeBreakpoint(uint offset = 0, Func<INativeBreakpoint, bool> cond = null);

		/// <summary>
		/// Creates a native code breakpoint that's only valid for the current debugging session.
		/// The breakpoint is not added to the breakpoints shown in the UI. The method must have been
		/// jitted or setting the breakpoint will fail.
		/// </summary>
		/// <param name="offset">Native code offset in method</param>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		INativeBreakpoint CreateNativeBreakpoint(int offset, Func<INativeBreakpoint, bool> cond = null);

		/// <summary>
		/// Write this to <paramref name="output"/>
		/// </summary>
		/// <param name="output">Destination</param>
		/// <param name="flags">Flags</param>
		void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags = TypeFormatFlags.Default);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		string ToString(TypeFormatFlags flags);
	}
}
