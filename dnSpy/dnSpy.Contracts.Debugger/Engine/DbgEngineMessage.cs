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

namespace dnSpy.Contracts.Debugger.Engine {
	/// <summary>
	/// Base class of messages created by a <see cref="DbgEngine"/>
	/// </summary>
	public abstract class DbgEngineMessage {
		/// <summary>
		/// Gets the message kind
		/// </summary>
		public abstract DbgEngineMessageKind MessageKind { get; }

		/// <summary>
		/// Gets the message flags
		/// </summary>
		public DbgEngineMessageFlags MessageFlags { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="messageFlags">Message flags</param>
		protected DbgEngineMessage(DbgEngineMessageFlags messageFlags) => MessageFlags = messageFlags;
	}

	/// <summary>
	/// <see cref="DbgEngineMessageKind.Connected"/> event. Should be the first event sent by the
	/// debug engine. If it couldn't connect, no more messages need to be sent after this message
	/// is sent.
	/// </summary>
	public sealed class DbgMessageConnected : DbgEngineMessage {
		/// <summary>
		/// Returns <see cref="DbgEngineMessageKind.Connected"/>
		/// </summary>
		public override DbgEngineMessageKind MessageKind => DbgEngineMessageKind.Connected;

		/// <summary>
		/// The error message or null if there's no error
		/// </summary>
		public string? ErrorMessage { get; }

		/// <summary>
		/// Gets the process id
		/// </summary>
		public int ProcessId { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="processId">Process id</param>
		/// <param name="messageFlags">Message flags</param>
		public DbgMessageConnected(int processId, DbgEngineMessageFlags messageFlags) : base(messageFlags) => ProcessId = processId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="errorMessage">Error message</param>
		/// <param name="messageFlags">Message flags</param>
		public DbgMessageConnected(string errorMessage, DbgEngineMessageFlags messageFlags)
			: base(messageFlags) => ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
	}

	/// <summary>
	/// <see cref="DbgEngineMessageKind.Disconnected"/> event
	/// </summary>
	public sealed class DbgMessageDisconnected : DbgEngineMessage {
		/// <summary>
		/// Returns <see cref="DbgEngineMessageKind.Disconnected"/>
		/// </summary>
		public override DbgEngineMessageKind MessageKind => DbgEngineMessageKind.Disconnected;

		/// <summary>
		/// Gets the exit code
		/// </summary>
		public int ExitCode { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="exitCode">Exit code</param>
		/// <param name="messageFlags">Message flags</param>
		public DbgMessageDisconnected(int exitCode, DbgEngineMessageFlags messageFlags) : base(messageFlags) => ExitCode = exitCode;
	}

	/// <summary>
	/// <see cref="DbgEngineMessageKind.Break"/> event
	/// </summary>
	public sealed class DbgMessageBreak : DbgEngineMessage {
		/// <summary>
		/// Returns <see cref="DbgEngineMessageKind.Break"/>
		/// </summary>
		public override DbgEngineMessageKind MessageKind => DbgEngineMessageKind.Break;

		/// <summary>
		/// The error message or null if there's no error
		/// </summary>
		public string? ErrorMessage { get; }

		/// <summary>
		/// Gets the thread or null if it's not known
		/// </summary>
		public DbgThread? Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="thread">Thread or null if it's not known</param>
		/// <param name="messageFlags">Message flags</param>
		public DbgMessageBreak(DbgThread? thread, DbgEngineMessageFlags messageFlags) : base(messageFlags) => Thread = thread;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="errorMessage">Error message</param>
		/// <param name="messageFlags">Message flags</param>
		public DbgMessageBreak(string errorMessage, DbgEngineMessageFlags messageFlags) : base(messageFlags) => ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
	}

	/// <summary>
	/// <see cref="DbgEngineMessageKind.EntryPointBreak"/> event
	/// </summary>
	public sealed class DbgMessageEntryPointBreak : DbgEngineMessage {
		/// <summary>
		/// Returns <see cref="DbgEngineMessageKind.EntryPointBreak"/>
		/// </summary>
		public override DbgEngineMessageKind MessageKind => DbgEngineMessageKind.EntryPointBreak;

		/// <summary>
		/// Gets the thread or null if it's not known
		/// </summary>
		public DbgThread? Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="thread">Thread or null if it's not known</param>
		/// <param name="messageFlags">Message flags</param>
		public DbgMessageEntryPointBreak(DbgThread? thread, DbgEngineMessageFlags messageFlags) : base(messageFlags) => Thread = thread;
	}

	/// <summary>
	/// <see cref="DbgEngineMessageKind.ProgramMessage"/> event
	/// </summary>
	public sealed class DbgMessageProgramMessage : DbgEngineMessage {
		/// <summary>
		/// Returns <see cref="DbgEngineMessageKind.ProgramMessage"/>
		/// </summary>
		public override DbgEngineMessageKind MessageKind => DbgEngineMessageKind.ProgramMessage;

