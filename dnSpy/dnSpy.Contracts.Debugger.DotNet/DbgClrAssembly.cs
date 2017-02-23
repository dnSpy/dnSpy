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

namespace dnSpy.Contracts.Debugger.DotNet {
	/// <summary>
	/// A .NET assembly in a debugged process
	/// </summary>
	public abstract class DbgClrAssembly : DbgObject {
		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => AppDomain.Process;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime ClrRuntime => AppDomain.ClrRuntime;

		/// <summary>
		/// Gets the app domain
		/// </summary>
		public abstract DbgClrAppDomain AppDomain { get; }

		/// <summary>
		/// Gets all modules
		/// </summary>
		public abstract DbgClrModule[] Modules { get; }

		/// <summary>
		/// Raised when <see cref="Modules"/> is changed
		/// </summary>
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgClrModule>> ModulesChanged;

		/// <summary>
		/// Gets the manifest module or null if it hasn't been loaded yet
		/// </summary>
		public abstract DbgClrModule ManifestModule { get; }
	}
}
