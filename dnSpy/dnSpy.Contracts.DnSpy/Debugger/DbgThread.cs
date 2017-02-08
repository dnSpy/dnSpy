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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// A thread in a debugged process
	/// </summary>
	public abstract class DbgThread : DbgObject {
		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgRuntime Runtime { get; }

		/// <summary>
		/// Gets the thread kind
		/// </summary>
		public abstract DbgThreadKind Kind { get; }
	}

	/// <summary>
	/// Thread kind
	/// </summary>
	public enum DbgThreadKind {
		/// <summary>
		/// Unknown thread
		/// </summary>
		Unknown,

		/// <summary>
		/// Some other type of thread
		/// </summary>
		Other,

		/// <summary>
		/// Main thread
		/// </summary>
		Main,

		/// <summary>
		/// Worker thread
		/// </summary>
		WorkerThread,
	}
}
