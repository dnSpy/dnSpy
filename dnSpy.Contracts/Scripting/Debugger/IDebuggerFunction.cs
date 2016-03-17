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
using dnlib.DotNet;
using dnSpy.Contracts.Highlighting;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// A method in a module in the debugged process (<c>ICorDebugFunction</c>)
	/// </summary>
	public interface IDebuggerFunction {
		/// <summary>
		/// Gets the method signature. It's currently using custom <see cref="TypeDef"/>,
		/// <see cref="TypeRef"/> and <see cref="TypeSpec"/> instances that don't reveal all
		/// information available in the metadata.
		/// </summary>
		MethodSig MethodSig { get; }

		/// <summary>
		/// Owner module
		/// </summary>
		IDebuggerModule Module { get; }

		/// <summary>
		/// Owner class
		/// </summary>
		IDebuggerClass Class { get; }

		/// <summary>
		/// Token of method
		/// </summary>
		uint Token { get; }

		/// <summary>
		/// Gets/sets JMC (just my code) flag
		/// </summary>
		bool JustMyCode { get; set; }

		/// <summary>
		/// Gets EnC (edit and continue) version number of the latest edit, and might be greater
		/// than this function's version number. See <see cref="VersionNumber"/>.
		/// </summary>
		uint CurrentVersionNumber { get; }

		/// <summary>
		/// Gets the EnC (edit and continue) version number of this function
		/// </summary>
		uint VersionNumber { get; }

		/// <summary>
		/// Gets the local variables signature token or 0 if none
		/// </summary>
		uint LocalVarSigToken { get; }

		/// <summary>
		/// Gets the IL code or null
		/// </summary>
		IDebuggerCode ILCode { get; }

		/// <summary>
		/// Gets the native code or null. If it's a generic method that's been JITed more than once,
		/// the returned code could be any one of the JITed codes.
		/// </summary>
		/// <remarks><c>EnumerateNativeCode()</c> should be called but that method hasn't been
		/// implemented by the CLR debugger yet.</remarks>
		IDebuggerCode NativeCode { get; }

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
		void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		string ToString(TypeFormatFlags flags);
	}
}