		/// <summary>
		/// Gets the message
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Gets the thread or null if it's not known
		/// </summary>
		public DbgThread? Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Message</param>
		/// <param name="thread">Thread or null if it's not known</param>
		/// <param name="messageFlags">Message flags</param>
		public DbgMessageProgramMessage(string message, DbgThread? thread, DbgEngineMessageFlags messageFlags)
			: base(messageFlags) {
			Message = message ?? throw new ArgumentNullException(nameof(message));
			Thread = thread;
		}
	}

	/// <summary>
	/// <see cref="DbgEngineMessageKind.Breakpoint"/> event
	/// </summary>
	public sealed class DbgMessageBreakpoint : DbgEngineMessage {
		/// <summary>
		/// Returns <see cref="DbgEngineMessageKind.Breakpoint"/>
		/// </summary>
		public override DbgEngineMessageKind MessageKind => DbgEngineMessageKind.Breakpoint;

		/// <summary>
		/// Gets the breakpoint
		/// </summary>
		public DbgBoundCodeBreakpoint BoundBreakpoint { get; }

		/// <summary>
		/// Gets the thread or null if it's not known
		/// </summary>
		public DbgThread? Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="boundBreakpoint">Breakpoint</param>
		/// <param name="thread">Thread or null if it's not known</param>
		/// <param name="messageFlags">Message flags</param>
		public DbgMessageBreakpoint(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread? thread, DbgEngineMessageFlags messageFlags)
			: base(messageFlags) {
			BoundBreakpoint = boundBreakpoint ?? throw new ArgumentNullException(nameof(boundBreakpoint));
			Thread = thread;
		}
	}

	/// <summary>
	/// <see cref="DbgEngineMessageKind.ProgramBreak"/> event
	/// </summary>
	public sealed class DbgMessageProgramBreak : DbgEngineMessage {
		/// <summary>
		/// Returns <see cref="DbgEngineMessageKind.ProgramBreak"/>
		/// </summary>
		public override DbgEngineMessageKind MessageKind => DbgEngineMessageKind.ProgramBreak;

		/// <summary>
		/// Gets the thread or null if it's not known
		/// </summary>
		public DbgThread? Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="thread">Thread or null if it's not known</param>
		/// <param name="messageFlags">Message flags</param>
		public DbgMessageProgramBreak(DbgThread? thread, DbgEngineMessageFlags messageFlags)
			: base(messageFlags) => Thread = thread;
	}

	/// <summary>
	/// <see cref="DbgEngineMessageKind.SetIPComplete"/> event
	/// </summary>
	public sealed class DbgMessageSetIPComplete : DbgEngineMessage {
		/// <summary>
		/// Returns <see cref="DbgEngineMessageKind.SetIPComplete"/>
		/// </summary>
		public override DbgEngineMessageKind MessageKind => DbgEngineMessageKind.SetIPComplete;

		/// <summary>
		/// Gets the thread
		/// </summary>
		public DbgThread Thread { get; }

		/// <summary>
		/// true if all frames in the thread have been invalidated
		/// </summary>
		public bool FramesInvalidated { get; }

		/// <summary>
		/// Gets the error string or null if none
		/// </summary>
		public string? Error { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="thread">Thread</param>
		/// <param name="framesInvalidated">true if all frames in the thread have been invalidated</param>
		/// <param name="error">Error string or null if none</param>
		/// <param name="messageFlags">Message flags</param>
		public DbgMessageSetIPComplete(DbgThread thread, bool framesInvalidated, string? error, DbgEngineMessageFlags messageFlags)
			: base(messageFlags) {
			Thread = thread ?? throw new ArgumentNullException(nameof(thread));
			FramesInvalidated = framesInvalidated;
			Error = error;
		}
	}

	/// <summary>
	/// <see cref="DbgEngineMessageKind.AsyncProgramMessage"/> event
	/// </summary>
	public sealed class DbgMessageAsyncProgramMessage : DbgEngineMessage {
		/// <summary>
		/// Returns <see cref="DbgEngineMessageKind.AsyncProgramMessage"/>
		/// </summary>
		public override DbgEngineMessageKind MessageKind => DbgEngineMessageKind.AsyncProgramMessage;

		/// <summary>
		/// Gets the message source
		/// </summary>
		public AsyncProgramMessageSource Source { get; }

		/// <summary>
		/// Gets the message
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="source">Source</param>
		/// <param name="message">Message</param>
		public DbgMessageAsyncProgramMessage(AsyncProgramMessageSource source, string message)
			: base(DbgEngineMessageFlags.None) {
			Source = source;
			Message = message ?? throw new ArgumentNullException(nameof(message));
		}
	}
}
