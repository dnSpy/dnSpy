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
		/// Gets the owner debug manager
		/// </summary>
		public abstract DbgManager DbgManager { get; }

		/// <summary>
		/// Process id
		/// </summary>
		public abstract int Id { get; }

		/// <summary>
		/// Gets all runtimes
		/// </summary>
		public abstract DbgRuntime[] Runtimes { get; }

		/// <summary>
		/// Raised when <see cref="Runtimes"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgRuntime>> RuntimesChanged;

		/// <summary>
		/// Gets the process bitness (32 or 64)
		/// </summary>
		public abstract int Bitness { get; }

		/// <summary>
		/// Machine
		/// </summary>
		public abstract DbgMachine Machine { get; }

		/// <summary>
		/// Gets the filename or an empty string if it's unknown
		/// </summary>
		public abstract string Filename { get; }

		/// <summary>
		/// Gets all threads
		/// </summary>
		public abstract DbgThread[] Threads { get; }

		/// <summary>
		/// Reads memory. Unreadable memory is returned as 0s.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="destination">Destination address</param>
		/// <param name="size">Number of bytes to read</param>
		/// <returns></returns>
		public unsafe abstract void ReadMemory(ulong address, byte* destination, int size);

		/// <summary>
		/// Reads memory. Unreadable memory is returned as 0s.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="destination">Destination buffer</param>
		/// <param name="destinationIndex">Destination index</param>
		/// <param name="size">Number of bytes to read</param>
		/// <returns></returns>
		public abstract void ReadMemory(ulong address, byte[] destination, int destinationIndex, int size);

		/// <summary>
		/// Writes memory.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="source">Source address</param>
		/// <param name="size">Number of bytes to write</param>
		/// <returns></returns>
		public unsafe abstract void WriteMemory(ulong address, byte* source, int size);

		/// <summary>
		/// Writes memory.
		/// </summary>
		/// <param name="address">Address in the debugged process</param>
		/// <param name="source">Source buffer</param>
		/// <param name="sourceIndex">Source index</param>
		/// <param name="size">Number of bytes to write</param>
		/// <returns></returns>
		public abstract void WriteMemory(ulong address, byte[] source, int sourceIndex, int size);
	}

	/// <summary>
	/// Machine
	/// </summary>
	public enum DbgMachine {
		/// <summary>
		/// x86, 32-bit
		/// </summary>
		X86,

		/// <summary>
		/// x64, 64-bit
		/// </summary>
		X64,
	}
}
