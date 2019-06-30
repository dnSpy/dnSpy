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

namespace dnSpy.Contracts.Debugger.CallStack {
	/// <summary>
	/// Walks the stack of a thread in a debugged process and creates <see cref="DbgStackFrame"/>s
	/// </summary>
	public abstract class DbgStackWalker : DbgObject {
		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Thread.Process;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime Runtime => Thread.Runtime;

		/// <summary>
		/// Gets the thread
		/// </summary>
		public abstract DbgThread Thread { get; }

		/// <summary>
		/// Gets the next frames or an empty list if there are no more frames
		/// </summary>
		/// <param name="maxFrames">Max number of frames to return</param>
		/// <returns></returns>
		public abstract DbgStackFrame[] GetNextStackFrames(int maxFrames);

		/// <summary>
		/// Closes this instance. Any created <see cref="DbgStackFrame"/>s are not closed.
		/// </summary>
		public void Close() => Thread.Process.DbgManager.Close(this);
	}
}
