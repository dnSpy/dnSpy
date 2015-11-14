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
using dnlib.DotNet;

namespace dnSpy.Contracts.Files {
	/// <summary>
	/// Manages all loaded files (which are shown in the tree view)
	/// </summary>
	public interface IFileManager {
		/// <summary>
		/// Notified when the collection gets changed
		/// </summary>
		event EventHandler<NotifyFileCollectionChangedEventArgs> CollectionChanged;

		/// <summary>
		/// Gets all files
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
		/// Creates a new <see cref="IDnSpyFile"/> instance or returns an existing one
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <returns></returns>
		IDnSpyFile GetOrCreate(string filename);

		/// <summary>
		/// Resolves an assembly. Returns null if it couldn't be resolved.
		/// </summary>
		/// <param name="asm">Assembly</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or null</param>
		/// <returns></returns>
		IDnSpyFile Resolve(IAssembly asm, ModuleDef sourceModule);

		/// <summary>
		/// Removes a file
		/// </summary>
		/// <param name="key">Key of file to remove. See <see cref="IDnSpyFile.Key"/></param>
		void Remove(IDnSpyFilenameKey key);

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
	}
}
