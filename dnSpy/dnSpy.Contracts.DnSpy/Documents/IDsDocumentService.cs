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
using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.Contracts.Documents {
	/// <summary>
	/// Manages all loaded documents (which are shown in the treeview)
	/// </summary>
	public interface IDsDocumentService {
		/// <summary>
		/// Call this to disable loading assemblies in the document list until the return value's
		/// <see cref="IDisposable.Dispose()"/> method has been called.
		/// </summary>
		/// <returns></returns>
		IDisposable DisableAssemblyLoad();

		/// <summary>
		/// Notified when the collection gets changed
		/// </summary>
		event EventHandler<NotifyDocumentCollectionChangedEventArgs> CollectionChanged;

		/// <summary>
		/// Gets all documents. Doesn't include any children.
		/// </summary>
		/// <returns></returns>
		IDsDocument[] GetDocuments();

		/// <summary>
		/// Adds a new <see cref="IDsDocument"/> instance if it hasn't already been added. Returns
		/// the input or the existing instance.
		/// </summary>
		/// <param name="document">Document</param>
		/// <returns></returns>
		IDsDocument GetOrAdd(IDsDocument document);

		/// <summary>
		/// Adds <paramref name="document"/> to the list, even if another instance has already been
		/// inserted. Returns the input.
		/// </summary>
		/// <param name="document">Document</param>
		/// <param name="delayLoad">true to delay load</param>
		/// <param name="data">Data passed to listeners</param>
		/// <returns></returns>
		IDsDocument ForceAdd(IDsDocument document, bool delayLoad, object data);

		/// <summary>
		/// Creates a new <see cref="IDsDocument"/> instance or returns an existing one. null is
		/// returned if it couldn't be created.
		/// </summary>
		/// <param name="info">Document info</param>
		/// <param name="isAutoLoaded">New value of <see cref="IDsDocument.IsAutoLoaded"/> if the
		/// document gets created.</param>
		/// <returns></returns>
		IDsDocument TryGetOrCreate(DsDocumentInfo info, bool isAutoLoaded = false);

		/// <summary>
		/// Tries to create a new <see cref="IDsDocument"/> without adding it to the list. null is
		/// returned if it couldn't be created.
		/// </summary>
		/// <param name="info">Document info</param>
		/// <returns></returns>
		IDsDocument TryCreateOnly(DsDocumentInfo info);

		/// <summary>
		/// Resolves an assembly. Returns null if it couldn't be resolved.
		/// </summary>
		/// <param name="asm">Assembly</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or null</param>
		/// <returns></returns>
		IDsDocument Resolve(IAssembly asm, ModuleDef sourceModule);

		/// <summary>
		/// Returns an assembly or null if it's not in the list
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <returns></returns>
		IDsDocument FindAssembly(IAssembly assembly);

		/// <summary>
		/// Returns an inserted <see cref="IDsDocument"/> instance or null
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns></returns>
		IDsDocument Find(IDsDocumentNameKey key);

		/// <summary>
		/// Removes a document
		/// </summary>
		/// <param name="key">Key of document to remove. See <see cref="IDsDocument.Key"/></param>
		void Remove(IDsDocumentNameKey key);

		/// <summary>
		/// Removes documents
		/// </summary>
		/// <param name="documents">Documents</param>
		void Remove(IEnumerable<IDsDocument> documents);

		/// <summary>
		/// Clears all documents
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
		/// Creates a <see cref="IDsDocument"/>
		/// </summary>
		/// <param name="documentInfo">Document info</param>
		/// <param name="filename">Filename</param>
		/// <param name="isModule">true if it's a module, false if it's an assembly</param>
		/// <returns></returns>
		IDsDocument CreateDocument(DsDocumentInfo documentInfo, string filename, bool isModule = false);

		/// <summary>
		/// The assembly resolver it uses
		/// </summary>
		IAssemblyResolver AssemblyResolver { get; }
	}
}
