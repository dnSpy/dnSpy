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
using dnlib.DotNet;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Contracts.Debugger.DotNet.Metadata {
	/// <summary>
	/// Loads and creates dynamic modules (they can get extra classses and members at runtime)
	/// </summary>
	public abstract class DbgDynamicModuleProvider {
		/// <summary>
		/// Raised when a new class has been loaded in a dynamic assembly
		/// </summary>
		public abstract event EventHandler<ClassLoadedEventArgs>? ClassLoaded;

		/// <summary>
		/// Executes <paramref name="action"/> asynchronously on the thread required to load dynamic modules.
		/// </summary>
		/// <param name="action">Code to execute</param>
		public abstract void BeginInvoke(Action action);

		/// <summary>
		/// Gets the dynamic module's metadata or null if none is available
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="moduleId">Module id</param>
		/// <returns></returns>
		public abstract ModuleDef? GetDynamicMetadata(DbgModule module, out ModuleId moduleId);

		/// <summary>
		/// Gets all modified types. This method is called on the engine thread (see <see cref="BeginInvoke(Action)"/>)
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		public abstract IEnumerable<uint> GetModifiedTypes(DbgModule module);

		/// <summary>
		/// Initializes new classes that haven't gotten a load-class event yet. This method is called on the engine thread (see <see cref="BeginInvoke(Action)"/>)
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="nonLoadedTokens">Sorted tokens of classes that haven't been loaded but are still present in the metadata</param>
		public abstract void InitializeNonLoadedClasses(DbgModule module, uint[] nonLoadedTokens);

		/// <summary>
		/// Called on the engine thread just before and just after all types and members are force loaded
		/// </summary>
		/// <param name="modules">Modules that will be loaded</param>
		/// <param name="started">true if we're about to load all modules, false if we're done</param>
		public abstract void LoadEverything(DbgModule[] modules, bool started);
	}

	/// <summary>
	/// Class loaded event args
	/// </summary>
	public readonly struct ClassLoadedEventArgs {
		/// <summary>
		/// Module
		/// </summary>
		public DbgModule Module { get; }

		/// <summary>
		/// Token of loaded class
		/// </summary>
		public uint LoadedClassToken { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="loadedClassToken">Token of loaded class</param>
		public ClassLoadedEventArgs(DbgModule module, uint loadedClassToken) {
			Module = module ?? throw new ArgumentNullException(nameof(module));
			LoadedClassToken = loadedClassToken;
		}
	}
}
