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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// A debugged process
	/// </summary>
	public abstract class DbgProcess : DbgObject {
		/// <summary>
		/// Process id
		/// </summary>
		public abstract int Id { get; }

		/// <summary>
		/// Gets all runtimes
		/// </summary>
		public abstract DbgRuntime[] Runtimes { get; }

		/// <summary>
		/// Gets the process bit size (32 or 64)
		/// </summary>
		public abstract int BitSize { get; }

		/// <summary>
		/// Gets all threads
		/// </summary>
		public abstract DbgThread[] Threads { get; }

		/// <summary>
		/// Reads memory. Returns the number of bytes read.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="destination">Destination address</param>
		/// <param name="size">Number of bytes to read</param>
		/// <returns></returns>
		public abstract int ReadMemory(ulong address, IntPtr destination, int size);

		/// <summary>
		/// Reads memory. Returns the number of bytes read.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="destination">Destination buffer</param>
		/// <param name="destinationIndex">Destination index</param>
		/// <param name="size">Number of bytes to read</param>
		/// <returns></returns>
		public abstract int ReadMemory(ulong address, byte[] destination, int destinationIndex, int size);

		/// <summary>
		/// Writes memory. Returns the number of bytes written.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="source">Source address</param>
		/// <param name="size">Number of bytes to write</param>
		/// <returns></returns>
		public abstract int WriteMemory(ulong address, IntPtr source, int size);

		/// <summary>
		/// Writes memory. Returns the number of bytes written.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="source">Source buffer</param>
		/// <param name="sourceIndex">Source index</param>
		/// <param name="size">Number of bytes to write</param>
		/// <returns></returns>
		public abstract int WriteMemory(ulong address, byte[] source, int sourceIndex, int size);
	}
}
