/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace dnSpy.Contracts.Files {
	/// <summary>
	/// Manages all loaded files (which are shown in the treeview)
	/// </summary>
	public interface IFileManager {
		/// <summary>
		/// Notified when the collection gets changed
		/// </summary>
		event EventHandler<NotifyFileCollectionChangedEventArgs> CollectionChanged;

		/// <summary>
		/// Gets all files. Doesn't include any children.
		/// </summary>
		/// <returns></returns>
		IDnSpyFile[] GetFiles();

		/// <summary>
		/// Adds a new <see cref="IDnSpyFile"/> instance if it hasn't already been added. Returns
		/// the input or the existing instance.
		/// </summary>
		/// <param name="file">File</param>
		/// <returns></returns>
		IDnSpyFile GetOrAdd(IDnSpyFile file);

		/// <summary>
		/// Adds <paramref name="file"/> to the list, even if another instance has already been
		/// inserted. Returns the input.
		/// </summary>
		/// <param name="file">File</param>
		/// <param name="delayLoad">true to delay load</param>
		/// <param name="data">Data passed to listeners</param>
		/// <returns></returns>
		IDnSpyFile ForceAdd(IDnSpyFile file, bool delayLoad, object data);

		/// <summary>
		/// Creates a new <see cref="IDnSpyFile"/> instance or returns an existing one. null is
		/// returned if it couldn't be created.
		/// </summary>
		/// <param name="info">File info</param>
		/// <param name="isAutoLoaded">New value of <see cref="IDnSpyFile.IsAutoLoaded"/> if the
		/// file gets created.</param>
		/// <returns></returns>
		IDnSpyFile TryGetOrCreate(DnSpyFileInfo info, bool isAutoLoaded = false);

		/// <summary>
		/// Resolves an assembly. Returns null if it couldn't be resolved.
		/// </summary>
		/// <param name="asm">Assembly</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or null</param>
		/// <returns></returns>
		IDnSpyFile Resolve(IAssembly asm, ModuleDef sourceModule);

		/// <summary>
		/// Returns an assembly or null if it's not in the list
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <returns></returns>
		IDnSpyFile FindAssembly(IAssembly assembly);

		/// <summary>
		/// Returns an inserted <see cref="IDnSpyFile"/> instance or null
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns></returns>
		IDnSpyFile Find(IDnSpyFilenameKey key);

		/// <summary>
		/// Removes a file
		/// </summary>
		/// <param name="key">Key of file to remove. See <see cref="IDnSpyFile.Key"/></param>
		void Remove(IDnSpyFilenameKey key);

		/// <summary>
		/// Removes files
		/// </summary>
		/// <param name="files">Files</param>
		void Remove(IEnumerable<IDnSpyFile> files);

		/// <summary>
		/// Clears all files
		/// </summary>
		void Clear();

		/// <summary>
		/// Can be called once to set a delegate instance that will execute code in a certain
		/// thread. <see cref="CollectionChanged"/> can be called in any thread unless this method
		/// gets called.
		/// </summary>
		/// <param name="action">Action</param>
		void SetDispatcher(Action<Action> action);

		/// <summary>
		/// Gets the settings
		/// </summary>
		IFileManagerSettings Settings { get; }

		/// <summary>
		/// The assembly resolver it uses
		/// </summary>
		IAssemblyResolver AssemblyResolver { get; }
	}
}
