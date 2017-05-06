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

using System.Collections.Generic;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Debugger.CallStack {
	/// <summary>
	/// A stack frame in a debugged process. The owner is responsible for closing this instance by calling <see cref="Close"/>
	/// or <see cref="DbgManager.Close(IEnumerable{DbgObject})"/> if more than one frame must be closed at the same time.
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
		/// Formats the stack frame
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="options">Options</param>
		public abstract void Format(ITextColorWriter writer, DbgStackFrameFormatOptions options);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract string ToString(DbgStackFrameFormatOptions options);

		/// <summary>
		/// Closes this instance. If multiple frames must be closed at the same time, use <see cref="DbgManager.Close(IEnumerable{DbgObject})"/> instead.
		/// </summary>
		public void Close() => Thread.Process.DbgManager.Close(this);
	}
}
