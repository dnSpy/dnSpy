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

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// A thread in a debugged process
	/// </summary>
	public abstract class DbgThread : DbgObject, INotifyPropertyChanged {
		/// <summary>
		/// Raised when a property is changed
		/// </summary>
		public abstract event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgRuntime Runtime { get; }

		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Runtime.Process;

		/// <summary>
		/// Gets the app domain or null if it's a process thread
		/// </summary>
		/// <returns></returns>
		public abstract DbgAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the thread kind, see <see cref="PredefinedThreadKinds"/>
		/// </summary>
		public abstract string Kind { get; }

		/// <summary>
		/// Gets the id of this thread
		/// </summary>
		public abstract int Id { get; }

		/// <summary>
		/// Gets the managed id of this thread or null if it's not a managed thread
		/// </summary>
		public abstract int? ManagedId { get; }

		/// <summary>
		/// Gets the thread name
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the suspended count. It's 0 if the thread isn't suspended, and greater than zero if it's suspended.
		/// </summary>
		public abstract int SuspendedCount { get; }

		/// <summary>
		/// Thread state
		/// </summary>
		public abstract ReadOnlyCollection<DbgStateInfo> State { get; }
	}
}
