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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Notifies the debugger that the memory of a module has been updated (eg. decrypted)
	/// </summary>
	public abstract class DbgModuleMemoryRefreshedNotifier {
		/// <summary>
		/// Raised when the module's memory has been updated (eg. decrypted). The debugger will
		/// try to reset all breakpoints.
		/// </summary>
		public abstract event EventHandler<ModulesRefreshedEventArgs>? ModulesRefreshed;
	}

	/// <summary>
	/// <see cref="DbgModuleMemoryRefreshedNotifier.ModulesRefreshed"/> event args
	/// </summary>
	public readonly struct ModulesRefreshedEventArgs {
		/// <summary>
		/// Gets the modules
		/// </summary>
		public DbgModule[] Modules { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		public ModulesRefreshedEventArgs(DbgModule module) => Modules = new[] { module ?? throw new ArgumentNullException(nameof(module)) };

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="modules">Modules</param>
		public ModulesRefreshedEventArgs(DbgModule[] modules) => Modules = modules ?? throw new ArgumentNullException(nameof(modules));
	}
}
