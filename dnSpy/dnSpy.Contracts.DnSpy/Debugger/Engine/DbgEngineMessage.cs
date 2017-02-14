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
		/// Gets the process id
		/// </summary>
		public int ProcessId { get; }

		/// <summary>
		/// The error message or null if there's no error
		/// </summary>
		public string ErrorMessage { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="processId">Process id</param>
		public DbgMessageConnected(int processId) => ProcessId = processId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="errorMessage">Error message</param>
		public DbgMessageConnected(string errorMessage) => ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
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
}
