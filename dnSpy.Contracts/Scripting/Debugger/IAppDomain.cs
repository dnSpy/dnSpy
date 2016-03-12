/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// An AppDomain in the debugged process
	/// </summary>
	public interface IAppDomain {
		/// <summary>
		/// AppDomain Id
		/// </summary>
		int Id { get; }

		/// <summary>
		/// true if the debugger is attached to the AppDomain
		/// </summary>
		bool IsAttached { get; }

		/// <summary>
		/// true if the threads are running freely
		/// </summary>
		bool IsRunning { get; }

		/// <summary>
		/// AppDomain name
		/// </summary>
		string Name { get; }

		/// <summary>
		/// true if the AppDomain has exited
		/// </summary>
		bool HasExited { get; }

		/// <summary>
		/// Gets all threads
		/// </summary>
		IEnumerable<IThread> Threads { get; }

		/// <summary>
		/// Gets all assemblies
		/// </summary>
		IEnumerable<IDebuggerAssembly> Assemblies { get; }

		/// <summary>
		/// Gets all modules
		/// </summary>
		IEnumerable<IDebuggerModule> Modules { get; }

		/// <summary>
		/// Gets the CLR AppDomain object or null if it hasn't been constructed yet
		/// </summary>
		IDebuggerValue Object { get; }

		/// <summary>
		/// Sets the debug state of all managed threads
		/// </summary>
		/// <param name="state">New state</param>
		/// <param name="thread">Thread to exempt from the new state or null</param>
		void SetAllThreadsDebugState(ThreadState state, IThread thread = null);
	}
}
