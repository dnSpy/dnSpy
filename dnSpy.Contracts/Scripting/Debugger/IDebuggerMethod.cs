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
	public interface IDebuggerMethod {
		/// <summary>
		/// Gets the name
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the implementation attributes
		/// </summary>
		MethodImplAttributes ImplAttributes { get; }

		/// <summary>
		/// Gets the attributes
		/// </summary>
		MethodAttributes Attributes { get; }

		/// <summary>
		/// Gets the access
		/// </summary>
		MethodAttributes Access { get; }

		/// <summary>
		/// true if compiler controlled / private scope
		/// </summary>
		bool IsCompilerControlled { get; }

		/// <summary>
		/// true if compiler controlled / private scope
		/// </summary>
		bool IsPrivateScope { get; }

		/// <summary>
		/// true if private
		/// </summary>
		bool IsPrivate { get; }

		/// <summary>
		/// true if family and assembly
		/// </summary>
		bool IsFamilyAndAssembly { get; }

		/// <summary>
		/// true if assembly
		/// </summary>
		bool IsAssembly { get; }

		/// <summary>
		/// true if family
		/// </summary>
		bool IsFamily { get; }

		/// <summary>
		/// true if family or assembly
		/// </summary>
		bool IsFamilyOrAssembly { get; }

		/// <summary>
		/// true if public
		/// </summary>
		bool IsPublic { get; }

		/// <summary>
		/// true if static
		/// </summary>
		bool IsStatic { get; }

		/// <summary>
		/// true if final
		/// </summary>
		bool IsFinal { get; }

		/// <summary>
		/// true if virtual
		/// </summary>
		bool IsVirtual { get; }

		/// <summary>
		/// true if hide by signature
		/// </summary>
		bool IsHideBySig { get; }

		/// <summary>
		/// true if new slot
		/// </summary>
		bool IsNewSlot { get; }

		/// <summary>
		/// true if reuse slot
		/// </summary>
		bool IsReuseSlot { get; }

		/// <summary>
		/// true if check access on override
		/// </summary>
		bool IsCheckAccessOnOverride { get; }

		/// <summary>
		/// true if abstract
		/// </summary>
		bool IsAbstract { get; }

		/// <summary>
		/// true if special name
		/// </summary>
		bool IsSpecialName { get; }

		/// <summary>
		/// true if P/Invoke implementation
		/// </summary>
		bool IsPinvokeImpl { get; }

		/// <summary>
		/// true if unmanaged export
		/// </summary>
		bool IsUnmanagedExport { get; }

		/// <summary>
		/// true if runtime special name
		/// </summary>
		bool IsRuntimeSpecialName { get; }

		/// <summary>
		/// true if it has a security descriptor
		/// </summary>
		bool HasSecurity { get; }

		/// <summary>
		/// true if require security object
		/// </summary>
		bool IsRequireSecObject { get; }

		/// <summary>
		/// Gets the code type
		/// </summary>
		MethodImplAttributes CodeType { get; }

		/// <summary>
		/// true if IL code
		/// </summary>
		bool IsIL { get; }

		/// <summary>
		/// true if native code
		/// </summary>
		bool IsNative { get; }

		/// <summary>
		/// true if OPTIL code
		/// </summary>
		bool IsOPTIL { get; }

		/// <summary>
		/// true if runtime code
		/// </summary>
		bool IsRuntime { get; }

		/// <summary>
		/// true if unmanaged code
		/// </summary>
		bool IsUnmanaged { get; }

		/// <summary>
		/// true if managed code
		/// </summary>
		bool IsManaged { get; }

		/// <summary>
		/// true if forward reference
		/// </summary>
		bool IsForwardRef { get; }

		/// <summary>
		/// true if preserve signature
		/// </summary>
		bool IsPreserveSig { get; }

		/// <summary>
		/// true if internal call
		/// </summary>
		bool IsInternalCall { get; }

		/// <summary>
		/// true if synchronized
		/// </summary>
		bool IsSynchronized { get; }

		/// <summary>
		/// true if no inlining
		/// </summary>
		bool IsNoInlining { get; }

		/// <summary>
		/// true if aggressive inlining
		/// </summary>
		bool IsAggressiveInlining { get; }

		/// <summary>
		/// true if no optimization
		/// </summary>
		bool IsNoOptimization { get; }

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
		/// than this method's version number. See <see cref="VersionNumber"/>.
		/// </summary>
		uint CurrentVersionNumber { get; }

		/// <summary>
		/// Gets the EnC (edit and continue) version number of this method
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
		void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags = TypeFormatFlags.ShowParameterTypes | TypeFormatFlags.ShowReturnTypes | TypeFormatFlags.ShowNamespaces | TypeFormatFlags.ShowTypeKeywords);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		string ToString(TypeFormatFlags flags);
	}
}
