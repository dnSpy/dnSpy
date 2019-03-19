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
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Contracts.Debugger.Engine.Steppers;

namespace dnSpy.Contracts.Debugger.Engine {
	/// <summary>
	/// A debug engine contains all the logic to control the debugged process
	/// </summary>
	public abstract class DbgEngine : DbgObject {
		/// <summary>
		/// How was the debugged process started?
		/// </summary>
		public abstract DbgStartKind StartKind { get; }

		/// <summary>
		/// Contains properties used to create a <see cref="DbgRuntime"/>. This property is called
		/// after the engine has connected and just before the <see cref="DbgRuntime"/> instance is
		/// created.
		/// </summary>
		public abstract DbgEngineRuntimeInfo RuntimeInfo { get; }

		/// <summary>
		/// Gets all debug tags, see <see cref="PredefinedDebugTags"/>
		/// </summary>
		public abstract string[] DebugTags { get; }

		/// <summary>
		/// What is being debugged. This is shown in the UI (eg. Processes window)
		/// </summary>
		public abstract string[] Debugging { get; }

		/// <summary>
		/// Called when the engine can start or attach to the debugged process. This method is called shortly after
		/// this instance gets created by a call to <see cref="DbgEngineProvider.Create(DbgManager, DebugProgramOptions)"/>.
		/// It must send a <see cref="DbgMessageConnected"/> message when it has connected to the program or
		/// if it failed.
		/// </summary>
		/// <param name="options">Same options that were passed to <see cref="DbgEngineProvider.Create(DbgManager, DebugProgramOptions)"/></param>
		public abstract void Start(DebugProgramOptions options);

		/// <summary>
		/// Raised when there's a new message. It can be raised in any thread.
		/// </summary>
		public abstract event EventHandler<DbgEngineMessage> Message;

		/// <summary>
		/// Creates a <see cref="DbgInternalRuntime"/> instance. It's called by the runtime constructor.
		/// </summary>
		/// <param name="runtime">Runtime instance</param>
		/// <returns></returns>
		public abstract DbgInternalRuntime CreateInternalRuntime(DbgRuntime runtime);

		/// <summary>
		/// Called when its connected message has been received by <see cref="DbgManager"/>
		/// </summary>
		/// <param name="objectFactory">Object factory</param>
		/// <param name="runtime">Runtime</param>
		public abstract void OnConnected(DbgObjectFactory objectFactory, DbgRuntime runtime);

		/// <summary>
		/// Pauses the debugged program, if it's not already paused. This is an asynchronous method.
		/// Once the program is paused (even if it already was paused), message <see cref="DbgMessageBreak"/>
		/// must be sent.
		/// </summary>
		public abstract void Break();

		/// <summary>
		/// Lets the program run again. This is an asynchronous method. No message is sent.
		/// </summary>
		public abstract void Run();

		/// <summary>
		/// Terminates the debugged program. This is an asynchronous method.
		/// 
		/// This method gets called when the user chooses Terminate All from the Debug menu
		/// 
		/// When the program has been terminated or detached, message <see cref="DbgMessageDisconnected"/>
		/// must be sent.
		/// </summary>
		public abstract void Terminate();

		/// <summary>
		/// true if the engine can detach from the debugged program without terminating it.
		/// </summary>
		public abstract bool CanDetach { get; }

		/// <summary>
		/// Detaches from the debugged program. If it's not possible, the program must be terminated.
		/// This is an asynchronous method.
		/// 
		/// This method gets called when the user chooses Detach All from the Debug menu.
		/// 
		/// When the program has been terminated or detached, message <see cref="DbgMessageDisconnected"/>
		/// must be sent.
		/// </summary>
		public abstract void Detach();

		/// <summary>
		/// Freezes the thread
		/// </summary>
		/// <param name="thread">Thread</param>
		public abstract void Freeze(DbgThread thread);

		/// <summary>
		/// Thaws the thread
		/// </summary>
		/// <param name="thread">Thread</param>
		public abstract void Thaw(DbgThread thread);

		/// <summary>
		/// Removes all breakpoints. There's no guarantee that this method will be called to delete all bound breakpoints.
		/// To always get notified when a bound breakpoint gets deleted, add custom data that implements <see cref="IDisposable"/>
		/// when creating the bound breakpoint, see eg. <see cref="DbgObjectFactory.Create{T}(DbgBoundCodeBreakpointInfo{T}[])"/>
		/// </summary>
		/// <param name="modules">Modules</param>
		/// <param name="boundBreakpoints">Bound breakpoints</param>
		/// <param name="includeNonModuleBreakpoints">If false, remove breakpoints that exist in <paramref name="modules"/>, and if true,
		/// remove breakpoints that exist in <paramref name="modules"/> and all breakpoints not in a <see cref="DbgModule"/></param>
		public abstract void RemoveBreakpoints(DbgModule[] modules, DbgBoundCodeBreakpoint[] boundBreakpoints, bool includeNonModuleBreakpoints);

		/// <summary>
		/// Adds all breakpoints. To get notified when a bound breakpoint gets deleted, add custom data that implements
		/// <see cref="IDisposable"/> when creating the bound breakpoint, see eg. <see cref="DbgObjectFactory.Create{T}(DbgBoundCodeBreakpointInfo{T}[])"/>
		/// </summary>
		/// <param name="modules">Modules</param>
		/// <param name="locations">Breakpoint locations. The engine can ignore non-supported locations.</param>
		/// <param name="includeNonModuleBreakpoints">If false, add breakpoints that exist in <paramref name="modules"/>, and if true,
		/// add breakpoints that exist in <paramref name="modules"/> and all breakpoints not in a <see cref="DbgModule"/></param>
		public abstract void AddBreakpoints(DbgModule[] modules, DbgCodeLocation[] locations, bool includeNonModuleBreakpoints);

		/// <summary>
		/// Creates a stack walker
		/// </summary>
		/// <param name="thread">Thread created by this engine</param>
		/// <returns></returns>
		public abstract DbgEngineStackWalker CreateStackWalker(DbgThread thread);

		/// <summary>
		/// Creates a stepper
		/// </summary>
		/// <param name="thread">Thread to step</param>
		/// <returns></returns>
		public abstract DbgEngineStepper CreateStepper(DbgThread thread);

		/// <summary>
		/// Sets a new instruction pointer
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <param name="location">New location</param>
		public abstract void SetIP(DbgThread thread, DbgCodeLocation location);

		/// <summary>
		/// Checks if <see cref="SetIP(DbgThread, DbgCodeLocation)"/> can be called
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <param name="location">New location</param>
		/// <returns></returns>
		public abstract bool CanSetIP(DbgThread thread, DbgCodeLocation location);
	}

	/// <summary>
	/// Start kind
	/// </summary>
	public enum DbgStartKind {
		/// <summary>
		/// The program was started by the debugger
		/// </summary>
		Start,

		/// <summary>
		/// The debugger attached to the program after it was started by someone else
		/// </summary>
		Attach,
	}
}
