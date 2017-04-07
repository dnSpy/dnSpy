/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
	}

	/// <summary>
	/// Base class of messages created by a <see cref="DbgEngine"/> that can contain an error message
	/// </summary>
	public abstract class DbgEngineMessageWithPossibleErrorMessage : DbgEngineMessage {
		/// <summary>
		/// The error message or null if there's no error
		/// </summary>
		public string ErrorMessage { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		protected DbgEngineMessageWithPossibleErrorMessage() { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="errorMessage">Error message</param>
		protected DbgEngineMessageWithPossibleErrorMessage(string errorMessage) => ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
	}

	/// <summary>
	/// <see cref="DbgEngineMessageKind.Connected"/> event. Should be the first event sent by the
	/// debug engine. If it couldn't connect, no more messages need to be sent after this message
	/// is sent.
	/// </summary>
	public sealed class DbgMessageConnected : DbgEngineMessageWithPossibleErrorMessage {
		/// <summary>
		/// Returns <see cref="DbgEngineMessageKind.Connected"/>
		/// </summary>
		public override DbgEngineMessageKind MessageKind => DbgEngineMessageKind.Connected;

		/// <summary>
		/// Gets the process id
		/// </summary>
		public int ProcessId { get; }

		/// <summary>
		/// true if the process should be paused, false if other code gets to decide if it should be paused
		/// </summary>
		public bool Pause { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="processId">Process id</param>
		/// <param name="pause">true if the process should be paused, false if other code gets to decide if it should be paused</param>
		public DbgMessageConnected(int processId, bool pause = false) {
			ProcessId = processId;
			Pause = pause;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="errorMessage">Error message</param>
		public DbgMessageConnected(string errorMessage) : base(errorMessage) { }
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
		public DbgMessageDisconnected(int exitCode) => ExitCode = exitCode;
	}

	/// <summary>
	/// <see cref="DbgEngineMessageKind.Break"/> event
	/// </summary>
	public sealed class DbgMessageBreak : DbgEngineMessageWithPossibleErrorMessage {
		/// <summary>
		/// Returns <see cref="DbgEngineMessageKind.Break"/>
		/// </summary>
		public override DbgEngineMessageKind MessageKind => DbgEngineMessageKind.Break;

		/// <summary>
		/// Gets the thread or null if it's not known
		/// </summary>
		public DbgThread Thread { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		public DbgMessageBreak(DbgThread thread) => Thread = thread;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="errorMessage">Error message</param>
		public DbgMessageBreak(string errorMessage) : base(errorMessage) { }
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
		public DbgThread Thread { get; }

		/// <summary>
		/// true if the process should be paused, false if other code gets to decide if it should be paused
		/// </summary>
		public bool Pause { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Message</param>
		/// <param name="thread">Thread or null if it's not known</param>
		/// <param name="pause">true if the process should be paused, false if other code gets to decide if it should be paused</param>
		public DbgMessageProgramMessage(string message, DbgThread thread, bool pause = false) {
			Message = message ?? throw new ArgumentNullException(nameof(message));
			Thread = thread;
			Pause = pause;
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
		public DbgThread Thread { get; }

		/// <summary>
		/// true if the process should be paused, false if other code gets to decide if it should be paused
		/// </summary>
		public bool Pause { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="boundBreakpoint">Breakpoint</param>
		/// <param name="thread">Thread or null if it's not known</param>
		/// <param name="pause">true if the process should be paused, false if other code gets to decide if it should be paused</param>
		public DbgMessageBreakpoint(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread, bool pause = false) {
			BoundBreakpoint = boundBreakpoint ?? throw new ArgumentNullException(nameof(boundBreakpoint));
			Thread = thread;
			Pause = pause;
		}
	}
}
