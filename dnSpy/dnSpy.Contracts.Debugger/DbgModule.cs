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
using System.ComponentModel;
using System.Diagnostics;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// A module in a process
	/// </summary>
	public abstract class DbgModule : DbgObject, INotifyPropertyChanged {
		/// <summary>
		/// Raised when a property is changed
		/// </summary>
		public abstract event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgRuntime Runtime { get; }

		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Runtime.Process;

		/// <summary>
		/// Gets the app domain or null if it's a process module
		/// </summary>
		public abstract DbgAppDomain? AppDomain { get; }

		/// <summary>
		/// Gets the module object created by the debug engine
		/// </summary>
		public abstract DbgInternalModule InternalModule { get; }

		/// <summary>
		/// true if it's an EXE file, false if it's a DLL file
		/// </summary>
		public abstract bool IsExe { get; }

		/// <summary>
		/// true if <see cref="Address"/> and <see cref="Size"/> are valid
		/// </summary>
		public bool HasAddress => Address != 0 && Size != 0;

		/// <summary>
		/// Address of module. Only valid if <see cref="HasAddress"/> is true
		/// </summary>
		public abstract ulong Address { get; }

		/// <summary>
		/// Size of module. Only valid if <see cref="HasAddress"/> is true
		/// </summary>
		public abstract uint Size { get; }

		/// <summary>
		/// Image layout
		/// </summary>
		public abstract DbgImageLayout ImageLayout { get; }

		/// <summary>
		/// Name of module
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Filename if it exists on disk, else it could be any longer name
		/// </summary>
		public abstract string Filename { get; }

		/// <summary>
		/// true if it's a dynamic module (the application can add more types and members to the module at runtime)
		/// </summary>
		public abstract bool IsDynamic { get; }

		/// <summary>
		/// true if it's an in-memory module
		/// </summary>
		public abstract bool IsInMemory { get; }

		/// <summary>
		/// true if it's an optimized module, false if it's an unoptimized module, and null if it's a native module.
		/// </summary>
		public abstract bool? IsOptimized { get; }

		/// <summary>
		/// Load order of this module
		/// </summary>
		public abstract int Order { get; }

		/// <summary>
		/// Timestamp (UTC) of module (eg. as found in the PE header) or null
		/// </summary>
		public abstract DateTime? Timestamp { get; }

		/// <summary>
		/// Gets the version, eg. the file version, see <see cref="FileVersionInfo"/>
		/// </summary>
		public abstract string Version { get; }

		/// <summary>
		/// Raised when the module's memory has been updated (eg. decrypted)
		/// </summary>
		public abstract event EventHandler? Refreshed;

		/// <summary>
		/// Gets incremented when the module gets refreshed (<see cref="Refreshed"/>)
		/// </summary>
		public abstract int RefreshedVersion { get; }
	}
}
