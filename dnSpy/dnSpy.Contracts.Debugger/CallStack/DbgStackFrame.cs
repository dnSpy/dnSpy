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
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Documents;

namespace dnSpy.Contracts.Debugger.CallStack {
	/// <summary>
	/// A stack frame in a debugged process
	/// </summary>
	public abstract class DbgStackFrame : DbgObject {
		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Thread.Process;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime Runtime => Thread.Runtime;

		/// <summary>
		/// Gets the AppDomain or null if it's unknown
		/// </summary>
		public DbgAppDomain AppDomain => Module?.AppDomain ?? Thread.AppDomain;

		/// <summary>
		/// Gets the thread
		/// </summary>
		public abstract DbgThread Thread { get; }

		/// <summary>
		/// Gets the location or null if none. Can be passed to <see cref="ReferenceNavigatorService.GoTo(object, object[])"/>
		/// or can be used to create a breakpoint if you call <see cref="DbgCodeLocation.Clone"/>
		/// </summary>
		public abstract DbgCodeLocation Location { get; }

		/// <summary>
		/// Gets the module or null if it's unknown
		/// </summary>
		public abstract DbgModule Module { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public abstract DbgStackFrameFlags Flags { get; }

		/// <summary>
		/// Gets the offset of the IP relative to the start of the function
		/// </summary>
		public abstract uint FunctionOffset { get; }

		/// <summary>
		/// Gets the function token or <see cref="uint.MaxValue"/> if it doesn't have a token.
		/// </summary>
		public abstract uint FunctionToken { get; }

		/// <summary>
		/// true if <see cref="FunctionToken"/> is valid
		/// </summary>
		public bool HasFunctionToken => FunctionToken != uint.MaxValue;

		/// <summary>
		/// Closes this instance
		/// </summary>
		public void Close() => Thread.Process.DbgManager.Close(this);
	}

	/// <summary>
	/// Stack frame flags
	/// </summary>
	[Flags]
	public enum DbgStackFrameFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Set if <see cref="DbgStackFrame.Location"/> is the next statement to execute in this frame. It's also
		/// possible to set a BP at that location.
		/// 
		/// It's false if <see cref="DbgStackFrame.Location"/> is just an approximate location and it's not safe
		/// to set a BP at the location.
		/// </summary>
		LocationIsNextStatement	= 0x00000001,
	}
}
