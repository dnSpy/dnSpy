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

namespace dnSpy.Contracts.Debugger.Exceptions {
	/// <summary>
	/// Thrown exception in the debugged process
	/// </summary>
	public abstract class DbgException : DbgObject {
		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Runtime.Process;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgRuntime Runtime { get; }

		/// <summary>
		/// Gets the exception id
		/// </summary>
		public abstract DbgExceptionId Id { get; }

		/// <summary>
		/// Gets the exception event flags
		/// </summary>
		public abstract DbgExceptionEventFlags Flags { get; }

		/// <summary>
		/// true if it's a first chance exception
		/// </summary>
		public bool IsFirstChance => (Flags & DbgExceptionEventFlags.FirstChance) != 0;

		/// <summary>
		/// true if it's a second chance exception
		/// </summary>
		public bool IsSecondChance => (Flags & DbgExceptionEventFlags.SecondChance) != 0;

		/// <summary>
		/// true if it's an unhandled exception. The program will be terminated if it tries to run again.
		/// </summary>
		public bool IsUnhandled => (Flags & DbgExceptionEventFlags.Unhandled) != 0;

		/// <summary>
		/// Exception message or null if none
		/// </summary>
		public abstract string Message { get; }

		/// <summary>
		/// Thread where exception was thrown or null if it's not known
		/// </summary>
		public abstract DbgThread Thread { get; }

		/// <summary>
		/// Module where exception was thrown or null if it's not known
		/// </summary>
		public abstract DbgModule Module { get; }
	}
}
