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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// A runtime in a process
	/// </summary>
	public abstract class DbgRuntime : DbgObject {
		/// <summary>
		/// Gets the process
		/// </summary>
		public abstract DbgProcess Process { get; }

		/// <summary>
		/// Gets the process unique runtime id. There must be exactly one such id per process.
		/// </summary>
		public abstract RuntimeId Id { get; }

		/// <summary>
		/// Gets the runtime GUID, see <see cref="PredefinedDbgRuntimeGuids"/>
		/// </summary>
		public abstract Guid Guid { get; }

		/// <summary>
		/// Gets the runtime kind GUID, see <see cref="PredefinedDbgRuntimeKindGuids"/>
		/// </summary>
		public abstract Guid RuntimeKindGuid { get; }

		/// <summary>
		/// Gets the runtime name
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets all runtime tags
		/// </summary>
		public abstract ReadOnlyCollection<string> Tags { get; }

		/// <summary>
		/// Gets the runtime object created by the debug engine
		/// </summary>
		public abstract DbgInternalRuntime InternalRuntime { get; }

		/// <summary>
		/// Gets all app domains
		/// </summary>
		public abstract DbgAppDomain[] AppDomains { get; }

		/// <summary>
		/// Raised when <see cref="AppDomains"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgAppDomain>> AppDomainsChanged;

		/// <summary>
		/// Gets all modules
		/// </summary>
		public abstract DbgModule[] Modules { get; }

		/// <summary>
		/// Raised when <see cref="Modules"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgModule>> ModulesChanged;

		/// <summary>
		/// Gets all threads
		/// </summary>
		public abstract DbgThread[] Threads { get; }

		/// <summary>
		/// Raised when <see cref="Threads"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgThread>> ThreadsChanged;

		/// <summary>
		/// Gets the break infos, it gets updated when the runtime breaks and cleared when it continues.
		/// </summary>
		public abstract ReadOnlyCollection<DbgBreakInfo> BreakInfos { get; }

		/// <summary>
		/// Closes <paramref name="obj"/> just before the runtime continues (or when it gets closed if it never continues)
		/// </summary>
		/// <param name="obj">Object</param>
		public abstract void CloseOnContinue(DbgObject obj);

		/// <summary>
		/// Closes <paramref name="objs"/> just before the runtime continues (or when it gets closed if it never continues)
		/// </summary>
		/// <param name="objs">Objects</param>
		public abstract void CloseOnContinue(IEnumerable<DbgObject> objs);

		/// <summary>
		/// Closes <paramref name="obj"/> when the runtime closes
		/// </summary>
		/// <param name="obj">Object</param>
		public void CloseOnExit(DbgObject obj) => CloseOnExit(new[] { obj ?? throw new ArgumentNullException(nameof(obj)) });

		/// <summary>
		/// Closes <paramref name="objs"/> when the runtime closes
		/// </summary>
		/// <param name="objs">Objects</param>
		public abstract void CloseOnExit(IEnumerable<DbgObject> objs);

		/// <summary>
		/// Closes <paramref name="obj"/> when the runtime closes
		/// </summary>
		/// <param name="obj">Object</param>
		public void CloseOnExit(IDisposable obj) => CloseOnExit(new[] { obj ?? throw new ArgumentNullException(nameof(obj)) });

		/// <summary>
		/// Closes <paramref name="objs"/> when the runtime closes
		/// </summary>
		/// <param name="objs">Objects</param>
		public abstract void CloseOnExit(IEnumerable<IDisposable> objs);
	}

	/// <summary>
	/// Break info kind
	/// </summary>
	public enum DbgBreakInfoKind {
		/// <summary>
		/// Unknown break reason
		/// </summary>
		Unknown,

		/// <summary>
		/// We've connected to the debugged process
		/// </summary>
		Connected,

		/// <summary>
		/// It's paused due to some debug message. <see cref="DbgBreakInfo.Data"/> is a <see cref="DbgMessageEventArgs"/>
		/// </summary>
		Message,
	}

	/// <summary>
	/// Break info
	/// </summary>
	public readonly struct DbgBreakInfo {
		/// <summary>
		/// Gets the kind
		/// </summary>
		public DbgBreakInfoKind Kind { get; }

		/// <summary>
		/// Gets the data, see <see cref="DbgBreakInfoKind"/> for more info
		/// </summary>
		public object Data { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="kind">Kind</param>
		/// <param name="data">Data</param>
		public DbgBreakInfo(DbgBreakInfoKind kind, object data) {
			Kind = kind;
			Data = data;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Debug message</param>
		public DbgBreakInfo(DbgMessageEventArgs message) {
			Kind = DbgBreakInfoKind.Message;
			Data = message ?? throw new ArgumentNullException(nameof(message));
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			switch (Kind) {
			case DbgBreakInfoKind.Unknown:
			case DbgBreakInfoKind.Connected:
			default:
				return Kind.ToString();

			case DbgBreakInfoKind.Message:
				return $"Debug message: {((DbgMessageEventArgs)Data).Kind}";
			}
		}
	}
}
