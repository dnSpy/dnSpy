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
	/// A debug engine contains all the logic to control the debugged process
	/// </summary>
	public abstract class DbgEngine : DbgObject {
		/// <summary>
		/// How was the debugged process started?
		/// </summary>
		public abstract DbgStartKind StartKind { get; }

		/// <summary>
		/// Gets all debug tags, see <see cref="PredefinedDebugTags"/>
		/// </summary>
		public abstract string[] DebugTags { get; }

		/// <summary>
		/// Called when the engine can start or attach to the debugged process. This method is called shortly after
		/// this instance gets created by a call to <see cref="DbgEngineProvider.Create(DbgManager, StartDebuggingOptions)"/>.
		/// It must send a <see cref="DbgMessageConnected"/> message when it has connected to the program or
		/// if it failed.
		/// </summary>
		/// <param name="options">Same options that were passed to <see cref="DbgEngineProvider.Create(DbgManager, StartDebuggingOptions)"/></param>
		public abstract void Start(StartDebuggingOptions options);

		/// <summary>
		/// Raised when there's a new message. It can be raised in any thread.
		/// </summary>
		public abstract event EventHandler<DbgEngineMessage> Message;

		/// <summary>
		/// Creates the runtime. Called once after the engine has connected to the debugged process.
		/// </summary>
		/// <param name="process">Owner process</param>
		/// <returns></returns>
		public abstract DbgRuntime CreateRuntime(DbgProcess process);

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
		/// Stops debugging. This is an asynchronous method.
		/// 
		/// This method gets called when the user presses the Stop Debugging button.
		/// 
		/// If the engine attached to the program, it should be detached, if possible. Otherwise
		/// (eg. the program was started by the engine), the program should be terminated.
		/// 
		/// When the program has been terminated or detached, message <see cref="DbgMessageDisconnected"/>
		/// must be sent.
		/// </summary>
		public abstract void StopDebugging();

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
