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
using System.Collections.Generic;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Manages all debug engines. All events are raised on the dispatcher thread.
	/// If you need to hook events before debugging starts, you should export an <see cref="IDbgManagerStartListener"/>.
	/// It gets called when <see cref="Start(DebugProgramOptions)"/> gets called for the first time.
	/// </summary>
	public abstract class DbgManager {
		/// <summary>
		/// Gets the dispatcher. All debugger events are raised on this thread. <see cref="DbgObject.Close(DbgDispatcher)"/>
		/// is also called on this thread including disposing of data added by eg. <see cref="DbgObject.GetOrCreateData{T}()"/>.
		/// </summary>
		public abstract DbgDispatcher Dispatcher { get; }

		/// <summary>
		/// Raised on the debugger thread when there's a new message, eg. a process was created, a thread has exited, etc.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageEventArgs> Message;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageProcessCreatedEventArgs> MessageProcessCreated;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageProcessExitedEventArgs> MessageProcessExited;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageRuntimeCreatedEventArgs> MessageRuntimeCreated;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageRuntimeExitedEventArgs> MessageRuntimeExited;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageAppDomainLoadedEventArgs> MessageAppDomainLoaded;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageAppDomainUnloadedEventArgs> MessageAppDomainUnloaded;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageModuleLoadedEventArgs> MessageModuleLoaded;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageModuleUnloadedEventArgs> MessageModuleUnloaded;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageThreadCreatedEventArgs> MessageThreadCreated;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageThreadExitedEventArgs> MessageThreadExited;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageExceptionThrownEventArgs> MessageExceptionThrown;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageEntryPointBreakEventArgs> MessageEntryPointBreak;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageProgramMessageEventArgs> MessageProgramMessage;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageBoundBreakpointEventArgs> MessageBoundBreakpoint;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageProgramBreakEventArgs> MessageProgramBreak;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageStepCompleteEventArgs> MessageStepComplete;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageSetIPCompleteEventArgs> MessageSetIPComplete;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageUserMessageEventArgs> MessageUserMessage;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageBreakEventArgs> MessageBreak;

		/// <summary>
		/// Raised on the debugger thread when there's a new message.
		/// The listeners can pause the debugged program by setting <see cref="DbgMessageEventArgs.Pause"/> to true.
		/// </summary>
		public abstract event EventHandler<DbgMessageAsyncProgramMessageEventArgs> MessageAsyncProgramMessage;

		/// <summary>
		/// Starts debugging. Returns an error string if it failed to create a debug engine, or null on success.
		/// See <see cref="IDbgManagerStartListener"/> on how to get called the first time this method gets called.
		/// </summary>
		/// <param name="options">Options needed to start the program or attach to it</param>
		public abstract string Start(DebugProgramOptions options);

		/// <summary>
		/// true if <see cref="Restart"/> can be called
		/// </summary>
		public abstract bool CanRestart { get; }

		/// <summary>
		/// Restarts the debugged program(s)
		/// </summary>
		public abstract void Restart();

		/// <summary>
		/// true if a program is being debugged
		/// </summary>
		public abstract bool IsDebugging { get; }

		/// <summary>
		/// Raised when <see cref="IsDebugging"/> is changed
		/// </summary>
		public abstract event EventHandler IsDebuggingChanged;

		/// <summary>
		/// true if all processes are running, false if they're all paused, and null
		/// if some are running and some are paused.
		/// This property is valid only if <see cref="IsDebugging"/> is true.
		/// </summary>
		public abstract bool? IsRunning { get; }

		/// <summary>
		/// Raised when <see cref="IsRunning"/> is changed, see also <see cref="DelayedIsRunningChanged"/>
		/// </summary>
		public abstract event EventHandler IsRunningChanged;

		/// <summary>
		/// Raised when all processes have been running for a little while, eg. 1 second.
		/// </summary>
		public abstract event EventHandler DelayedIsRunningChanged;

		/// <summary>
		/// Gets all debug tags, see <see cref="PredefinedDebugTags"/>
		/// </summary>
		public abstract string[] DebugTags { get; }

		/// <summary>
		/// Raised when <see cref="DebugTags"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<string>> DebugTagsChanged;

		/// <summary>
		/// Raised when a process gets paused due to some event in the process. If more than one process
		/// is being debugged, this is normally only raised once, for the first process.
		/// </summary>
		public abstract event EventHandler<ProcessPausedEventArgs> ProcessPaused;

		/// <summary>
		/// Gets all debugged processes. Can be empty even if <see cref="IsDebugging"/> is true
		/// if the process hasn't been created yet.
		/// </summary>
		public abstract DbgProcess[] Processes { get; }

		/// <summary>
		/// Raised when <see cref="Processes"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgProcess>> ProcessesChanged;

		/// <summary>
		/// Pauses all debugged processes
		/// </summary>
		public abstract void BreakAll();

		/// <summary>
		/// Lets all programs run again. This is the inverse of <see cref="BreakAll"/>
		/// </summary>
		public abstract void RunAll();

		/// <summary>
		/// Lets <paramref name="process"/> run again. If <see cref="DebuggerSettings.BreakAllProcesses"/>
		/// is true, all other processes will also run.
		/// </summary>
		/// <param name="process">Process to run</param>
		public abstract void Run(DbgProcess process);

		/// <summary>
		/// Stops debugging. All programs started by the debugger will be terminated. All
		/// other programs will be detached, if possible, else terminated.
		/// </summary>
		public abstract void StopDebuggingAll();

		/// <summary>
		/// Terminates all debugged programs
		/// </summary>
		public abstract void TerminateAll();

		/// <summary>
		/// Detaches all debugged programs, if possible. If it's not possible to detach a
		/// program, it will be terminated.
		/// </summary>
		public abstract void DetachAll();

		/// <summary>
		/// true if <see cref="DetachAll"/> can be called without terminating any programs
		/// </summary>
		public abstract bool CanDetachWithoutTerminating { get; }

		/// <summary>
		/// Gets the current process
		/// </summary>
		public abstract DbgCurrentObject<DbgProcess> CurrentProcess { get; }

		/// <summary>
		/// Raised when <see cref="CurrentProcess"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCurrentObjectChangedEventArgs<DbgProcess>> CurrentProcessChanged;

		/// <summary>
		/// Gets the current runtime
		/// </summary>
		public abstract DbgCurrentObject<DbgRuntime> CurrentRuntime { get; }

		/// <summary>
		/// Raised when <see cref="CurrentRuntime"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCurrentObjectChangedEventArgs<DbgRuntime>> CurrentRuntimeChanged;

		/// <summary>
		/// Gets the current thread
		/// </summary>
		public abstract DbgCurrentObject<DbgThread> CurrentThread { get; }

		/// <summary>
		/// Raised when <see cref="CurrentThread"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCurrentObjectChangedEventArgs<DbgThread>> CurrentThreadChanged;

		/// <summary>
		/// Raised when the module's memory has been updated (eg. decrypted)
		/// </summary>
		public abstract event EventHandler<ModulesRefreshedEventArgs> ModulesRefreshed;

		/// <summary>
		/// Returns true if the runtime can be debugged
		/// </summary>
		/// <param name="pid">Process id</param>
		/// <param name="rid">Runtime id</param>
		/// <returns></returns>
		public abstract bool CanDebugRuntime(int pid, RuntimeId rid);

		/// <summary>
		/// Closes <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Object to close</param>
		public abstract void Close(DbgObject obj);

		/// <summary>
		/// Closes <paramref name="objs"/>
		/// </summary>
		/// <param name="objs">Objects to close</param>
		public abstract void Close(IEnumerable<DbgObject> objs);

		/// <summary>
		/// Writes a message that will be shown in the output window
		/// </summary>
		/// <param name="message">Message</param>
		public void WriteMessage(string message) => WriteMessage(PredefinedDbgManagerMessageKinds.Output, message);

		/// <summary>
		/// Shows an error message and returns immediately
		/// </summary>
		/// <param name="errorMessage">Error message</param>
		public void ShowError(string errorMessage) => WriteMessage(PredefinedDbgManagerMessageKinds.ErrorUser, errorMessage);

		/// <summary>
		/// Writes a message
		/// </summary>
		/// <param name="messageKind">Message kind, see <see cref="PredefinedDbgManagerMessageKinds"/></param>
		/// <param name="message">Message</param>
		public abstract void WriteMessage(string messageKind, string message);

		/// <summary>
		/// Raised when <see cref="WriteMessage(string)"/> gets called. This event is raised on a random thread.
		/// </summary>
		public abstract event EventHandler<DbgManagerMessageEventArgs> DbgManagerMessage;
	}

	/// <summary>
	/// Predefined message kinds, see <see cref="DbgManager.WriteMessage(string, string)"/>
	/// </summary>
	public static class PredefinedDbgManagerMessageKinds {
		/// <summary>
		/// Output window
		/// </summary>
		public const string Output = nameof(Output);

		/// <summary>
		/// An error message that should be shown to the user
		/// </summary>
		public const string ErrorUser = nameof(ErrorUser);

		/// <summary>
		/// Messages by the stepper
		/// </summary>
		public const string StepFilter = nameof(StepFilter);
	}

	/// <summary>
	/// Message event args
	/// </summary>
	public readonly struct DbgManagerMessageEventArgs {
		/// <summary>
		/// Gets the message kind, see <see cref="PredefinedDbgManagerMessageKinds"/>
		/// </summary>
		public string MessageKind { get; }

		/// <summary>
		/// Gets the message
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="messageKind">Message kind, see <see cref="PredefinedDbgManagerMessageKinds"/></param>
		/// <param name="message">Message</param>
		public DbgManagerMessageEventArgs(string messageKind, string message) {
			MessageKind = messageKind ?? throw new ArgumentNullException(nameof(messageKind));
			Message = message ?? throw new ArgumentNullException(nameof(message));
		}
	}

	/// <summary>
	/// Contains the current object and the object that caused the debugger to enter break mode
	/// </summary>
	/// <typeparam name="T">Type of object</typeparam>
	public abstract class DbgCurrentObject<T> where T : DbgObject {
		/// <summary>
		/// Gets the current object or null if none
		/// </summary>
		public abstract T Current { get; set; }

		/// <summary>
		/// Gets the object that caused the debugger to enter break mode
		/// </summary>
		public abstract T Break { get; }
	}

	/// <summary>
	/// <see cref="DbgCurrentObject{T}"/> changed event args
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public readonly struct DbgCurrentObjectChangedEventArgs<T> where T : DbgObject {
		/// <summary>
		/// true if <see cref="DbgCurrentObject{T}.Current"/> changed
		/// </summary>
		public bool CurrentChanged { get; }

		/// <summary>
		/// true if <see cref="DbgCurrentObject{T}.Break"/> changed
		/// </summary>
		public bool BreakChanged { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="currentChanged">true if <see cref="DbgCurrentObject{T}.Current"/> changed</param>
		/// <param name="breakChanged">true if <see cref="DbgCurrentObject{T}.Break"/> changed</param>
		public DbgCurrentObjectChangedEventArgs(bool currentChanged, bool breakChanged) {
			CurrentChanged = currentChanged;
			BreakChanged = breakChanged;
		}
	}

	/// <summary>
	/// Process paused event args
	/// </summary>
	public readonly struct ProcessPausedEventArgs {
		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process { get; }

		/// <summary>
		/// Gets the thread or null if unknown
		/// </summary>
		public DbgThread Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="process">Process</param>
		/// <param name="thread">Thread or null if unknown</param>
		public ProcessPausedEventArgs(DbgProcess process, DbgThread thread) {
			Process = process ?? throw new ArgumentNullException(nameof(process));
			Thread = thread;
		}
	}
}
