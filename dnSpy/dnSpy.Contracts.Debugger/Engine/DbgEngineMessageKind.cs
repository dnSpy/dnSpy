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

namespace dnSpy.Contracts.Debugger.Engine {
	/// <summary>
	/// <see cref="DbgEngineMessage"/> kind
	/// </summary>
	public enum DbgEngineMessageKind {
		/// <summary>
		/// The engine has connected to the debugged process. This message is the first
		/// message sent by the <see cref="DbgEngine"/>, even on failure.
		/// 
		/// If the connection was successful, the program must be paused until
		/// <see cref="DbgEngine.Run"/> gets called.
		/// </summary>
		Connected,

		/// <summary>
		/// The engine has been disconnected from the debugged process
		/// </summary>
		Disconnected,

		/// <summary>
		/// The debugged executable is paused due to a call to <see cref="DbgEngine.Break"/> or
		/// due to some other engine event.
		/// </summary>
		Break,

		/// <summary>
		/// Entry point has been reached. The engine has paused the program.
		/// This message is only sent if the user chose to break at the entry point.
		/// </summary>
		EntryPointBreak,

		/// <summary>
		/// Log message written by the debugged program. The engine has paused the program.
		/// </summary>
		ProgramMessage,

		/// <summary>
		/// A breakpoint has been hit. The engine has paused the program.
		/// </summary>
		Breakpoint,

		/// <summary>
		/// The program paused itself by executing a break instruction or method
		/// </summary>
		ProgramBreak,

		/// <summary>
		/// SetIP() is complete
		/// </summary>
		SetIPComplete,

		/// <summary>
		/// Message written by the debugged program. The program is still running.
		/// This message is sent when a GUI app writes to the console.
		/// </summary>
		AsyncProgramMessage,
	}
}
