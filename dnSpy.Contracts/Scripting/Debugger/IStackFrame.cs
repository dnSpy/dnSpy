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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dnSpy.Contracts.Highlighting;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// A stack frame in the debugged process. This stack frame is only valid until the debugged
	/// process continues.
	/// </summary>
	public interface IStackFrame {
		/// <summary>
		/// Can be called if this instance has been neutered (eg. the program has continued or the
		/// instruction pointer was changed) to get a new instance of this frame that isn't neutered.
		/// Returns null if the frame wasn't found or if the debugged process isn't paused.
		/// </summary>
		/// <returns></returns>
		IStackFrame TryGetNewFrame();

		/// <summary>
		/// true if it has been neutered. It gets neutered when the program continues or if the
		/// instruction pointer is changed. See <see cref="TryGetNewFrame"/>.
		/// </summary>
		bool IsNeutered { get; }

		/// <summary>
		/// Gets its chain
		/// </summary>
		IStackChain Chain { get; }

		/// <summary>
		/// Gets the token of the method or 0
		/// </summary>
		uint Token { get; }

		/// <summary>
		/// Start address of the stack segment
		/// </summary>
		ulong StackStart { get; }

		/// <summary>
		/// End address of the stack segment
		/// </summary>
		ulong StackEnd { get; }

		/// <summary>
		/// true if it's an IL frame
		/// </summary>
		bool IsILFrame { get; }

		/// <summary>
		/// true if it's a native frame
		/// </summary>
		bool IsNativeFrame { get; }

		/// <summary>
		/// true if it's a JIT-compiled frame (<see cref="IsILFrame"/> and <see cref="IsNativeFrame"/>
		/// are both true).
		/// </summary>
		bool IsJITCompiledFrame { get; }

		/// <summary>
		/// true if it's an internal frame
		/// </summary>
		bool IsInternalFrame { get; }

		/// <summary>
		/// true if this is a runtime unwindable frame
		/// </summary>
		bool IsRuntimeUnwindableFrame { get; }

		/// <summary>
		/// Gets the IL frame IP. Only valid if <see cref="IsILFrame"/> is true
		/// </summary>
		ILFrameIP ILFrameIP { get; }

		/// <summary>
		/// Gets the native frame IP. Only valid if <see cref="IsNativeFrame"/> is true. Writing
		/// a new value will neuter this instance.
		/// </summary>
		uint NativeOffset { get; set; }

		/// <summary>
		/// Gets the internal frame type or <see cref="Debugger.InternalFrameType.None"/>
		/// if it's not an internal frame
		/// </summary>
		InternalFrameType InternalFrameType { get; }

		/// <summary>
		/// Gets the stack frame index
		/// </summary>
		int Index { get; }

		/// <summary>
		/// Gets the method or null
		/// </summary>
		IDebuggerMethod Method { get; }

		/// <summary>
		/// Gets the IL code or null
		/// </summary>
		IDebuggerCode ILCode { get; }

		/// <summary>
		/// Gets the code or null
		/// </summary>
		IDebuggerCode Code { get; }

		/// <summary>
		/// Gets all arguments
		/// </summary>
		IDebuggerValue[] Arguments { get; }

		/// <summary>
		/// Gets all locals
		/// </summary>
		IDebuggerValue[] Locals { get; }

		/// <summary>
		/// Gets all generic type and/or method arguments. The first returned values are the generic
		/// type args, followed by the generic method args. See also
		/// <see cref="GenericTypeArguments"/>, <see cref="GenericMethodArguments"/> and
		/// <see cref="GetGenericArguments(out List{IDebuggerType}, out List{IDebuggerType})"/>
		/// </summary>
		IDebuggerType[] GenericArguments { get; }

		/// <summary>
		/// Gets all generic type arguments
		/// </summary>
		IDebuggerType[] GenericTypeArguments { get; }

		/// <summary>
		/// Gets all generic method arguments
		/// </summary>
		IDebuggerType[] GenericMethodArguments { get; }

		/// <summary>
		/// Gets a local variable or null if <see cref="IsILFrame"/> is false
		/// </summary>
		/// <param name="index">Index of local</param>
		/// <returns></returns>
		IDebuggerValue GetLocal(uint index);

		/// <summary>
		/// Gets a local variable or null if <see cref="IsILFrame"/> is false
		/// </summary>
		/// <param name="index">Index of local</param>
		/// <returns></returns>
		IDebuggerValue GetLocal(int index);

		/// <summary>
		/// Gets an argument or null if <see cref="IsILFrame"/> is false
		/// </summary>
		/// <param name="index">Index of argument</param>
		/// <returns></returns>
		IDebuggerValue GetArgument(uint index);

		/// <summary>
		/// Gets an argument or null if <see cref="IsILFrame"/> is false
		/// </summary>
		/// <param name="index">Index of argument</param>
		/// <returns></returns>
		IDebuggerValue GetArgument(int index);

		/// <summary>
		/// Gets all locals
		/// </summary>
		/// <param name="kind">Kind</param>
		IDebuggerValue[] GetLocals(ILCodeKind kind);

		/// <summary>
		/// Gets a local variable or null
		/// </summary>
		/// <param name="kind">Kind</param>
		/// <param name="index">Index of local</param>
		/// <returns></returns>
		IDebuggerValue GetLocal(ILCodeKind kind, uint index);

		/// <summary>
		/// Gets a local variable or null
		/// </summary>
		/// <param name="kind">Kind</param>
		/// <param name="index">Index of local</param>
		/// <returns></returns>
		IDebuggerValue GetLocal(ILCodeKind kind, int index);

		/// <summary>
		/// Gets the code or null
		/// </summary>
		/// <param name="kind">Kind</param>
		/// <returns></returns>
		IDebuggerCode GetCode(ILCodeKind kind);

		/// <summary>
		/// Splits up <see cref="GenericArguments"/> into generic type and method arguments
		/// </summary>
		/// <param name="typeGenArgs">Gets updated with a list containing all generic type arguments</param>
		/// <param name="methGenArgs">Gets updated with a list containing all generic method arguments</param>
		/// <returns></returns>
		bool GetGenericArguments(out List<IDebuggerType> typeGenArgs, out List<IDebuggerType> methGenArgs);

		/// <summary>
		/// Step into the method
		/// </summary>
		void StepInto();

		/// <summary>
		/// Step into the method and call <see cref="IDebugger.WaitAsync(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> StepIntoAsync(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step into the method and call <see cref="IDebugger.Wait(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepIntoWait(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step into the method and call <see cref="IDebugger.Wait(CancellationToken, int)"/>
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepIntoWait(CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step over the method
		/// </summary>
		void StepOver();

		/// <summary>
		/// Step over the method and call <see cref="IDebugger.WaitAsync(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> StepOverAsync(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step over the method and call <see cref="IDebugger.Wait(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepOverWait(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step over the method and call <see cref="IDebugger.Wait(CancellationToken, int)"/>
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepOverWait(CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step out of the method
		/// </summary>
		void StepOut();

		/// <summary>
		/// Step out of the method and call <see cref="IDebugger.WaitAsync(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> StepOutAsync(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step out of the method and call <see cref="IDebugger.Wait(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepOutWait(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step out of the method and call <see cref="IDebugger.Wait(CancellationToken, int)"/>
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepOutWait(CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Let the program execute until it returns to this frame
		/// </summary>
		/// <returns></returns>
		bool RunTo();

		/// <summary>
		/// Let the program execute until it returns to this frame and call <see cref="IDebugger.WaitAsync(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> RunToAsync(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Let the program execute until it returns to this frame and call <see cref="IDebugger.Wait(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool RunToWait(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Let the program execute until it returns to this frame and call <see cref="IDebugger.Wait(CancellationToken, int)"/>
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool RunToWait(CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Set next instruction to execute. All <see cref="IStackFrame"/> and <see cref="IDebuggerValue"/>
		/// instances will be neutered and can't be used after this method returns.
		/// </summary>
		/// <param name="offset">New offset within the current method</param>
		/// <returns></returns>
		bool SetOffset(int offset);

		/// <summary>
		/// Set next instruction to execute. All <see cref="IStackFrame"/> and <see cref="IDebuggerValue"/>
		/// instances will be neutered and can't be used after this method returns.
		/// </summary>
		/// <param name="offset">New offset within the current method</param>
		/// <returns></returns>
		bool SetOffset(uint offset);

		/// <summary>
		/// Set next instruction to execute. All <see cref="IStackFrame"/> and <see cref="IDebuggerValue"/>
		/// instances will be neutered and can't be used after this method returns.
		/// </summary>
		/// <param name="offset">New offset within the current method</param>
		/// <returns></returns>
		bool SetNativeOffset(int offset);

		/// <summary>
		/// Set next instruction to execute. All <see cref="IStackFrame"/> and <see cref="IDebuggerValue"/>
		/// instances will be neutered and can't be used after this method returns.
		/// </summary>
		/// <param name="offset">New offset within the current method</param>
		/// <returns></returns>
		bool SetNativeOffset(uint offset);

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		IDebuggerValue ReadStaticField(IDebuggerField field);

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="cls">Class</param>
		/// <param name="token">Field token</param>
		/// <returns></returns>
		IDebuggerValue ReadStaticField(IDebuggerClass cls, uint token);

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="token">Field token</param>
		/// <returns></returns>
		IDebuggerValue ReadStaticField(IDebuggerType type, uint token);

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="cls">Class</param>
		/// <param name="name">Field name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerValue ReadStaticField(IDebuggerClass cls, string name, bool checkBaseClasses = true);

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="name">Field name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerValue ReadStaticField(IDebuggerType type, string name, bool checkBaseClasses = true);

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="type">Declaring type</param>
		/// <param name="field">Field</param>
		/// <returns></returns>
		IDebuggerValue ReadStaticField(IDebuggerType type, IDebuggerField field);

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
