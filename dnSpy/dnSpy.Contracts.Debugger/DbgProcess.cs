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
using System.ComponentModel;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// A debugged process
	/// </summary>
	public abstract class DbgProcess : DbgObject, INotifyPropertyChanged {
		/// <summary>
		/// Raised when a property is changed
		/// </summary>
		public abstract event PropertyChangedEventHandler PropertyChanged;

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
		/// Gets the process state
		/// </summary>
		public abstract DbgProcessState State { get; }

		/// <summary>
		/// Gets the filename or an empty string if it's unknown
		/// </summary>
		public abstract string Filename { get; }

		/// <summary>
		/// What is being debugged. This is shown in the UI (eg. Processes window)
		/// </summary>
		public abstract string Debugging { get; }

		/// <summary>
		/// Gets all threads
		/// </summary>
		public abstract DbgThread[] Threads { get; }

		/// <summary>
		/// Raised when <see cref="Threads"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgThread>> ThreadsChanged;

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

		/// <summary>
		/// true if the process gets detached when debugging stops (<see cref="StopDebugging"/>),
		/// false if the process gets terminated.
		/// </summary>
		public abstract bool ShouldDetach { get; set; }

		/// <summary>
		/// Stops debugging. This will either detach from the process or terminate it depending on <see cref="ShouldDetach"/>
		/// </summary>
		public void StopDebugging() {
			if (ShouldDetach)
				Detach();
			else
				Terminate();
		}

		/// <summary>
		/// Detaches the process, if possible, else it will be terminated
		/// </summary>
		public abstract void Detach();

		/// <summary>
		/// Terminates the process
		/// </summary>
		public abstract void Terminate();
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

	/// <summary>
	/// Process state
	/// </summary>
	public enum DbgProcessState {
		/// <summary>
		/// The process is running
		/// </summary>
		Running,

		/// <summary>
		/// The process is paused
		/// </summary>
		Paused,

		/// <summary>
		/// The process is terminated
		/// </summary>
		Terminated,
	}
}
