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
using System.Collections.Generic;
using System.Reflection;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// A thread in the debugged process
	/// </summary>
	public interface IDebuggerThread {
		/// <summary>
		/// Unique id per debugger
		/// </summary>
		int UniqueId { get; }

		/// <summary>
		/// Gets the thread ID (calls ICorDebugThread::GetID()). This is not necessarily the OS
		/// thread ID in V2 or later, see <see cref="VolatileThreadId"/>
		/// </summary>
		int ThreadId { get; }

		/// <summary>
		/// Gets the OS thread ID (calls ICorDebugThread2::GetVolatileOSThreadID()) or -1. This value
		/// can change during execution of the thread.
		/// </summary>
		int VolatileThreadId { get; }

		/// <summary>
		/// true if the thread has exited
		/// </summary>
		bool HasExited { get; }

		/// <summary>
		/// Gets the AppDomain or null if none
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets all stack frames
		/// </summary>
		IEnumerable<IStackFrame> Frames { get; }

		/// <summary>
		/// Gets the active stack frame. See also <see cref="ActiveILFrame"/>
		/// </summary>
		IStackFrame ActiveFrame { get; }

		/// <summary>
		/// Gets the active IL frame. This is the first IL frame in the active thread, and is usually
		/// <see cref="ActiveFrame"/>
		/// </summary>
		IStackFrame ActiveILFrame { get; }

		/// <summary>
		/// Gets the current thread handle. It's owned by the CLR debugger. The handle may change as
		/// the process executes, and may be different for different parts of the thread.
		/// </summary>
		IntPtr Handle { get; }

		/// <summary>
		/// true if the thread is running
		/// </summary>
		bool IsRunning { get; set; }

		/// <summary>
		/// true if the thread is suspended
		/// </summary>
		bool IsSuspended { get; set; }

		/// <summary>
		/// Gets/sets the thread state
		/// </summary>
		ThreadState State { get; }

		/// <summary>
		/// true if a termination of the thread has been requested.
		/// </summary>
		bool StopRequested { get; }

		/// <summary>
		/// true if a suspension of the thread has been requested.
		/// </summary>
		bool SuspendRequested { get; }

		/// <summary>
		/// true if the thread is running in the background.
		/// </summary>
		bool IsBackground { get; }

		/// <summary>
		/// true if the thread has not started executing.
		/// </summary>
		bool IsUnstarted { get; }

		/// <summary>
		/// true if the thread has been terminated.
		/// </summary>
		bool IsStopped { get; }

		/// <summary>
		/// true if the thread is waiting for another thread to complete a task.
		/// </summary>
		bool IsWaitSleepJoin { get; }

		/// <summary>
		/// true if the thread has been suspended. Use <see cref="IsSuspended"/> instead of this property.
		/// </summary>
		bool IsUserStateSuspended { get; }

		/// <summary>
		/// true if the thread is at an unsafe point. That is, the thread is at a point in execution where it may block garbage collection.
		/// 
		/// Debug events may be dispatched from unsafe points, but suspending a thread at an unsafe point will very likely cause a deadlock until the thread is resumed. The safe and unsafe points are determined by the just-in-time (JIT) and garbage collection implementation.
		/// </summary>
		bool IsUnsafePoint { get; }

		/// <summary>
		/// true if the thread is from the thread pool.
		/// </summary>
		bool IsThreadPool { get; }

		/// <summary>
		/// Gets the user state of this thread
		/// </summary>
		ThreadUserState UserState { get; }

		/// <summary>
		/// Gets the CLR thread object
		/// </summary>
		IDebuggerValue Object { get; }

		/// <summary>
		/// Gets the current exception or null
		/// </summary>
		IDebuggerValue CurrentException { get; }

		/// <summary>
		/// Intercept the current exception on this thread
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <returns></returns>
		bool InterceptCurrentException(IStackFrame frame);

		/// <summary>
		/// Gets the active chain or null
		/// </summary>
		IStackChain ActiveChain { get; }

		/// <summary>
		/// Gets all chains
		/// </summary>
		IEnumerable<IStackChain> Chains { get; }

		/// <summary>
		/// Calls a function in the debugged process
		/// </summary>
		/// <param name="func">Function to call</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// If this is an instance method, the first argument must be the <c>this</c> pointer. Use
		/// <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(IDebuggerFunction func, params object[] args);

		/// <summary>
		/// Calls a function in the debugged process
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="methodName">Method name</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// If this is an instance method, the first argument must be the <c>this</c> pointer. Use
		/// <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(string modName, string className, string methodName, params object[] args);

		/// <summary>
		/// Calls a function in the debugged process
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="token">Method token</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// If this is an instance method, the first argument must be the <c>this</c> pointer. Use
		/// <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(string modName, uint token, params object[] args);

		/// <summary>
		/// Calls a function in the debugged process
		/// </summary>
		/// <param name="genericArgs">Generic type arguments followed by generic method arguments</param>
		/// <param name="func">Function to call</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// If this is an instance method, the first argument must be the <c>this</c> pointer. Use
		/// <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(IDebuggerType[] genericArgs, IDebuggerFunction func, params object[] args);

		/// <summary>
		/// Calls a function in the debugged process
		/// </summary>
		/// <param name="genericArgs">Generic type arguments followed by generic method arguments</param>
		/// <param name="func">Function to call</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// If this is an instance method, the first argument must be the <c>this</c> pointer. Use
		/// <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(Type[] genericArgs, IDebuggerFunction func, params object[] args);

		/// <summary>
		/// Calls a function in the debugged process
		/// </summary>
		/// <param name="genericArgs">Generic type arguments followed by generic method arguments</param>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="methodName">Method name</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// If this is an instance method, the first argument must be the <c>this</c> pointer. Use
		/// <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(IDebuggerType[] genericArgs, string modName, string className, string methodName, params object[] args);

		/// <summary>
		/// Calls a function in the debugged process
		/// </summary>
		/// <param name="genericArgs">Generic type arguments followed by generic method arguments</param>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="methodName">Method name</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// If this is an instance method, the first argument must be the <c>this</c> pointer. Use
		/// <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(Type[] genericArgs, string modName, string className, string methodName, params object[] args);

		/// <summary>
		/// Calls a function in the debugged process
		/// </summary>
		/// <param name="genericArgs">Generic type arguments followed by generic method arguments</param>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="token">Method token</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// If this is an instance method, the first argument must be the <c>this</c> pointer. Use
		/// <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(IDebuggerType[] genericArgs, string modName, uint token, params object[] args);

		/// <summary>
		/// Calls a function in the debugged process
		/// </summary>
		/// <param name="genericArgs">Generic type arguments followed by generic method arguments</param>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="token">Method token</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// If this is an instance method, the first argument must be the <c>this</c> pointer. Use
		/// <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(Type[] genericArgs, string modName, uint token, params object[] args);

		/// <summary>
		/// Loads the assembly in the debugged process by calling <see cref="Assembly.Load(byte[])"/>
		/// </summary>
		/// <param name="assembly">Assembly bytes</param>
		IDebuggerValue AssemblyLoad(byte[] assembly);

		/// <summary>
		/// Loads the assembly in the debugged process by calling <see cref="Assembly.LoadFile(string)"/>
		/// </summary>
		/// <param name="filename">Filename</param>
		IDebuggerValue AssemblyLoadFile(string filename);

		/// <summary>
		/// Calls <paramref name="value"/>'s <see cref="object.ToString"/> method
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		string ToString(IDebuggerValue value);

		/// <summary>
		/// Creates an evaluator
		/// </summary>
		/// <returns></returns>
		IEval CreateEval();
	}
}
