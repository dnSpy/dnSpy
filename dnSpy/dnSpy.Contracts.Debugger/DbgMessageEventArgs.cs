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
using dnSpy.Contracts.Debugger.Exceptions;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Debugger message kind
	/// </summary>
	public enum DbgMessageKind {
		/// <summary>
		/// A process was created (<see cref="DbgMessageProcessCreatedEventArgs"/>)
		/// </summary>
		ProcessCreated,

		/// <summary>
		/// A process has exited (<see cref="DbgMessageProcessExitedEventArgs"/>)
		/// </summary>
		ProcessExited,

		/// <summary>
		/// A runtime was created (<see cref="DbgMessageRuntimeCreatedEventArgs"/>)
		/// </summary>
		RuntimeCreated,

		/// <summary>
		/// A runtime has exited (<see cref="DbgMessageRuntimeExitedEventArgs"/>)
		/// </summary>
		RuntimeExited,

		/// <summary>
		/// An app domain was loaded (<see cref="DbgMessageAppDomainLoadedEventArgs"/>)
		/// </summary>
		AppDomainLoaded,

		/// <summary>
		/// An app domain was unloaded (<see cref="DbgMessageAppDomainUnloadedEventArgs"/>). This message isn't sent if the program has exited.
		/// </summary>
		AppDomainUnloaded,

		/// <summary>
		/// A module was loaded (<see cref="DbgMessageModuleLoadedEventArgs"/>)
		/// </summary>
		ModuleLoaded,

		/// <summary>
		/// A module was unloaded (<see cref="DbgMessageModuleUnloadedEventArgs"/>). This message isn't sent if the program has exited or if its app domain has unloaded.
		/// </summary>
		ModuleUnloaded,

		/// <summary>
		/// A thread was created (<see cref="DbgMessageThreadCreatedEventArgs"/>)
		/// </summary>
		ThreadCreated,

		/// <summary>
		/// A thread has exited (<see cref="DbgMessageThreadExitedEventArgs"/>). This message isn't sent if the program has exited.
		/// </summary>
		ThreadExited,

		/// <summary>
		/// An exception was thrown (<see cref="DbgMessageExceptionThrownEventArgs"/>)
		/// </summary>
		ExceptionThrown,

		/// <summary>
		/// The entry point has been reached (<see cref="DbgMessageEntryPointBreakEventArgs"/>).
		/// This message is only sent if the user chose to break at the entry point.
		/// </summary>
		EntryPointBreak,

		/// <summary>
		/// Message from the debugged program (<see cref="DbgMessageProgramMessageEventArgs"/>)
		/// </summary>
		ProgramMessage,

		/// <summary>
		/// A bound breakpoint was hit (<see cref="DbgMessageBoundBreakpointEventArgs"/>)
		/// </summary>
		BoundBreakpoint,

		/// <summary>
		/// The program paused itself by executing a break instruction or method (<see cref="DbgMessageProgramBreakEventArgs"/>)
		/// </summary>
		ProgramBreak,

		/// <summary>
		/// Step into/over/out is complete (<see cref="DbgMessageStepCompleteEventArgs"/>)
		/// </summary>
		StepComplete,

		/// <summary>
		/// SetIP() is complete (<see cref="DbgMessageSetIPCompleteEventArgs"/>)
		/// </summary>
		SetIPComplete,

		/// <summary>
		/// Some message that should be shown to the user, eg. we failed to connect to the debugged process (<see cref="DbgMessageUserMessageEventArgs"/>)
		/// </summary>
		UserMessage,

		/// <summary>
		/// The program was paused by the user, or because some other program was paused for some other reason (<see cref="DbgMessageBreakEventArgs"/>)
		/// </summary>
		Break,

		/// <summary>
		/// Message from the debugged program (<see cref="DbgMessageAsyncProgramMessageEventArgs"/>)
		/// </summary>
		AsyncProgramMessage,
	}

	/// <summary>
	/// Base class of all debugger messages
	/// </summary>
	public abstract class DbgMessageEventArgs : EventArgs {
		/// <summary>
		/// Gets the message kind
		/// </summary>
		public abstract DbgMessageKind Kind { get; }

		/// <summary>
		/// true if the program should be paused. It's only possible to write true to this property.
		/// </summary>
		public bool Pause {
			get => pause;
			set => pause |= value;
		}
		bool pause;
	}

	/// <summary>
	/// Process message base class
	/// </summary>
	public abstract class DbgMessageProcessEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="process">Process</param>
		protected DbgMessageProcessEventArgs(DbgProcess process) =>
			Process = process ?? throw new ArgumentNullException(nameof(process));
	}

	/// <summary>
	/// Process created message (<see cref="DbgMessageKind.ProcessCreated"/>)
	/// </summary>
	public sealed class DbgMessageProcessCreatedEventArgs : DbgMessageProcessEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.ProcessCreated"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.ProcessCreated;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="process">Process</param>
		public DbgMessageProcessCreatedEventArgs(DbgProcess process)
			: base(process) {
		}
	}

	/// <summary>
	/// Process exited message (<see cref="DbgMessageKind.ProcessExited"/>)
	/// </summary>
	public sealed class DbgMessageProcessExitedEventArgs : DbgMessageProcessEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.ProcessExited"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.ProcessExited;

		/// <summary>
		/// Gets the exit code
		/// </summary>
		public int ExitCode { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="process">Process</param>
		/// <param name="exitCode">Process exit code</param>
		public DbgMessageProcessExitedEventArgs(DbgProcess process, int exitCode)
			: base(process) {
		}
	}

	/// <summary>
	/// Runtime message base class
	/// </summary>
	public abstract class DbgMessageRuntimeEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime Runtime { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="runtime">Runtime</param>
		protected DbgMessageRuntimeEventArgs(DbgRuntime runtime) =>
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
	}

	/// <summary>
	/// Runtime created message (<see cref="DbgMessageKind.RuntimeCreated"/>)
	/// </summary>
	public sealed class DbgMessageRuntimeCreatedEventArgs : DbgMessageRuntimeEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.RuntimeCreated"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.RuntimeCreated;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="runtime">Runtime</param>
		public DbgMessageRuntimeCreatedEventArgs(DbgRuntime runtime)
			: base(runtime) {
		}
	}

	/// <summary>
	/// Runtime exited message (<see cref="DbgMessageKind.RuntimeExited"/>)
	/// </summary>
	public sealed class DbgMessageRuntimeExitedEventArgs : DbgMessageRuntimeEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.RuntimeExited"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.RuntimeExited;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="runtime">Runtime</param>
		public DbgMessageRuntimeExitedEventArgs(DbgRuntime runtime)
			: base(runtime) {
		}
	}

	/// <summary>
	/// App domain message base class
	/// </summary>
	public abstract class DbgMessageAppDomainEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Gets the app domain
		/// </summary>
		public DbgAppDomain AppDomain { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="appDomain">App domain</param>
		protected DbgMessageAppDomainEventArgs(DbgAppDomain appDomain) =>
			AppDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
	}

	/// <summary>
	/// App domain loaded message (<see cref="DbgMessageKind.AppDomainLoaded"/>)
	/// </summary>
	public sealed class DbgMessageAppDomainLoadedEventArgs : DbgMessageAppDomainEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.AppDomainLoaded"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.AppDomainLoaded;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="appDomain">App domain</param>
		public DbgMessageAppDomainLoadedEventArgs(DbgAppDomain appDomain)
			: base(appDomain) {
		}
	}

	/// <summary>
	/// App domain unloaded message (<see cref="DbgMessageKind.AppDomainUnloaded"/>)
	/// </summary>
	public sealed class DbgMessageAppDomainUnloadedEventArgs : DbgMessageAppDomainEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.AppDomainUnloaded"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.AppDomainUnloaded;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="appDomain">App domain</param>
		public DbgMessageAppDomainUnloadedEventArgs(DbgAppDomain appDomain)
			: base(appDomain) {
		}
	}

	/// <summary>
	/// Module message base class
	/// </summary>
	public abstract class DbgMessageModuleEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Gets the module
		/// </summary>
		public DbgModule Module { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		protected DbgMessageModuleEventArgs(DbgModule module) =>
			Module = module ?? throw new ArgumentNullException(nameof(module));
	}

	/// <summary>
	/// Module loaded message (<see cref="DbgMessageKind.ModuleLoaded"/>)
	/// </summary>
	public sealed class DbgMessageModuleLoadedEventArgs : DbgMessageModuleEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.ModuleLoaded"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.ModuleLoaded;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		public DbgMessageModuleLoadedEventArgs(DbgModule module)
			: base(module) {
		}
	}

	/// <summary>
	/// Module unloaded message (<see cref="DbgMessageKind.ModuleUnloaded"/>)
	/// </summary>
	public sealed class DbgMessageModuleUnloadedEventArgs : DbgMessageModuleEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.ModuleUnloaded"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.ModuleUnloaded;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		public DbgMessageModuleUnloadedEventArgs(DbgModule module)
			: base(module) {
		}
	}

	/// <summary>
	/// Thread message base class
	/// </summary>
	public abstract class DbgMessageThreadEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Gets the thread
		/// </summary>
		public DbgThread Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="thread">Thread</param>
		protected DbgMessageThreadEventArgs(DbgThread thread) =>
			Thread = thread ?? throw new ArgumentNullException(nameof(thread));
	}

	/// <summary>
	/// Thread created message (<see cref="DbgMessageKind.ThreadCreated"/>)
	/// </summary>
	public sealed class DbgMessageThreadCreatedEventArgs : DbgMessageThreadEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.ThreadCreated"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.ThreadCreated;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="thread">Thread</param>
		public DbgMessageThreadCreatedEventArgs(DbgThread thread)
			: base(thread) {
		}
	}

	/// <summary>
	/// Thread exited message (<see cref="DbgMessageKind.ThreadExited"/>)
	/// </summary>
	public sealed class DbgMessageThreadExitedEventArgs : DbgMessageThreadEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.ThreadExited"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.ThreadExited;

		/// <summary>
		/// Gets the exit code
		/// </summary>
		public int ExitCode { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <param name="exitCode">Thread exit code</param>
		public DbgMessageThreadExitedEventArgs(DbgThread thread, int exitCode)
			: base(thread) {
		}
	}

	/// <summary>
	/// Exception thrown message (<see cref="DbgMessageKind.ExceptionThrown"/>)
	/// </summary>
	public sealed class DbgMessageExceptionThrownEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.ExceptionThrown"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.ExceptionThrown;

		/// <summary>
		/// Gets the exception
		/// </summary>
		public DbgException Exception { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="exception">Exception</param>
		public DbgMessageExceptionThrownEventArgs(DbgException exception) =>
			Exception = exception ?? throw new ArgumentNullException(nameof(exception));
	}

	/// <summary>
	/// The entry point has been reached (<see cref="DbgMessageKind.EntryPointBreak"/>).
	/// This message is only sent if the user chose to break at the entry point.
	/// </summary>
	public sealed class DbgMessageEntryPointBreakEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.EntryPointBreak"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.EntryPointBreak;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime Runtime { get; }

		/// <summary>
		/// Gets the thread or null if it's unknown
		/// </summary>
		public DbgThread? Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="thread">Thread or null if it's unknown</param>
		public DbgMessageEntryPointBreakEventArgs(DbgRuntime runtime, DbgThread? thread) {
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			Thread = thread;
		}
	}

	/// <summary>
	/// Message from the debugged program (<see cref="DbgMessageKind.ProgramMessage"/>)
	/// </summary>
	public sealed class DbgMessageProgramMessageEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.ProgramMessage"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.ProgramMessage;

		/// <summary>
		/// Gets the text
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime Runtime { get; }

		/// <summary>
		/// Gets the thread or null if it's unknown
		/// </summary>
		public DbgThread? Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Message</param>
		/// <param name="runtime">Runtime</param>
		/// <param name="thread">Thread or null if it's unknown</param>
		public DbgMessageProgramMessageEventArgs(string message, DbgRuntime runtime, DbgThread? thread) {
			Message = message ?? throw new ArgumentNullException(nameof(message));
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			Thread = thread;
		}
	}

	/// <summary>
	/// A bound breakpoint was hit (<see cref="DbgMessageKind.BoundBreakpoint"/>)
	/// </summary>
	public sealed class DbgMessageBoundBreakpointEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.BoundBreakpoint"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.BoundBreakpoint;

		/// <summary>
		/// Gets the bound breakpoint
		/// </summary>
		public DbgBoundCodeBreakpoint BoundBreakpoint { get; }

		/// <summary>
		/// Gets the thread or null if it's unknown
		/// </summary>
		public DbgThread? Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="boundBreakpoint">Bound breakpoint</param>
		/// <param name="thread">Thread or null if it's unknown</param>
		public DbgMessageBoundBreakpointEventArgs(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread? thread) {
			BoundBreakpoint = boundBreakpoint ?? throw new ArgumentNullException(nameof(boundBreakpoint));
			Thread = thread;
		}
	}

	/// <summary>
	/// The program paused itself by executing a break instruction or method (<see cref="DbgMessageKind.ProgramBreak"/>)
	/// </summary>
	public sealed class DbgMessageProgramBreakEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.ProgramBreak"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.ProgramBreak;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime Runtime { get; }

		/// <summary>
		/// Gets the thread or null if it's unknown
		/// </summary>
		public DbgThread? Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="thread">Thread or null if it's unknown</param>
		public DbgMessageProgramBreakEventArgs(DbgRuntime runtime, DbgThread? thread) {
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			Thread = thread;
		}
	}

	/// <summary>
	/// Step into/over/out is complete (<see cref="DbgMessageKind.StepComplete"/>)
	/// </summary>
	public sealed class DbgMessageStepCompleteEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.StepComplete"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.StepComplete;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime Runtime => Thread.Runtime;

		/// <summary>
		/// Gets the thread
		/// </summary>
		public DbgThread Thread { get; }

		/// <summary>
		/// Gets the error message or null if none
		/// </summary>
		public string? Error { get; }

		/// <summary>
		/// true if there was an error. Error message is in <see cref="Error"/>
		/// </summary>
		public bool HasError => !(Error is null);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <param name="error">Error message or null if none</param>
		public DbgMessageStepCompleteEventArgs(DbgThread thread, string? error) {
			Thread = thread ?? throw new ArgumentNullException(nameof(thread));
			Error = error;
		}
	}

	/// <summary>
	/// SetIP() is complete (<see cref="DbgMessageKind.SetIPComplete"/>)
	/// </summary>
	public sealed class DbgMessageSetIPCompleteEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.SetIPComplete"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.SetIPComplete;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime Runtime => Thread.Runtime;

		/// <summary>
		/// Gets the thread
		/// </summary>
		public DbgThread Thread { get; }

		/// <summary>
		/// true if all frames in the thread have been invalidated
		/// </summary>
		public bool FramesInvalidated { get; }

		/// <summary>
		/// Gets the error message or null if none
		/// </summary>
		public string? Error { get; }

		/// <summary>
		/// true if there was an error. Error message is in <see cref="Error"/>
		/// </summary>
		public bool HasError => !(Error is null);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <param name="framesInvalidated">true if all frames in the thread have been invalidated</param>
		/// <param name="error">Error message or null if none</param>
		public DbgMessageSetIPCompleteEventArgs(DbgThread thread, bool framesInvalidated, string? error) {
			Thread = thread ?? throw new ArgumentNullException(nameof(thread));
			FramesInvalidated = framesInvalidated;
			Error = error;
		}
	}

	/// <summary>
	/// A message that should be shown to the user
	/// </summary>
	public sealed class DbgMessageUserMessageEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.UserMessage"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.UserMessage;

		/// <summary>
		/// Gets the message kind
		/// </summary>
		public UserMessageKind MessageKind { get; }

		/// <summary>
		/// Gets the message
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="messageKind">Message kind</param>
		/// <param name="message">Message</param>
		public DbgMessageUserMessageEventArgs(UserMessageKind messageKind, string message) {
			MessageKind = messageKind;
			Message = message ?? throw new ArgumentNullException(nameof(message));
		}
	}

	/// <summary>
	/// <see cref="DbgMessageUserMessageEventArgs"/> message kinds
	/// </summary>
	public enum UserMessageKind {
		/// <summary>
		/// We failed to connect to the debugged process
		/// </summary>
		CouldNotConnect,

		/// <summary>
		/// Could not break the debugged process
		/// </summary>
		CouldNotBreak,
	}

	/// <summary>
	/// The program was paused by the user, or because some other program was paused for some other reason (<see cref="DbgMessageKind.Break"/>)
	/// </summary>
	public sealed class DbgMessageBreakEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.Break"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.Break;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime Runtime { get; }

		/// <summary>
		/// Gets the thread or null if it's unknown
		/// </summary>
		public DbgThread? Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="thread">Thread or null if it's unknown</param>
		public DbgMessageBreakEventArgs(DbgRuntime runtime, DbgThread? thread) {
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			Thread = thread;
		}
	}

	/// <summary>
	/// Message source kind
	/// </summary>
	public enum AsyncProgramMessageSource {
		/// <summary>
		/// Some other source
		/// </summary>
		Other,

		/// <summary>
		/// Standard output text
		/// </summary>
		StandardOutput,

		/// <summary>
		/// Standard error text
		/// </summary>
		StandardError,
	}

	/// <summary>
	/// Message from the debugged program (<see cref="DbgMessageKind.AsyncProgramMessage"/>)
	/// </summary>
	public sealed class DbgMessageAsyncProgramMessageEventArgs : DbgMessageEventArgs {
		/// <summary>
		/// Returns <see cref="DbgMessageKind.AsyncProgramMessage"/>
		/// </summary>
		public override DbgMessageKind Kind => DbgMessageKind.AsyncProgramMessage;

		/// <summary>
		/// Gets the message source kind
		/// </summary>
		public AsyncProgramMessageSource Source { get; }

		/// <summary>
		/// Gets the text
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime Runtime { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="source">Source</param>
		/// <param name="message">Message</param>
		/// <param name="runtime">Runtime</param>
		public DbgMessageAsyncProgramMessageEventArgs(AsyncProgramMessageSource source, string message, DbgRuntime runtime) {
			Source = source;
			Message = message ?? throw new ArgumentNullException(nameof(message));
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
		}
	}
}
