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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Disassembly;

namespace dnSpy.Contracts.Debugger.Disassembly {
	/// <summary>
	/// Returns a method's native code
	/// </summary>
	public abstract class DbgNativeCodeProvider {
		/// <summary>
		/// Checks if it's possible to get native code
		/// </summary>
		/// <param name="frame">Stack frame</param>
		/// <returns></returns>
		public abstract bool CanGetNativeCode(DbgStackFrame frame);

		/// <summary>
		/// Tries to get the native code
		/// </summary>
		/// <param name="frame">Stack frame</param>
		/// <param name="options">Options</param>
		/// <param name="result">Native code if successful</param>
		/// <returns></returns>
		public abstract bool TryGetNativeCode(DbgStackFrame frame, DbgNativeCodeOptions options, out GetNativeCodeResult result);

		/// <summary>
		/// Checks if it's possible to get native code
		/// </summary>
		/// <param name="boundBreakpoint">A bound breakpoint</param>
		/// <returns></returns>
		public abstract bool CanGetNativeCode(DbgBoundCodeBreakpoint boundBreakpoint);

		/// <summary>
		/// Tries to get the native code
		/// </summary>
		/// <param name="boundBreakpoint">A bound breakpoint</param>
		/// <param name="options">Options</param>
		/// <param name="result">Native code if successful</param>
		/// <returns></returns>
		public abstract bool TryGetNativeCode(DbgBoundCodeBreakpoint boundBreakpoint, DbgNativeCodeOptions options, out GetNativeCodeResult result);

		/// <summary>
		/// Checks if it's possible to get native code
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="location">Code location</param>
		/// <returns></returns>
		public abstract bool CanGetNativeCode(DbgRuntime runtime, DbgCodeLocation location);

		/// <summary>
		/// Tries to get the native code
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="location">Code location</param>
		/// <param name="options">Options</param>
		/// <param name="result">Native code if successful</param>
		/// <returns></returns>
		public abstract bool TryGetNativeCode(DbgRuntime runtime, DbgCodeLocation location, DbgNativeCodeOptions options, out GetNativeCodeResult result);
	}

	/// <summary>
	/// Native code result
	/// </summary>
	public readonly struct GetNativeCodeResult {
		/// <summary>
		/// Native code
		/// </summary>
		public NativeCode Code { get; }

		/// <summary>
		/// Symbol resolver or null
		/// </summary>
		public ISymbolResolver? SymbolResolver { get; }

		/// <summary>
		/// Header or null
		/// </summary>
		public string? Header { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Native code</param>
		/// <param name="symbolResolver">Symbol resolver or null</param>
		/// <param name="header">Header or null</param>
		public GetNativeCodeResult(NativeCode code, ISymbolResolver? symbolResolver, string? header) {
			Code = code;
			SymbolResolver = symbolResolver;
			Header = header;
		}
	}

	/// <summary>
	/// Native code options
	/// </summary>
	[Flags]
	public enum DbgNativeCodeOptions : uint {
		/// <summary>
		/// No option is enabled
		/// </summary>
		None						= 0,

		/// <summary>
		/// Show IL code, if available
		/// </summary>
		ShowILCode					= 0x00000001,

		/// <summary>
		/// Show source code or decompiled code, if available
		/// </summary>
		ShowCode					= 0x00000002,
	}
}
