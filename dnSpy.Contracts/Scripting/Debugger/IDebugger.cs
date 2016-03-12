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
using System.Threading;
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
		/// Waits until the debugged process is paused or terminated. Returns true if the wait
		/// didn't time out.
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		bool Wait(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Waits until the debugged process is paused or terminated.
		/// </summary>
		/// <param name="token">Cancellation token</param>
		void Wait(CancellationToken token);

		/// <summary>
		/// Waits until the debugged process is not paused. Returns true if the wait didn't time out.
		/// </summary>
		/// <param name="millisecondsTimeout">Millisecs to wait or -1 (<see cref="Timeout.Infinite"/>)
		/// to wait indefinitely</param>
		bool WaitRun(int millisecondsTimeout = Timeout.Infinite);

		/// <summary>
		/// Waits until the debugged process is not paused.
		/// </summary>
		/// <param name="token">Cancellation token</param>
		void WaitRun(CancellationToken token);

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
		/// Step into the method
		/// </summary>
		void StepInto();

		/// <summary>
		/// Step into the method
		/// </summary>
		/// <param name="frame">Frame</param>
		void StepInto(IStackFrame frame);

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
		/// Step out of the method
		/// </summary>
		void StepOut();

		/// <summary>
		/// Step out of the method
		/// </summary>
		/// <param name="frame">Frame</param>
		void StepOut(IStackFrame frame);

		/// <summary>
		/// Let the program execute until it returns to <paramref name="frame"/>
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <returns></returns>
		bool RunTo(IStackFrame frame);

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
		IEnumerable<IThread> Threads { get; }

		/// <summary>
		/// Gets/sets the active thread shown in the UI
		/// </summary>
		IThread ActiveThread { get; set; }

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
		int ActiveStackFrameIndex { get; set; }

		/// <summary>
		/// Gets all AppDomains
		/// </summary>
		IEnumerable<IAppDomain> AppDomains { get; }

		/// <summary>
		/// Gets the first <see cref="IAppDomain"/> in <see cref="AppDomains"/>
		/// </summary>
		IAppDomain FirstAppDomain { get; }

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
		void BreakOnLoad(string name, Action action = null);

		/// <summary>
		/// Breaks when an assembly gets loaded
		/// </summary>
		/// <param name="assemblyName">Assembly name or just part of the full name</param>
		/// <param name="action">Called (on the UI thread) when the module gets loaded</param>
		/// <param name="flags">Assembly name comparer flags</param>
		void BreakOnLoadAssembly(string assemblyName, Action action = null, AssemblyNameComparerFlags flags = AssemblyNameComparerFlags.Name);

		/// <summary>
		/// Breaks when an assembly gets loaded
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <param name="action">Called (on the UI thread) when the module gets loaded</param>
		/// <param name="flags">Assembly name comparer flags</param>
		void BreakOnLoadAssembly(IAssembly assembly, Action action = null, AssemblyNameComparerFlags flags = AssemblyNameComparerFlags.All);

		/// <summary>
		/// Removes a breakpoint
		/// </summary>
		/// <param name="bp">Breakpoint</param>
		void Remove(IBreakpoint bp);
	}
}
