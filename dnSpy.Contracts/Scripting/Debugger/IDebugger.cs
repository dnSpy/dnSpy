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
using System.Threading;
using System.Threading.Tasks;
using dnlib.DotNet;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Script access to the debugger plugin
	/// </summary>
	public interface IDebugger {
		/// <summary>
		/// Notified when <see cref="State"/> gets changed. This gets notified on the main UI
		/// thread.
		/// </summary>
		event EventHandler<DebuggerEventArgs> OnProcessStateChanged;

		/// <summary>
		/// Gets the debugger process state
		/// </summary>
		DebuggerProcessState State { get; }

		/// <summary>
		/// Gets all <see cref="DebuggerPauseState"/>s
		/// </summary>
		DebuggerPauseState[] PauseStates { get; }

		/// <summary>
		/// Gets the first <see cref="Debugger.PauseReason"/> from <see cref="PauseStates"/>
		/// </summary>
		PauseReason PauseReason { get; }

		/// <summary>
		/// true if <see cref="State"/> equals <see cref="DebuggerProcessState.Starting"/>
		/// </summary>
		bool IsStarting { get; }

		/// <summary>
		/// true if <see cref="State"/> equals <see cref="DebuggerProcessState.Continuing"/>
		/// </summary>
		bool IsContinuing { get; }

		/// <summary>
		/// true if <see cref="State"/> equals <see cref="DebuggerProcessState.Running"/>
		/// </summary>
		bool IsRunning { get; }

		/// <summary>
		/// true if <see cref="State"/> equals <see cref="DebuggerProcessState.Paused"/>
		/// </summary>
		bool IsPaused { get; }

		/// <summary>
		/// true if <see cref="State"/> equals <see cref="DebuggerProcessState.Terminated"/>
		/// </summary>
		bool IsTerminated { get; }

		/// <summary>
		/// true if we're debugging
		/// </summary>
		bool IsDebugging { get; }

		/// <summary>
		/// true if we attached to the debugged process
		/// </summary>
		bool HasAttached { get; }

		/// <summary>
		/// true if we're evaluating (eg. a property in the debugged process is being called by the
		/// debugger)
		/// </summary>
		bool IsEvaluating { get; }

		/// <summary>
		/// true if an eval has completed
		/// </summary>
		bool EvalCompleted { get; }

		/// <summary>
		/// Waits until the debugged process is paused or terminated. Returns true if the wait didn't time out.
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> WaitAsync(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Waits until the debugged process is not paused. Returns true if the wait didn't time out.
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> WaitRunAsync(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Waits until the debugged process is paused or terminated. Returns true if the wait didn't time out.
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool Wait(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Waits until the debugged process is paused or terminated. Returns true if the wait didn't time out.
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool Wait(CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Waits until the debugged process is not paused. Returns true if the wait didn't time out.
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool WaitRun(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Waits until the debugged process is not paused. Returns true if the wait didn't time out.
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool WaitRun(CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Shows a dialog box and debugs the selected process
		/// </summary>
		/// <returns></returns>
		bool Start();

		/// <summary>
		/// Debug a program
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		bool Start(DebugOptions options);

		/// <summary>
		/// Debug a program
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="cmdLine">Command line</param>
		/// <param name="cwd">Working directory</param>
		/// <param name="breakKind">Break kind</param>
		/// <returns></returns>
		bool Start(string filename, string cmdLine = null, string cwd = null, BreakProcessKind breakKind = BreakProcessKind.None);

		/// <summary>
		/// Shows a dialog box and debugs the selected CoreCLR process
		/// </summary>
		/// <returns></returns>
		bool StartCoreCLR();

		/// <summary>
		/// Debug a CoreCLR program
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		bool StartCoreCLR(CoreCLRDebugOptions options);

		/// <summary>
		/// Debug a CoreCLR program
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="cmdLine">Command line</param>
		/// <param name="cwd">Working directory</param>
		/// <param name="breakKind">Break kind</param>
		/// <param name="hostFilename">Host filename, eg. path to <c>CoreRun.exe</c></param>
		/// <param name="hostCommandLine">Host command line</param>
		/// <returns></returns>
		bool StartCoreCLR(string filename, string cmdLine = null, string cwd = null, BreakProcessKind breakKind = BreakProcessKind.None, string hostFilename = null, string hostCommandLine = null);

		/// <summary>
		/// Shows a dialog box and attaches to the selected process
		/// </summary>
		/// <returns></returns>
		bool Attach();

		/// <summary>
		/// Attach to a process
		/// </summary>
		/// <param name="options"></param>
		bool Attach(AttachOptions options);

		/// <summary>
		/// Attach to a process
		/// </summary>
		/// <param name="pid">Process id</param>
		bool Attach(int pid);

		/// <summary>
		/// Restart the debugged process
		/// </summary>
		void Restart();

		/// <summary>
		/// Break the process
		/// </summary>
		void Break();

		/// <summary>
		/// Stop and kill the process
		/// </summary>
		void Stop();

		/// <summary>
		/// Detach
		/// </summary>
		void Detach();

		/// <summary>
		/// Let the debugged program run
		/// </summary>
		void Continue();

		/// <summary>
		/// Let the debugged program run and call <see cref="WaitAsync(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> ContinueAsync(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Let the debugged program run and call <see cref="Wait(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool ContinueWait(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Let the debugged program run and call <see cref="Wait(CancellationToken, int)"/>
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool ContinueWait(CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step into the method
		/// </summary>
		void StepInto();

		/// <summary>
		/// Step into the method
		/// </summary>
		/// <param name="frame">Frame</param>
		void StepInto(IStackFrame frame);

		/// <summary>
		/// Step into the method and call <see cref="WaitAsync(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> StepIntoAsync(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step into the method and call <see cref="Wait(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepIntoWait(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step into the method and call <see cref="Wait(CancellationToken, int)"/>
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepIntoWait(CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step into the method and call <see cref="WaitAsync(int)"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> StepIntoAsync(IStackFrame frame, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step into the method and call <see cref="Wait(int)"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepIntoWait(IStackFrame frame, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step into the method and call <see cref="Wait(CancellationToken, int)"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepIntoWait(IStackFrame frame, CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step over the method
		/// </summary>
		void StepOver();

		/// <summary>
		/// Step over the method
		/// </summary>
		/// <param name="frame">Frame</param>
		void StepOver(IStackFrame frame);

		/// <summary>
		/// Step over the method and call <see cref="WaitAsync(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> StepOverAsync(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step over the method and call <see cref="Wait(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepOverWait(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step over the method and call <see cref="Wait(CancellationToken, int)"/>
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepOverWait(CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step over the method and call <see cref="WaitAsync(int)"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> StepOverAsync(IStackFrame frame, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step over the method and call <see cref="Wait(int)"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepOverWait(IStackFrame frame, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step over the method and call <see cref="Wait(CancellationToken, int)"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepOverWait(IStackFrame frame, CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step out of the method
		/// </summary>
		void StepOut();

		/// <summary>
		/// Step out of the method
		/// </summary>
		/// <param name="frame">Frame</param>
		void StepOut(IStackFrame frame);

		/// <summary>
		/// Step out of the method and call <see cref="WaitAsync(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> StepOutAsync(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step out of the method and call <see cref="Wait(int)"/>
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepOutWait(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step out of the method and call <see cref="Wait(CancellationToken, int)"/>
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepOutWait(CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step out of the method and call <see cref="WaitAsync(int)"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> StepOutAsync(IStackFrame frame, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step out of the method and call <see cref="Wait(int)"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepOutWait(IStackFrame frame, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Step out of the method and call <see cref="Wait(CancellationToken, int)"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool StepOutWait(IStackFrame frame, CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Let the program execute until it returns to <paramref name="frame"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <returns></returns>
		bool RunTo(IStackFrame frame);

		/// <summary>
		/// Let the program execute until it returns to <paramref name="frame"/> and call <see cref="WaitAsync(int)"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		Task<bool> RunToAsync(IStackFrame frame, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Let the program execute until it returns to <paramref name="frame"/> and call <see cref="Wait(int)"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool RunToWait(IStackFrame frame, int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Let the program execute until it returns to <paramref name="frame"/> and call <see cref="Wait(CancellationToken, int)"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="token">Cancellation token</param>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		/// <returns></returns>
		bool RunToWait(IStackFrame frame, CancellationToken token, int millisecondsTimeout = Timeout.Infinite);

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
		/// Set next instruction to execute. All <see cref="IStackFrame"/> and <see cref="IDebuggerValue"/>
		/// instances will be neutered and can't be used after this method returns.
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="offset">New offset within the current method</param>
		/// <returns></returns>
		bool SetOffset(IStackFrame frame, int offset);

		/// <summary>
		/// Set next instruction to execute. All <see cref="IStackFrame"/> and <see cref="IDebuggerValue"/>
		/// instances will be neutered and can't be used after this method returns.
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="offset">New offset within the current method</param>
		/// <returns></returns>
		bool SetOffset(IStackFrame frame, uint offset);

		/// <summary>
		/// Set next instruction to execute. All <see cref="IStackFrame"/> and <see cref="IDebuggerValue"/>
		/// instances will be neutered and can't be used after this method returns.
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="offset">New offset within the current method</param>
		/// <returns></returns>
		bool SetNativeOffset(IStackFrame frame, int offset);

		/// <summary>
		/// Set next instruction to execute. All <see cref="IStackFrame"/> and <see cref="IDebuggerValue"/>
		/// instances will be neutered and can't be used after this method returns.
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="offset">New offset within the current method</param>
		/// <returns></returns>
		bool SetNativeOffset(IStackFrame frame, uint offset);

		/// <summary>
		/// Gets all threads
		/// </summary>
		IEnumerable<IDebuggerThread> Threads { get; }

		/// <summary>
		/// Gets/sets the active thread shown in the UI
		/// </summary>
		IDebuggerThread ActiveThread { get; set; }

		/// <summary>
		/// Gets/sets the active stack frame of the active thread shown in the UI. See also
		/// <see cref="ActiveILFrame"/>
		/// </summary>
		IStackFrame ActiveFrame { get; set; }

		/// <summary>
		/// Gets the active IL frame. This is the first IL frame in the active thread, and is usually
		/// <see cref="ActiveFrame"/>
		/// </summary>
		IStackFrame ActiveILFrame { get; }

		/// <summary>
		/// Gets/sets the active stack frame index of the active thread shown in the UI
		/// </summary>
		int ActiveFrameIndex { get; set; }

		/// <summary>
		/// Gets all AppDomains
		/// </summary>
		IEnumerable<IAppDomain> AppDomains { get; }

		/// <summary>
		/// Gets the first <see cref="IAppDomain"/> in <see cref="AppDomains"/>
		/// </summary>
		IAppDomain FirstAppDomain { get; }

		/// <summary>
		/// Gets all assemblies in <see cref="FirstAppDomain"/>
		/// </summary>
		IEnumerable<IDebuggerAssembly> Assemblies { get; }

		/// <summary>
		/// Gets all modules in <see cref="FirstAppDomain"/>
		/// </summary>
		IEnumerable<IDebuggerModule> Modules { get; }

		/// <summary>
		/// Gets the core module (mscorlib) in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerModule CorLib { get; }

		/// <summary>
		/// Finds a module in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		IDebuggerModule GetModule(Module module);

		/// <summary>
		/// Finds a module in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="name">Module name</param>
		/// <returns></returns>
		IDebuggerModule GetModule(ModuleName name);

		/// <summary>
		/// Finds a module in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="name">Full path, filename, or filename without extension of module</param>
		/// <returns></returns>
		IDebuggerModule GetModuleByName(string name);

		/// <summary>
		/// Finds an assembly in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="asm">Assembly</param>
		/// <returns></returns>
		IDebuggerAssembly GetAssembly(Assembly asm);

		/// <summary>
		/// Finds an assembly in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="name">Full path, filename, or filename without extension of assembly, or
		/// assembly simple name or assembly full name</param>
		/// <returns></returns>
		IDebuggerAssembly GetAssembly(string name);

		/// <summary>
		/// Finds a class in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <returns></returns>
		IDebuggerClass GetClass(string modName, string className);

		/// <summary>
		/// Finds a method in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="methodName">Method name</param>
		/// <returns></returns>
		IDebuggerMethod GetMethod(string modName, string className, string methodName);

		/// <summary>
		/// Finds a field in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="fieldName">Field name</param>
		/// <returns></returns>
		IDebuggerField GetField(string modName, string className, string fieldName);

		/// <summary>
		/// Finds a property in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="propertyName">Property name</param>
		/// <returns></returns>
		IDebuggerProperty GetProperty(string modName, string className, string propertyName);

		/// <summary>
		/// Finds an event in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="eventName">Event name</param>
		/// <returns></returns>
		IDebuggerEvent GetEvent(string modName, string className, string eventName);

		/// <summary>
		/// Finds a type in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <returns></returns>
		IDebuggerType GetType(string modName, string className);

		/// <summary>
		/// Finds a type in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="genericArguments">Generic arguments</param>
		/// <returns></returns>
		IDebuggerType GetType(string modName, string className, params IDebuggerType[] genericArguments);

		/// <summary>
		/// Finds a type in <see cref="FirstAppDomain"/>
		/// </summary>
		/// <param name="type">A type that must exist in one of the loaded assemblies in the
		/// debugged process.</param>
		/// <returns></returns>
		IDebuggerType GetType(Type type);

		/// <summary>
		/// Gets a field
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		IDebuggerField GetField(FieldInfo field);

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		IDebuggerMethod GetMethod(MethodBase method);

		/// <summary>
		/// Gets a property
		/// </summary>
		/// <param name="prop">Property</param>
		/// <returns></returns>
		IDebuggerProperty GetProperty(PropertyInfo prop);

		/// <summary>
		/// Gets an event
		/// </summary>
		/// <param name="evt">Event</param>
		/// <returns></returns>
		IDebuggerEvent GetEvent(EventInfo evt);

		/// <summary>
		/// Gets type <see cref="Void"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType Void { get; }

		/// <summary>
		/// Gets type <see cref="bool"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType Boolean { get; }

		/// <summary>
		/// Gets type <see cref="char"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType Char { get; }

		/// <summary>
		/// Gets type <see cref="sbyte"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType SByte { get; }

		/// <summary>
		/// Gets type <see cref="byte"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType Byte { get; }

		/// <summary>
		/// Gets type <see cref="short"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType Int16 { get; }

		/// <summary>
		/// Gets type <see cref="ushort"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType UInt16 { get; }

		/// <summary>
		/// Gets type <see cref="int"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType Int32 { get; }

		/// <summary>
		/// Gets type <see cref="uint"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType UInt32 { get; }

		/// <summary>
		/// Gets type <see cref="long"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType Int64 { get; }

		/// <summary>
		/// Gets type <see cref="ulong"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType UInt64 { get; }

		/// <summary>
		/// Gets type <see cref="float"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType Single { get; }

		/// <summary>
		/// Gets type <see cref="double"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType Double { get; }

		/// <summary>
		/// Gets type <see cref="string"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType String { get; }

		/// <summary>
		/// Gets type <see cref="TypedReference"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType TypedReference { get; }

		/// <summary>
		/// Gets type <see cref="IntPtr"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType IntPtr { get; }

		/// <summary>
		/// Gets type <see cref="UIntPtr"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType UIntPtr { get; }

		/// <summary>
		/// Gets type <see cref="Object"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType Object { get; }

		/// <summary>
		/// Gets type <see cref="decimal"/> in <see cref="FirstAppDomain"/>
		/// </summary>
		IDebuggerType Decimal { get; }

		/// <summary>
		/// Creates an IL code breakpoint that's only valid for the current debugging session (or
		/// the next one if we're not debugging). The breakpoint is not added to the breakpoints
		/// shown in the UI.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Method token</param>
		/// <param name="offset">IL code offset in method</param>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		IILBreakpoint CreateBreakpoint(ModuleName module, uint token, uint offset = 0, Func<IILBreakpoint, bool> cond = null);

		/// <summary>
		/// Creates an IL code breakpoint that's only valid for the current debugging session (or
		/// the next one if we're not debugging). The breakpoint is not added to the breakpoints
		/// shown in the UI.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Method token</param>
		/// <param name="offset">IL code offset in method</param>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		IILBreakpoint CreateBreakpoint(ModuleName module, uint token, int offset, Func<IILBreakpoint, bool> cond = null);

		/// <summary>
		/// Creates a native code breakpoint that's only valid for the current debugging session.
		/// The breakpoint is not added to the breakpoints shown in the UI. The method must have
		/// been jitted or setting the breakpoint will fail.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Method token</param>
		/// <param name="offset">Native code offset in method</param>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		INativeBreakpoint CreateNativeBreakpoint(ModuleName module, uint token, uint offset = 0, Func<INativeBreakpoint, bool> cond = null);

		/// <summary>
		/// Creates a native code breakpoint that's only valid for the current debugging session.
		/// The breakpoint is not added to the breakpoints shown in the UI. The method must have
		/// been jitted or setting the breakpoint will fail.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Method token</param>
		/// <param name="offset">Native code offset in method</param>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		INativeBreakpoint CreateNativeBreakpoint(ModuleName module, uint token, int offset, Func<INativeBreakpoint, bool> cond = null);

		/// <summary>
		/// Creates a native code breakpoint that's only valid for the current debugging session (or
		/// the next one if we're not debugging). The breakpoint is not added to the breakpoints
		/// shown in the UI. The method must have been jitted or setting the breakpoint will fail.
		/// </summary>
		/// <param name="code">Native code</param>
		/// <param name="offset">Native code offset in method</param>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		INativeBreakpoint CreateNativeBreakpoint(IDebuggerCode code, uint offset = 0, Func<INativeBreakpoint, bool> cond = null);

		/// <summary>
		/// Creates a native code breakpoint that's only valid for the current debugging session (or
		/// the next one if we're not debugging). The breakpoint is not added to the breakpoints
		/// shown in the UI. The method must have been jitted or setting the breakpoint will fail.
		/// </summary>
		/// <param name="code">Native code</param>
		/// <param name="offset">Native code offset in method</param>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		INativeBreakpoint CreateNativeBreakpoint(IDebuggerCode code, int offset, Func<INativeBreakpoint, bool> cond = null);

		/// <summary>
		/// Creates an event breakpoint that's only valid for the current debugging session (or
		/// the next one if we're not debugging). The breakpoint is not added to the breakpoints
		/// shown in the UI.
		/// </summary>
		/// <param name="eventKind">Event</param>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		IEventBreakpoint CreateBreakpoint(DebugEventKind eventKind, Func<IEventBreakpoint, IDebugEventContext, bool> cond = null);

		/// <summary>
		/// Creates a <see cref="DebugEventKind.LoadModule"/> debug event breakpoint
		/// </summary>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		IEventBreakpoint CreateLoadModuleBreakpoint(Func<IEventBreakpoint, IModuleEventContext, bool> cond = null);

		/// <summary>
		/// Creates an any-event breakpoint that's only valid for the current debugging session (or
		/// the next one if we're not debugging). The breakpoint is not added to the breakpoints
		/// shown in the UI.
		/// </summary>
		/// <param name="cond">Returns true if the breakpoint should pause the debugged process. Called on the UI thread.</param>
		/// <returns></returns>
		IAnyEventBreakpoint CreateAnyEventBreakpoint(Func<IAnyEventBreakpoint, IDebugEventContext, bool> cond = null);

		/// <summary>
		/// Breaks when a module gets loaded
		/// </summary>
		/// <param name="name">Module name. Can be the full path or just the filename or filename
		/// without the extension</param>
		/// <param name="action">Called (on the UI thread) when the module gets loaded</param>
		void BreakOnLoad(string name, Action<IDebuggerModule> action = null);

		/// <summary>
		/// Breaks when an assembly gets loaded
		/// </summary>
		/// <param name="assemblyName">Assembly name or just part of the full name</param>
		/// <param name="action">Called (on the UI thread) when the module gets loaded</param>
		/// <param name="flags">Assembly name comparer flags</param>
		void BreakOnLoadAssembly(string assemblyName, Action<IDebuggerAssembly> action = null, AssemblyNameComparerFlags flags = AssemblyNameComparerFlags.Name);

		/// <summary>
		/// Breaks when an assembly gets loaded
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="action">Called (on the UI thread) when the module gets loaded</param>
		/// <param name="flags">Assembly name comparer flags</param>
		void BreakOnLoadAssembly(IAssembly assembly, Action<IDebuggerAssembly> action = null, AssemblyNameComparerFlags flags = AssemblyNameComparerFlags.All);

		/// <summary>
		/// Removes a breakpoint
		/// </summary>
		/// <param name="bp">Breakpoint</param>
		void Remove(IBreakpoint bp);

		/// <summary>
		/// Reads memory from the debugged process. Unmapped memory is read as 0s.
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="array">Destination</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to read</param>
		void Read(ulong address, byte[] array, long index, uint count);

		/// <summary>
		/// Reads memory from the debugged process. Unmapped memory is read as 0s.
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="array">Destination</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to read</param>
		void Read(ulong address, byte[] array, long index, int count);

		/// <summary>
		/// Reads memory from the debugged process. Unmapped memory is read as 0s.
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="count">Number of bytes to read</param>
		/// <returns></returns>
		byte[] Read(ulong address, uint count);

		/// <summary>
		/// Reads memory from the debugged process. Unmapped memory is read as 0s.
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="count">Number of bytes to read</param>
		/// <returns></returns>
		byte[] Read(ulong address, int count);

		/// <summary>
		/// Writes data to memory in the debugged process. Returns the number of bytes written.
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="array">Source</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to write</param>
		/// <returns></returns>
		uint Write(ulong address, byte[] array, long index, uint count);

		/// <summary>
		/// Writes data to memory in the debugged process. Returns the number of bytes written.
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="array">Source</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to write</param>
		/// <returns></returns>
		int Write(ulong address, byte[] array, long index, int count);

		/// <summary>
		/// Writes data to memory in the debugged process. Throws if all bytes couldn't be written.
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="array">Source</param>
		void Write(ulong address, byte[] array);

		/// <summary>
		/// Reads a <see cref="bool"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		bool ReadBoolean(ulong address);

		/// <summary>
		/// Reads a <see cref="char"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		char ReadChar(ulong address);

		/// <summary>
		/// Reads a <see cref="sbyte"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		sbyte ReadSByte(ulong address);

		/// <summary>
		/// Reads a <see cref="byte"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		byte ReadByte(ulong address);

		/// <summary>
		/// Reads a <see cref="short"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		short ReadInt16(ulong address);

		/// <summary>
		/// Reads a <see cref="ushort"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		ushort ReadUInt16(ulong address);

		/// <summary>
		/// Reads a <see cref="int"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		int ReadInt32(ulong address);

		/// <summary>
		/// Reads a <see cref="uint"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		uint ReadUInt32(ulong address);

		/// <summary>
		/// Reads a <see cref="long"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		long ReadInt64(ulong address);

		/// <summary>
		/// Reads a <see cref="ulong"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		ulong ReadUInt64(ulong address);

		/// <summary>
		/// Reads a <see cref="float"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		float ReadSingle(ulong address);

		/// <summary>
		/// Reads a <see cref="double"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		double ReadDouble(ulong address);

		/// <summary>
		/// Reads a <see cref="decimal"/> from an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		decimal ReadDecimal(ulong address);

		/// <summary>
		/// Writes a <see cref="bool"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, bool value);

		/// <summary>
		/// Writes a <see cref="char"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, char value);

		/// <summary>
		/// Writes a <see cref="sbyte"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, sbyte value);

		/// <summary>
		/// Writes a <see cref="byte"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, byte value);

		/// <summary>
		/// Writes a <see cref="short"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, short value);

		/// <summary>
		/// Writes a <see cref="ushort"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, ushort value);

		/// <summary>
		/// Writes a <see cref="int"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, int value);

		/// <summary>
		/// Writes a <see cref="uint"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, uint value);

		/// <summary>
		/// Writes a <see cref="long"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, long value);

		/// <summary>
		/// Writes a <see cref="ulong"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, ulong value);

		/// <summary>
		/// Writes a <see cref="float"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, float value);

		/// <summary>
		/// Writes a <see cref="double"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, double value);

		/// <summary>
		/// Writes a <see cref="decimal"/> to an address in the debugged process
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="value">Value</param>
		void Write(ulong address, decimal value);
	}
}
